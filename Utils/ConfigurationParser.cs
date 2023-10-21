using System;
using System.Dynamic;

namespace Utils;

enum ServerType
{
    TransactionManager,
    LeaseManager,
}

public sealed class ConfigurationParser
{
    private static readonly object lockObject = new object();

    private string _filename;
    private Dictionary<string, string> _clients;
    private Dictionary<string, Uri> _transactionManagers;
    private Dictionary<string, Uri> _leaseManagers;

    private List<string> _serverIdentifiers; // keep track of order in which servers are added

    private Dictionary<Tuple<string, int>, List<string>> _suspected;
    private Dictionary<Tuple<string, int>, bool> _failed;

    public int TimeSlots { get; private set; }
    public TimeSpan SlotDuration { get; private set; }
    public DateTime WallTime { get; set; }

    public ConfigurationParser(string filename)
    {
        _filename = filename;
        _clients = new Dictionary<string, string>();
        _leaseManagers = new Dictionary<string, Uri>();
        _transactionManagers = new Dictionary<string, Uri>();
        _serverIdentifiers = new List<string>();
        _suspected = new Dictionary<Tuple<string, int>, List<string>>();
        _failed = new Dictionary<Tuple<string, int>, bool>();

        Parse();
    }

    public List<string> ClientIdentifiers()
    {
        return _clients.Keys.ToList();
    }

    public string ClientScript(string identifier)
    {
        if (!_clients.ContainsKey(identifier))
        {
            throw new Exception("Invalid client identifier");
        }
        return _clients[identifier];
    }

    public string ServerUrl(string identifier)
    {
        if (_transactionManagers.ContainsKey(identifier))
        {
            return _transactionManagers[identifier].ToString();
        }
        else if (_leaseManagers.ContainsKey(identifier))
        {
            return _leaseManagers[identifier].ToString();
        }
        else
        {
            throw new Exception("Invalid server identifier");
        }
    }

    public string ServerHost(string identifier)
    {
        if (_transactionManagers.ContainsKey(identifier))
        {
            return _transactionManagers[identifier].Host;
        }
        else if (_leaseManagers.ContainsKey(identifier))
        {
            return _leaseManagers[identifier].Host;
        }
        else
        {
            throw new Exception("Invalid server identifier");
        }
    }

    public int ServerPort(string identifier)
    {
        if (_transactionManagers.ContainsKey(identifier))
        {
            return _transactionManagers[identifier].Port;
        }
        else if (_leaseManagers.ContainsKey(identifier))
        {
            return _leaseManagers[identifier].Port;
        }
        else
        {
            throw new Exception("Invalid server identifier");
        }
    }

    public List<string> TransactionManagerIdentifiers()
    {
        return _transactionManagers.Keys.ToList();
    }

    public Dictionary<string, Uri> TransactionManagerUrls()
    {
        return _transactionManagers;
    }

    public List<string> LeaseManagerIdentifiers()
    {
        return _leaseManagers.Keys.ToList();
    }

    public Dictionary<string, Uri> LeaseManagerUrls()
    {
        return _leaseManagers;
    }

    public int NumberLeaseManagers()
    {
        return _leaseManagers.Count;
    }

    public List<string> Suspected(string identifier, int slot)
    {
        var key = new Tuple<string, int>(identifier, slot);
        if (!_suspected.ContainsKey(key))
        {
            return new List<string>();
        }
        return _suspected[key];
    }

    public bool Failed(string identifier, int slot)
    {
        var key = new Tuple<string, int>(identifier, slot);
        if (!_failed.ContainsKey(key))
        {
            return false;
        }
        return _failed[key];
    }

    public async Task WaitForWallTimeAsync()
    {
        TimeSpan delay = WallTime - DateTime.Now;

        if (delay.TotalMilliseconds > 0)
        {
            await Task.Delay(delay);
        }
        else
        {
            throw new Exception("WallTime already passed");
        }
    }

    public void WriteWallTime(int increment)
    {
        string[] lines = File.ReadAllLines(_filename);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("T"))
            {
                lines[i] = $"T {DateTime.Now.AddSeconds(increment).ToString("HH:mm:ss")}";
            }
        }

        File.WriteAllLines(_filename, lines);
    }

    private void Parse()
    {
        var lines = File.ReadAllLines(_filename);
        foreach (var line in lines)
        {
            var tokens = line.Split(' ');

            // Ignore comments
            if (line.StartsWith("#"))
            {
                continue;
            }

            // Empty line denotes EOF
            else if (tokens.Length == 0)
            {
                return;
            }

            switch (tokens[0])
            {
                case "P":
                    if (tokens.Length < 4)
                    {
                        throw new Exception("Invalid P command");
                    }

                    switch (tokens[2])
                    {
                        case "T":
                            AddServer(ServerType.TransactionManager, tokens[1], new Uri(tokens[3]));
                            break;
                        case "L":
                            AddServer(ServerType.LeaseManager, tokens[1], new Uri(tokens[3]));
                            break;
                        case "C":
                            AddClient(tokens[1], tokens[3]);
                            break;
                    }
                    break;

                case "S":
                    TimeSlots = int.Parse(tokens[1]);
                    break;

                case "D":
                    SlotDuration = TimeSpan.FromMilliseconds(int.Parse(tokens[1]));
                    break;

                case "T":
                    WallTime = DateTime.Parse(DateTime.Today.ToString("yyyy-MM-dd") + " " + tokens[1]);
                    break;

                case "F":
                    int timeSlot = int.Parse(tokens[1]);
                    int i = 2;

                    while (i < tokens.Length && (tokens[i] == "C" || tokens[i] == "N"))
                    {
                        string serverIdentifier = GetNthServerIdentifier(i - 2);
                        switch (tokens[i])
                        {
                            case "N":
                                AddNormal(serverIdentifier, timeSlot);
                                break;
                            case "C":
                                AddCrashed(serverIdentifier, timeSlot);
                                break;
                        }
                        i++;
                    }

                    while (i < tokens.Length)
                    {
                        string[] pairs = tokens[i].TrimStart('(').TrimEnd(')').Split(',');
                        AddSuspected(pairs[0], timeSlot, pairs[1]);
                        i++;
                    }

                    break;

                default:
                    throw new Exception("Invalid command: " + tokens[0]);
            }
        }
    }

    private void AddServer(ServerType serverType, string identifier, Uri address)
    {
        switch (serverType)
        {
            case ServerType.LeaseManager:
                _leaseManagers.Add(identifier, address);
                break;
            case ServerType.TransactionManager:
                _transactionManagers.Add(identifier, address);
                break;
        }
        _serverIdentifiers.Add(identifier);
    }

    private void AddClient(string identifier, string filename)
    {
        _clients.Add(identifier, $"Config\\Client\\{filename}");
    }

    private void AddSuspected(string identifier, int slot, string suspected)
    {
        var key = new Tuple<string, int>(identifier, slot);
        if (!_suspected.ContainsKey(key))
        {
            _suspected.Add(key, new List<string>());
        }
        _suspected[key].Add(suspected);
    }

    private void AddNormal(string identifier, int slot)
    {
        var key = new Tuple<string, int>(identifier, slot);
        _failed.Add(key, false);
    }

    private void AddCrashed(string identifier, int slot)
    {
        var key = new Tuple<string, int>(identifier, slot);
        _failed.Add(key, true);
    }

    private string GetNthServerIdentifier(int n)
    {
        return _serverIdentifiers[n];
    }
}