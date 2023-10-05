using Launcher.Commands;
using System.Diagnostics;

namespace Launcher;

class Parser
{
    public static List<Command> Parse(string Filename)
    {
        var commands = new List<Command>();

        var lines = System.IO.File.ReadAllLines(Filename);
        foreach (var line in lines)
        {
            var tokens = line.Split(' ');

            // Ignore comments
            if (line.StartsWith("#"))
            {
                continue;
            }

            // Empty line denotes EOF
            else if (tokens.Length == 0)
            {
                return commands;
            }

            switch (tokens[0])
            {
                case "P":
                    if (tokens.Length < 4)
                    {
                        Console.WriteLine("Invalid P command");
                        return commands;
                    }

                    switch (tokens[2])
                    {
                        case "T":
                            commands.Add(new PServerCommand(tokens[1], ServerType.TransactionManager, tokens[3]));
                            break;
                        case "L":
                            commands.Add(new PServerCommand(tokens[1], ServerType.LeaseManager, tokens[3]));
                            break;
                        case "C":
                            commands.Add(new PClientCommand(tokens[1], tokens[3]));
                            break;
                    }
                    break;

                case "S":
                    commands.Add(new SCommand(int.Parse(tokens[1])));
                    break;

                case "D":
                    commands.Add(new DCommand(int.Parse(tokens[1])));
                    break;

                case "T":
                    commands.Add(new TCommand(tokens[1]));
                    break;

                // F 1 N N N N N N (TM1,TM2) (lease3,LM2)
                case "F":
                    var timeSlot = int.Parse(tokens[1]);

                    // Build string with N's or C's separated by spaces
                    string faulty = "";
                    var i = 2;
                    while (!tokens[i].StartsWith("("))
                    {
                        faulty += tokens[i] + " ";
                        i++;
                    }

                    // Build list of suspect pairs
                    var suspectPairs = new List<string>();
                    while (i < tokens.Length)
                    {
                        suspectPairs.Add(tokens[i]);
                        i++;
                    }

                    commands.Add(new FCommand(timeSlot, faulty, suspectPairs.ToArray()));

                    break;

                default:
                    throw new Exception("Invalid command: " + tokens[0]);
            }
        }

        return commands;
    }
}

class ProcessRunner
{
    public Process Run(string cwd, string executable, string arguments)
    {
        // Save current working directory
        var pwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(cwd);

        // Start process
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
        });

        if (process == null)
        {
            throw new Exception("Failed to start process");
        }

        // Restore working directory
        Directory.SetCurrentDirectory(pwd);

        return process;
    }
}
class Launcher
{
    private static int? _timeSlots = null;
    private static int? _duration = null;
    private static TimeSpan? _wallTime = null;

    private static ProcessRunner _processRunner = new ProcessRunner();
    private static List<Tuple<string, string>> _clients = new List<Tuple<string, string>>();
    private static List<Tuple<string, ServerType, string>> _servers = new List<Tuple<string, ServerType, string>>();

    private static List<string> _transactionManagerURLS = new List<string>();

    private static List<Process> _processes = new List<Process>();
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            PrintHelp();
            return;
        }
        var filename = args[0];
        var commads = Parser.Parse(filename);

        foreach (var command in commads)
        {
            Console.WriteLine(command);
            if (command is PServerCommand)
            {
                var c = command as PServerCommand ?? throw new Exception("Invalid command");
                _servers.Add(new(c.Identifier, ServerType.TransactionManager, c.URL));
            }
            else if (command is PClientCommand)
            {
                var c = command as PClientCommand ?? throw new Exception("Invalid command");
                _clients.Add(new(c.Identifier, c.Script));
            }
            else if (command is SCommand)
            {
                var c = command as SCommand ?? throw new Exception("Invalid command");
                _timeSlots = c.TimeSlots;
            }
            else if (command is DCommand)
            {
                var c = command as DCommand ?? throw new Exception("Invalid command");
                _duration = c.Duration;
            }
            else if (command is TCommand)
            {
                var c = command as TCommand ?? throw new Exception("Invalid command");
                _wallTime = c.WallTime;
            }
            else if (command is FCommand)
            {
                continue;
            }
        }

        // Create list of Transaction Manager URLs
        string transactionManagerURLS = string.Join(",", _servers.Where(server => server.Item2 == ServerType.TransactionManager).Select(server => server.Item3));

        // Spawn Clients
        Console.WriteLine(_wallTime);
        _processes.AddRange(_clients.Select(client => _processRunner.Run("Client", "dotnet", $"run {client.Item1} {client.Item2} {transactionManagerURLS} {_wallTime}")));

        // Spawn Transaction Managers
        _processes.AddRange(_servers.Where(server => server.Item2 == ServerType.TransactionManager).Select(server => _processRunner.Run("TransactionManager", "dotnet", $"run {server.Item1} {server.Item3} {transactionManagerURLS} {_wallTime}")));

        // Prompt user to kill all spawned processes.
        Console.WriteLine("Press any key to kill all processes.");
        Console.ReadLine();
        _processes.ForEach(process => process.Kill());
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: Launcher.exe <configuration_file>");
    }
}
