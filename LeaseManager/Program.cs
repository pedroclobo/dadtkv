using LeaseManager.Frontends;
using LeaseManager.Services;

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

            State state = new State();
            LeasePropagationFrontend leasePropagationFrontend = new LeasePropagationFrontend(transactionManagerURLS);
            PaxosFrontend paxosFrontend = new PaxosFrontend(identifier, state, leaseManagerURLS, leasePropagationFrontend);

            // Spawn Lease Manager
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = {
                    LeaseService.BindService(new LeaseServiceImpl(state)),
                    PaxosService.BindService(new PaxosServiceImpl(state))
                },
                Ports = { new Grpc.Core.ServerPort(host, port, Grpc.Core.ServerCredentials.Insecure) }
            };

            Console.WriteLine($"{identifier} listening on host {host} and port {port}");
            Console.WriteLine($"Starting at: {wallTime}");

            // Wait for wall time
            var now = DateTime.Now;
            var waitTime = new DateTime(now.Year, now.Month, now.Day, wallTime.Hours, wallTime.Minutes, wallTime.Minutes) - now;

            if (waitTime.TotalMilliseconds > 0)
            {
                Thread.Sleep(waitTime);
            }
            else
            {
                Console.WriteLine("Invalid time");
                return;
            }

            server.Start();

            // Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // TODO: Hardcoded Leader
            Timer timer = new Timer(async _ =>
            {
                if (identifier == "LM1")
                {
                    Console.WriteLine("Running paxos");
                    await paxosFrontend.Paxos();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            Console.WriteLine("Press any key to exit...");
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
        Console.WriteLine("Usage: LeaseManager.exe <identifier> <URL> <LM-URLS> <TM-URLS> <wall_time");
    }
}

