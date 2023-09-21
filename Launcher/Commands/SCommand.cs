namespace Launcher.Commands
{
    public class SCommand: Command
    {
        public int TimeSlots { get; set; }
        public SCommand(int timeSlots)
        {
            TimeSlots = timeSlots;
        }

        public override string ToString()
        {
            return $"[S Command] - Time Slots: {TimeSlots}";
        }
    }
}
