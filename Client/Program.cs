namespace Client;

class Client
{
    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            PrintHelp();
            return;
        }

        var identifier = args[0];
        var script = "../Config/Client/" + args[1];
        var serverURLs = args[2];

        Console.WriteLine($"I am a client with identifier: {identifier}");
        Console.WriteLine($"Running script: {script}");
        Console.WriteLine($"Transaction Manager URLs: {serverURLs}");

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
        Console.WriteLine("Usage: Client.exe <identifier> <script> <server_urls>");
    }
}
