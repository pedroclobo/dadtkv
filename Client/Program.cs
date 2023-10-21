using Client.Commands;
using Utils.ConfigurationParser;

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

        ConfigurationParser configurationParser = ConfigurationParser.From(filename);
        string script = configurationParser.ClientScript(identifier);
        DateTime wallTime = configurationParser.WallTime;

        Console.WriteLine($"Client {identifier} with script {script}");
        Console.WriteLine($"Starting at: {wallTime}");

        // Parse client script
        var frontend = new ClientFrontend(identifier, configurationParser.TransactionManagerUrls(), configurationParser);
        var commandParser = new CommandParser(frontend, script);
        commandParser.Parse();

        // Wait for wall time
        await configurationParser.WaitForWallTimeAsync();

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
