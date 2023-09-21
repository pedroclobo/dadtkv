namespace Launcher.Commands
{
    public class FCommand: Command
    {
        public int TimeSlot { get; set; }
        public List<bool> IsCrashed { get; set; }
        public Dictionary<string, string> SuspectPairs;

        public FCommand(int timeSlot, string faulty, string[] syspectPairs)
        {
            TimeSlot = TimeSlot;

            IsCrashed = new List<bool>();
            foreach (var c in faulty.Split(" "))
            {
                IsCrashed.Add(c == "C");
            }

            SuspectPairs = new Dictionary<string, string>();
            foreach (var pair in syspectPairs)
            {
                var p = pair.Split(",");
                SuspectPairs.Add(p[0], p[1]);
            }
        }

        public override string ToString()
        {
            return $"[F Command] - Time Slot: {TimeSlot}, Crashed Processes: {IsCrashed}, Suspect Pairs: {SuspectPairs}";
        }
    }
}
