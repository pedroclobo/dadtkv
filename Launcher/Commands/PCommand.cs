namespace Launcher.Commands
{
    public enum ServerType
    {
        TransactionManager,
        LeaseManager,
    }
    public abstract class PCommand: Command { }
    public class PServerCommand: PCommand
    {
        public string Identifier { get; set; }
        public ServerType Type { get; set; }
        public string URL { get; set; }

        public PServerCommand(string identifier, ServerType type, string url)
        {
            Identifier = identifier;
            Type = type;
            URL = url;
        }

        public override string ToString()
        {
            return $"[P Command] - Identifier: {Identifier}, Type: {Type}, URL: {URL}";
        }
    }

    public class PClientCommand: PCommand
    {
        public string Identifier { get; set; }
        public string Script { get; set; }

        public PClientCommand(string identifier, string script)
        {
            Identifier = identifier;
            Script = script;
        }
        public override string ToString()
        {
            return $"[P Command] - Identifier: {Identifier}, Script: {Script}";
        }
    }
}
