using System;
using System.Dynamic;

namespace Utils.ConfigurationParser;

enum ServerType
{
    TransactionManager,
    LeaseManager,
}

public sealed class ConfigurationParser
{
    private static ConfigurationParser? instance = null;
    private static readonly object lockObject = new object();

    private string _filename;
    private Dictionary<string, string> _clients;
    private Dictionary<string, Uri> _transactionManagers;
    private Dictionary<string, Uri> _leaseManagers;

    public int TimeSlots { get; private set; }
    public TimeSpan SlotDuration { get; private set; }
    public DateTime WallTime { get; private set; }

    private ConfigurationParser(string filename)
    {
        _filename = filename;
        _clients = new Dictionary<string, string>();
        _leaseManagers = new Dictionary<string, Uri>();
        _transactionManagers = new Dictionary<string, Uri>();
    }

    public static ConfigurationParser From(string filename)
    {
        lock (lockObject)
        {
            if (instance == null)
            {
                instance = new ConfigurationParser(filename);
                instance.Parse();
            }
        }

        return instance;
    }

    public List<String> ClientIdentifiers()
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
        } else if (_leaseManagers.ContainsKey(identifier))
        {
            return _leaseManagers[identifier].ToString();
        } else
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

    public List<String> TransactionManagerIdentifiers()
    {
        return _transactionManagers.Keys.ToList();
    }

    public List<Uri> TransactionManagerUrls()
    {
        return _transactionManagers.Values.ToList();
    }

    public List<String> LeaseManagerIdentifiers()
    {
        return _leaseManagers.Keys.ToList();
    }

    public List<Uri> LeaseManagerUrls()
    {
        return _leaseManagers.Values.ToList();
    }

    public async Task WaitForWallTimeAsync()
    {
        TimeSpan delay = WallTime - DateTime.Now;

        if (delay.TotalMilliseconds > 0)
        {
            await Task.Delay(delay);
        } else
        {
            throw new Exception("WallTime already passed");
        }
    }

    public void Parse()
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
    }

    private void AddClient(string identifier, string filename)
    {
        _clients.Add(identifier, $"Config\\Client\\{filename}");
    }
}