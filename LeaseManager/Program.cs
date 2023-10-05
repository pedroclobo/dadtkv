namespace LeaseManager;

class LeaseManager
{
    static void Main(string[] args)
    {
        try
        {
            if (args.Length != 5)
            {
                PrintHelp();
                return;
            }

            var identifier = args[0];
            string[] protocolHostnamePort = args[1].Split("://");
            string[] hostnameAndPort = protocolHostnamePort[1].Split(":");
            string host = hostnameAndPort[0];
            int port = int.Parse(hostnameAndPort[1]);

            var leaseManagerURLS = args[2].Split(",").ToList();
            leaseManagerURLS.Remove(args[1]); // Remove own URL

            var transactionManagerURLS = args[3].Split(",").ToList();

            var wallTime = TimeSpan.Parse(args[4]);

            // State state = new State();
            // URBFrontend urbFrontend = new URBFrontend(identifier, transactionManagerURLS);

            // // Spawn Lease Manager
            // Grpc.Core.Server server = new Grpc.Core.Server
            // {
            //     Services = {
            //         DADTKVClientService.BindService(new DADTKVClientServiceImpl(state, urbFrontend)),
            //         URBService.BindService(new URBServiceImpl(identifier, state))
            //     },
            //     Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            // };

            // Wait for wall time
            var now = DateTime.Now;
            var waitTime = new DateTime(now.Year, now.Month, now.Day, wallTime.Hours, wallTime.Minutes, wallTime.Minutes) - now;

            if (waitTime.TotalMilliseconds > 0)
            {
                Console.WriteLine($"Waiting for {waitTime.TotalMilliseconds}ms");
                Thread.Sleep(waitTime);
            }
            else
            {
                Console.WriteLine("Invalid time");
                return;
            }

// server.Start();

            // Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine($"{identifier} listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {wallTime}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            //// Shutdown Server and Services
            //urbFrontend.Shutdown();
            //server.ShutdownAsync().Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    private static void PrintHelp()
    {
        Console.WriteLine("Usage: LeaseManager.exe <identifier> <URL> <LM-URLS> <TM-URLS> <wall_time");
    }
}

