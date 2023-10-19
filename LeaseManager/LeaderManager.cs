using Utils.ConfigurationParser;

namespace LeaseManager;

public class LeaderManager
{
    private string _identifier;
    private List<string> _serverIdentifiers;
    private ConfigurationParser _configurationParser;

    public LeaderManager(string identifier, List<String> serverIdentifiers, ConfigurationParser parser)
    {
        _identifier = identifier;
        _serverIdentifiers = serverIdentifiers;
        _serverIdentifiers.Sort();
        _configurationParser = parser;
    }

    // TODO: this could possibly loop forever
    public bool AmILeader(int timeSlot)
    {
        int index = 0;
        List<string> suspected = _configurationParser.Suspected(_identifier, timeSlot);
        while (suspected.Contains(_serverIdentifiers[index]))
        {
            index = (index + 1) % _serverIdentifiers.Count;
        }

        return _identifier == _serverIdentifiers[index];
    }

    public bool AmIFailed(int timeSlot)
    {
        return _configurationParser.Failed(_identifier, timeSlot);
    }
}
