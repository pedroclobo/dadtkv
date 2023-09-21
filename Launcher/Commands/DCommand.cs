namespace Launcher.Commands
{
    public class DCommand: Command
    {
        public int Duration { get; set; }
        public DCommand(int duration)
        {
            Duration = duration;
        }

        public override string ToString()
        {
            return $"[D Command] - Duration: {Duration}";
        }
    }
}
