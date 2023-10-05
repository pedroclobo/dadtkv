namespace Client;

class Client
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            PrintHelp();
            return;
        }

        var identifier = args[0];
        var script = "../Config/Client/" + args[1];
        var serverURLs = args[2];
        var wallTime = TimeSpan.Parse(args[3]);

        Console.WriteLine($"I am a client with identifier: {identifier}");
        Console.WriteLine($"Running script: {script}");
        Console.WriteLine($"Transaction Manager URLs: {serverURLs}");
        Console.WriteLine($"Starting at: {wallTime}");

        // Wait for wall time
        var now = DateTime.Now;
        var waitTime = new DateTime(now.Year, now.Month, now.Day, wallTime.Hours, wallTime.Minutes, wallTime.Minutes) - now;

        if (waitTime.TotalMilliseconds > 0)
        {
            Thread.Sleep(waitTime);
        } else
        {
            Console.WriteLine("Invalid time");
            return;
        }

        // Spawn Frontend and Parser
        var frontend = new Frontend(identifier, serverURLs);
        var parser = new Parser(frontend, script);

        parser.Parse();

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();

        // Shutdown Grpc Channels
        frontend.Shutdown();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: Client.exe <identifier> <script> <server_urls> <wall_time>");
    }
}
