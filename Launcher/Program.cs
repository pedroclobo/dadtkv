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
    private static int? timeSlots = null;
    private static int? duration = null;
    private static DateTime? wallTime = null;

    private static ProcessRunner processRunner = new ProcessRunner();
    private static List<Tuple<string, string>> clients = new List<Tuple<string, string>>();
    private static List<Process> processes = new List<Process>();
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
                continue;
            }
            else if (command is PClientCommand)
            {
                var c = command as PClientCommand;
                clients.Add(new Tuple<string, string>(c.Identifier, c.Script));
            }
            else if (command is SCommand)
            {
                timeSlots = (command as SCommand).TimeSlots;
            }
            else if (command is DCommand)
            {
                duration = (command as DCommand).Duration;
            }
            else if (command is TCommand)
            {
                wallTime = (command as TCommand).WallTime;
            }
            else if (command is FCommand)
            {
                continue;
            }
        }

        // Spawn processes
        processes.AddRange(clients.Select(client => processRunner.Run("Client", "dotnet", $"run {client.Item1} {client.Item2}")));

        // Prompt user to kill all spawned processes.
        Console.WriteLine("Press any key to kill all processes.");
        Console.ReadLine();
        processes.ForEach(process => process.Kill());
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: Launcher.exe <configuration_file>");
    }
}
