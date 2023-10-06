using System.Diagnostics;
using Utils.ConfigurationParser;

namespace Launcher;
public class Launcher
{
    private static ProcessRunner _processRunner = new ProcessRunner();
    private static List<Process> _processes = new List<Process>();
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            PrintHelp();
            return;
        }
        string filename = args[0];

        // Initialize ConfigurationParser
        ConfigurationParser parser = ConfigurationParser.From(filename);

        // Spawn Clients
        foreach (string identifier in parser.ClientIdentifiers())
        {
            _processes.Add(_processRunner.Run($"Client {identifier} {filename}"));
        }

        // Spawn Transaction Managers
        foreach (string identifier in parser.TransactionManagerIdentifiers())
        {
            _processes.Add(_processRunner.Run($"TransactionManager {identifier} {filename}"));
        }

        // Spawn Lease Managers
        foreach (string identifier in parser.LeaseManagerIdentifiers())
        {
            _processes.Add(_processRunner.Run($"LeaseManager {identifier} {filename}"));
        }

        // Prompt user to kill all spawned processes.
        Console.WriteLine("Press any key to kill all processes.");
        Console.ReadLine();
        _processes.ForEach(process => process.Kill());
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: Launcher.exe <configuration_file>");
    }
    private class ProcessRunner
    {
        public Process Run(string arguments)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {arguments}",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
            });

            if (process == null)
            {
                throw new Exception("Failed to start process");
            }

            Console.WriteLine($"dotnet run --project {arguments}");

            return process;
        }
    }
}

