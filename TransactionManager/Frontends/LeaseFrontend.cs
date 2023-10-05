using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using TransactionManager.Services;

namespace TransactionManager.Frontends;
public class LeaseFrontend
{
    private string _identifier;

    private List<GrpcChannel> _channels;
    private List<LeaseService.LeaseServiceClient> _clients;

    private List<string> _leaseKeys;

    public LeaseFrontend(string identifier, List<string> leaseManagerUrls)
    {
        _identifier = identifier;

        _channels = new List<GrpcChannel>();
        foreach (var serverURL in leaseManagerUrls)
        {
            _channels.Add(GrpcChannel.ForAddress(serverURL));
        }

        _clients = new List<LeaseService.LeaseServiceClient>();
        foreach (var channel in _channels)
        {
            _clients.Add(new LeaseService.LeaseServiceClient(channel));
        }

        _leaseKeys = new List<string>();
    }
    public void RequestLease(List<string> keys)
    {
        var request = new LeaseRequest
        {
            TransactionManagerId = _identifier,
            Keys = { keys },
        };

        Console.WriteLine("Requesting lease for keys: {0}", string.Join(", ", keys));

        List<Task<Empty>> tasks = new List<Task<Empty>>();
        foreach (var client in _clients)
        {
            tasks.Add(Task.Run(() => client.RequestLease(request)));
        }

        Task.WhenAny(tasks);
    }
    public bool HasLease(string key)
    {
        lock (_leaseKeys)
        {
            return _leaseKeys.Contains(key);
        }
    }
    public bool HasLease(List<string> key)
    {
        lock (_leaseKeys)
        {
            return key.All(k => _leaseKeys.Contains(k));
        }
    }
    public void UpdateLeases(LeaseResponse response)
    {
        lock (_leaseKeys)
        {
            _leaseKeys.Clear();
            _leaseKeys.AddRange(response.Leases.Where(l => l.TransactionManagerId == _identifier).SelectMany(l => l.Keys));
        }

        Console.WriteLine("Updated leases: {0}", string.Join(", ", _leaseKeys));
    }

    public async Task WaitLeaseAsync(List<string> keys)
    {
        while (!HasLease(keys))
        {
            await Task.Delay(200);
        }

        return;
    }
    public void OnLeasesChanged(object sender, LeaseEventArgs args)
    {
        UpdateLeases(args.Response);
    }

    public void Shutdown()
    {
        foreach (var channel in _channels)
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
