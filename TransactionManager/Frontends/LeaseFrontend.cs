using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class LeaseFrontend
{
    private string _identifier;

    private List<GrpcChannel> _channels;
    private List<LeaseService.LeaseServiceClient> _clients;

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
    }
    public void RequestLease(string[] keys)
    {
        var leaseRequest = new LeaseRequest
        {
            TransactionManagerId = _identifier,
            Keys = { keys },
        };

        foreach (var client in _clients)
        {
            client.RequestLease(leaseRequest);
        }
    }

    public void Shutdown()
    {
        foreach (var channel in _channels)
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
