using Client.Commands;
using Utils;

namespace Client;
public class CommandParser
{
    private ClientFrontend _frontend;
    private string _script;
    private List<Command> _commands;

    public CommandParser(ClientFrontend frontend, string script)
    {
        _frontend = frontend;
        _script = script;
        _commands = new List<Command>();
    }

    public List<Command> Parse()
    {
        try
        {
            var lines = System.IO.File.ReadAllLines(_script);
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
                    return _commands;
                }

                switch (tokens[0])
                {
                    case "T":
                        if (tokens.Length < 3)
                        {
                            throw new Exception("Invalid T command");
                        }

                        // Split read keys
                        List<string> read = tokens[1].Trim('(', ')').Split(",").ToList();
                        read.RemoveAll(s => s == "");       // Remove all empty strings
                        // Remove all quotes
                        for (int i = 0; i < read.Count; i++)
                        {
                            read[i] = read[i].Trim('"');
                        }

                        // Split write <key, value> pairs
                        var write = new List<Utils.DadInteger>();
                        var pairs = tokens[2].Trim('(', ')').Split(">,<").ToList();
                        pairs.RemoveAll(s => s == "");       // Remove all empty strings
                        foreach (var token in pairs)
                        {
                            write.Add(Utils.DadInteger.Parse(token));
                        }

                        _commands.Add(new TCommand(read, write));

                        break;

                    case "W":
                        if (tokens.Length < 2)
                        {
                            throw new Exception("Invalid W command");
                        }

                        _commands.Add(new WCommand(int.Parse(tokens[1])));

                        break;

                    case "S":
                        _commands.Add(new SCommand());
                        break;

                    default:
                        throw new Exception("Invalid command: " + tokens[0]);
                }
            }
        }
        catch (System.IO.FileNotFoundException e)
        {
            Console.WriteLine("File not found: " + e.FileName);
        }
        catch (System.Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        return _commands;
    }

    public async Task Execute()
    {
        foreach (Command command in _commands)
        {
            if (command is TCommand)
            {
                TCommand tCommand = (TCommand)command;

                Console.WriteLine("Request: {0}", command);
                List<DadInteger> response = await _frontend.TxSubmit(tCommand.Read, tCommand.Write);
                Console.WriteLine("Reply: [{0}]", string.Join(", ", response));
            }
            else if (command is WCommand)
            {
                WCommand wCommand = (WCommand)command;

                Console.WriteLine("Waiting for {0} ms", wCommand.WaitTime);
                Thread.Sleep(wCommand.WaitTime);
            }
            else if (command is SCommand)
            {
                var responses = await _frontend.Status();
                foreach (var response in responses)
                {
                    Console.WriteLine($"Reply - S: {response}");
                }
            }
        }
    }
}
