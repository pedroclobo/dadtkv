using Client.Commands;

namespace Client;
public class Parser
{
    private Frontend _frontend;
    private string _script;
    public Parser(Frontend frontend, string script)
    {
        _frontend = frontend;
        _script = script;
    }

    public async void Parse()
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
                    return;
                }

                switch (tokens[0])
                {
                    case "T":
                        if (tokens.Length < 3)
                        {
                            throw new Exception("Invalid T command");
                        }

                        var read = new List<string>();
                        var write = new List<DadInt>();
                        var command = new TCommand(read, write);

                        var result = await _frontend.TxSubmit(read, write);

                        Console.WriteLine(command);
                        Console.WriteLine(result);

                        break;

                    case "W":
                        if (tokens.Length < 2)
                        {
                            throw new Exception("Invalid W command");
                        }

                        int time = int.Parse(tokens[1]);
                        Thread.Sleep(time);

                        break;

                    default:
                        throw new Exception("Invalid command: " + tokens[0]);
                }
            }

            Console.ReadLine();

        }
        catch (System.IO.FileNotFoundException e)
        {
            Console.WriteLine("File not found: " + e.FileName);
        }
        catch (System.Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }
}
