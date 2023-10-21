using TransactionManager.Frontends;
using TransactionManager.Services;
using Utils.ConfigurationParser;

namespace TransactionManager;

public class TransactionManager
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

            ConfigurationParser parser = ConfigurationParser.From(filename);
            string host = parser.ServerHost(identifier);
            int port = parser.ServerPort(identifier);

            Dictionary<string, Uri> transactionManagerURLS = parser.TransactionManagerUrls();
            transactionManagerURLS.Remove(identifier); // Remove own URL

            Dictionary<string, Uri> leaseManagerURLS = parser.LeaseManagerUrls();

            // Create server
            State state = new State();
            URBFrontend urbFrontend = new URBFrontend(identifier, transactionManagerURLS);
            LeaseFrontend leaseFrontend = new LeaseFrontend(identifier, leaseManagerURLS);
            LeaseManagementFrontend leaseManagementFrontend = new LeaseManagementFrontend(identifier, leaseManagerURLS);
            LeaseQueue leaseQueue = new LeaseQueue(identifier);

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = {
                    DADTKVClientService.BindService(new DADTKVClientServiceImpl(identifier, state, urbFrontend, leaseFrontend, leaseManagementFrontend, leaseQueue)),
                    LeaseManagementService.BindService(new LeaseManagementServiceImpl(identifier, leaseQueue)),
                    PaxosLearnerService.BindService(new PaxosLearnerServiceImpl(parser.LeaseManagerIdentifiers().Count(), leaseQueue)),
                    URBService.BindService(new URBServiceImpl(identifier, state))
                },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            Console.WriteLine($"Transaction Manager {identifier} will be listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {parser.WallTime}");

            // Configure HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Wait for wall time
            await parser.WaitForWallTimeAsync();

            server.Start();

            int currentTimeSlot = 1;
            int timeSlots = parser.TimeSlots;
            TimeSpan slotDuration = parser.SlotDuration;
            parser.Update(currentTimeSlot);

            Timer timer = null;
            timer = new Timer(async _ =>
            {
                if (currentTimeSlot > timeSlots)
                {
                    Console.WriteLine("Time slots expired! Shutting down!");
                    Console.WriteLine("Press any key to exit...");

                    urbFrontend.Shutdown();
                    leaseFrontend.Shutdown();
                    leaseManagementFrontend.Shutdown();
                    server.ShutdownAsync().Wait();

                    timer.Dispose();

                    Console.ReadLine();
                    Environment.Exit(0);
                }

                parser.Update(currentTimeSlot);

                Console.WriteLine($"\nTime Slot: {currentTimeSlot}");

                currentTimeSlot++;
            }, null, TimeSpan.Zero, slotDuration);

            Console.WriteLine("Press any key to exit...");
            Console.WriteLine();
            Console.ReadLine();

            // Shutdown Server and Services
            urbFrontend.Shutdown();
            leaseFrontend.Shutdown();
            server.ShutdownAsync().Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    private static void PrintHelp()
    {
        Console.WriteLine("Usage: TransactionManager.exe <identifier> <configuration-file>");
    }
}
