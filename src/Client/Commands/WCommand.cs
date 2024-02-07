namespace Client.Commands;

public class WCommand: Command
{
    public int WaitTime { get; }
    public WCommand(int waitTime)
    {
        WaitTime = waitTime;
    }
}
