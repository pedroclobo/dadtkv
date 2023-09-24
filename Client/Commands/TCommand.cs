using Utils;

namespace Client.Commands;
public class TCommand: Command
{
    public List<string> Read { get; }
    public List<DadInt> Write { get; }
    public TCommand(List<string> read, List<DadInt> write)
    {
        Read = read;
        Write = write;
    }
}
