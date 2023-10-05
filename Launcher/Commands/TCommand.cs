namespace Launcher.Commands
{
    public class TCommand: Command
    {
        public TimeSpan WallTime { get; set; }
        public TCommand(string time)
        {
            WallTime = TimeSpan.Parse(time);
        }

        public override string ToString()
        {
            return $"[T Command] - Wall Time: {WallTime}";
        }
    }
}
