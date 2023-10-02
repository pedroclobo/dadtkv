using TransactionManager.Services;

namespace TransactionManager;

class TransactionManager
{
    static void Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }

            var identifier = args[0];
            string[] protocolHostnamePort = args[1].Split("://");
            string[] hostnameAndPort = protocolHostnamePort[1].Split(":");
            string host = hostnameAndPort[0];
            int port = int.Parse(hostnameAndPort[1]);

            // Spawn Transaction Manager
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { DADTKVClientService.BindService(new DADTKVClientServiceImpl()) },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            server.Start();

            // Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine($"{identifier} listening on host {host} and port {port}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            // Shutdown Server
            server.ShutdownAsync().Wait();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    private static void PrintHelp()
    {
        Console.WriteLine("Usage: TransactionManager.exe <identifier> <URL>");
    }
}
