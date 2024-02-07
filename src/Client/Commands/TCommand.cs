using Utils;

namespace Client.Commands;
public class TCommand: Command
{
    public List<string> Read { get; }
    public List<DadInteger> Write { get; }
    public TCommand(List<string> read, List<DadInteger> write)
    {
        Read = read;
        Write = write;
    }

    public override string ToString()
    {
        var read = string.Join(", ", Read);
        var write = string.Join(", ", Write);

        return $"T - Read: [{read}], Write: [{write}]";
    }
}
