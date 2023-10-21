using Client.Commands;
using Utils;

namespace Client;

public class Client
{
    public static async Task Main(string[] args)
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

        string script = parser.ClientScript(identifier);
        DateTime wallTime = parser.WallTime;

        Console.WriteLine($"Client {identifier} with script {script}");
        Console.WriteLine($"Starting at: {wallTime}");

        // Parse client script
        var frontend = new ClientFrontend(identifier, parser.TransactionManagerUrls(), parser);
        var commandParser = new CommandParser(frontend, script);
        commandParser.Parse();

        // Wait for wall time
        await parser.WaitForWallTimeAsync();

        Console.WriteLine("Press any key to exit...");
        Console.WriteLine();
        await commandParser.Execute();
        Console.ReadLine();

        // Clean Resources
        frontend.Shutdown();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: Client.exe <identifier> <configuration-file>");
    }
}
