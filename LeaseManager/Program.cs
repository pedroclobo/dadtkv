using LeaseManager.Frontends;
using LeaseManager.Services;
using Utils;

namespace LeaseManager;

public class LeaseManager
{
    public static async Task Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }
            string identifier = args[0];
            string filename = args[1];

            ConfigurationParser parser = new ConfigurationParser(filename);
            FailureDetector failureDetector = new FailureDetector(identifier, parser);
            string host = parser.ServerHost(identifier);
            int port = parser.ServerPort(identifier);

            List<string> leaseManagerIdentifiers = parser.LeaseManagerIdentifiers();
            Dictionary<string, Uri> leaseManagerUrls = parser.LeaseManagerUrls();
            leaseManagerUrls.Remove(identifier); // Remove own URL

            Dictionary<string, Uri> transactionManagerURLS = parser.TransactionManagerUrls();

            // Create server
            State state = new State();
            LeaderManager leaderManager = new LeaderManager(identifier, leaseManagerIdentifiers, parser);
            PaxosFrontend paxosFrontend = new PaxosFrontend(identifier, state, leaseManagerUrls, transactionManagerURLS, failureDetector);
            Dictionary<string, Uri> urls = new Dictionary<string, Uri>(leaseManagerUrls);
            foreach (var pair in transactionManagerURLS)
            {
                urls.Add(pair.Key, pair.Value);
            }
            PaxosLearnerFrontend paxosLearnerFrontend = new PaxosLearnerFrontend(identifier, urls, failureDetector);

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = {
                    LeaseService.BindService(new LeaseServiceImpl(state)),
                    PaxosService.BindService(new PaxosServiceImpl(identifier, paxosLearnerFrontend, failureDetector)),
                    PaxosLearnerService.BindService(new PaxosLearnerServiceImpl(identifier, parser.LeaseManagerIdentifiers().Count(), failureDetector))
                },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            Console.WriteLine($"Lease Manager {identifier} will be listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {parser.WallTime}");

            // Configure HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Wait for wall time
            await parser.WaitForWallTimeAsync();

            server.Start();

            int currentTimeSlot = 1;
            int timeSlots = parser.TimeSlots;
            TimeSpan slotDuration = parser.SlotDuration;

            // TODO: Hardcoded Leader
            Timer timer = null;
            timer = new Timer(async _ =>
            {
                if (currentTimeSlot > timeSlots)
                {
                    Console.WriteLine("Time slots expired! Shutting down!");
                    Console.WriteLine("Press any key to exit...");

                    paxosFrontend.Shutdown();
                    paxosLearnerFrontend.Shutdown();
                    server.ShutdownAsync().Wait();

                    timer.Dispose();

                    Console.ReadLine();
                    Environment.Exit(0);
                }

                if (leaderManager.AmIFailed(currentTimeSlot))
                {
                    Console.WriteLine("I am failed! Shutting down!");
                    Console.WriteLine("Press any key to exit...");

                    paxosFrontend.Shutdown();
                    paxosLearnerFrontend.Shutdown();
                    server.ShutdownAsync().Wait();

                    timer.Dispose();

                    Console.ReadLine();
                    Environment.Exit(0);
                }

                failureDetector.SetTimeSlot(currentTimeSlot);

                Console.WriteLine($"\nTime Slot: {currentTimeSlot}");

                if (currentTimeSlot <= timeSlots && leaderManager.AmILeader(currentTimeSlot))
                {
                    Console.WriteLine($"I am the leader for epoch {currentTimeSlot}. Running paxos");
                    paxosFrontend.Paxos(currentTimeSlot);
                }
                currentTimeSlot++;
            }, null, TimeSpan.Zero, slotDuration);

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            // Shutdown Server and Services
            paxosFrontend.Shutdown();
            server.ShutdownAsync().Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    private static void PrintHelp()
    {
        Console.WriteLine("Usage: LeaseManager.exe <identifier> <configuration-file>");
    }
}

