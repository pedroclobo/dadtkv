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

            ConfigurationParser configurationParser = ConfigurationParser.From(filename);
            string host = configurationParser.ServerHost(identifier);
            int port = configurationParser.ServerPort(identifier);

            List<Uri> transactionManagerURLS = configurationParser.TransactionManagerUrls();
            transactionManagerURLS.Remove(new Uri(configurationParser.ServerUrl(identifier))); // Remove own URL

            List<Uri> leaseManagerURLS = configurationParser.LeaseManagerUrls();

            // Create server
            State state = new State();
            URBFrontend urbFrontend = new URBFrontend(identifier, transactionManagerURLS);
            LeaseFrontend leaseFrontend = new LeaseFrontend(identifier, leaseManagerURLS);

            LeasePropagationServiceImpl leasePropagationService = new LeasePropagationServiceImpl();
            leasePropagationService.LeasesChanged += leaseFrontend.OnLeasesChanged;

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = {
                    DADTKVClientService.BindService(new DADTKVClientServiceImpl(state, urbFrontend, leaseFrontend)),
                    URBService.BindService(new URBServiceImpl(identifier, state)),
                    LeasePropagationService.BindService(leasePropagationService)
                },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            Console.WriteLine($"Transaction Manager {identifier} will be listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {configurationParser.WallTime}");

            // Configure HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Wait for wall time
            await configurationParser.WaitForWallTimeAsync();

            server.Start();

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
