namespace Launcher.Commands
{
    public class TCommand: Command
    {
        public DateTime WallTime { get; set; }
        public TCommand(string time)
        {
            WallTime = DateTime.Parse(time);
        }

        public override string ToString()
        {
            return $"[T Command] - Wall Time: {WallTime}";
        }
    }
}
