using System.Diagnostics;
using Utils;

namespace Launcher;
public class Launcher
{
    private static ProcessRunner _processRunner = new ProcessRunner();
    private static List<Process> _processes = new List<Process>();
    public static void Main(string[] args)
    {
        if (args.Length > 2)
        {
            PrintHelp();
            return;
        }
        string filename = args[0];

        // Initialize ConfigurationParser
        ConfigurationParser parser = new ConfigurationParser(filename);

        // Override Wall Time from Configuration File
        if (args.Length == 2 && args[1].StartsWith("-o"))
        {
            parser.WriteWallTime(30);
            Console.WriteLine($"Setting wall time to 30 seconds from now");
        }

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
        Console.WriteLine("Usage: Launcher.exe <configuration_file> [-o]");
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

