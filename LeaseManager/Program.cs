using LeaseManager.Frontends;
using LeaseManager.Services;
using Utils.ConfigurationParser;

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

            ConfigurationParser configurationParser = ConfigurationParser.From(filename);
            string host = configurationParser.ServerHost(identifier);
            int port = configurationParser.ServerPort(identifier);

            List<Uri> leaseManagerUrls = configurationParser.LeaseManagerUrls();
            leaseManagerUrls.Remove(new Uri(configurationParser.ServerUrl(identifier))); // Remove own URL

            List<Uri> transactionManagerURLS = configurationParser.TransactionManagerUrls();

            // Create server
            State state = new State();
            LeasePropagationFrontend leasePropagationFrontend = new LeasePropagationFrontend(transactionManagerURLS);
            PaxosFrontend paxosFrontend = new PaxosFrontend(state, leaseManagerUrls, leasePropagationFrontend);

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = {
                    LeaseService.BindService(new LeaseServiceImpl(state)),
                    PaxosService.BindService(new PaxosServiceImpl(state))
                },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            Console.WriteLine($"Lease Manager {identifier} will be listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {configurationParser.WallTime}");

            // Configure HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Wait for wall time
            await configurationParser.WaitForWallTimeAsync();

            server.Start();

            int currentTimeSlot = 0;
            int timeSlots = configurationParser.TimeSlots;
            TimeSpan slotDuration = configurationParser.SlotDuration;

            // TODO: Hardcoded Leader
            Timer timer = new Timer(async _ =>
            {
                if (currentTimeSlot < timeSlots)
                {
                    if (identifier == "LM1")
                    {
                        Console.WriteLine("Running paxos");
                        await paxosFrontend.Paxos();
                    }
                    currentTimeSlot++;
                }
            }, null, TimeSpan.Zero, slotDuration);

            Console.WriteLine("Press any key to exit...");
            Console.WriteLine();
            Console.ReadLine();

            // Shutdown Server and Services
            leasePropagationFrontend.Shutdown();
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

