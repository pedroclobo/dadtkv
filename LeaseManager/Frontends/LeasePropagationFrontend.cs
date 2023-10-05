using Grpc.Net.Client;

namespace LeaseManager.Frontends;
public class LeasePropagationFrontend
{
    private List<GrpcChannel> _channels;
    private List<LeasePropagationService.LeasePropagationServiceClient> _clients;

    public LeasePropagationFrontend(List<string> serverURLs)
    {
        _channels = new List<GrpcChannel>();
        foreach (var serverURL in serverURLs)
        {
            _channels.Add(GrpcChannel.ForAddress(serverURL));
        }

        _clients = new List<LeasePropagationService.LeasePropagationServiceClient>();
        foreach (var channel in _channels)
        {
            _clients.Add(new LeasePropagationService.LeasePropagationServiceClient(channel));
        }
    }
    public void BroadcastLeases(int epoch, List<Lease> leases)
    {
        LeaseResponse response = new LeaseResponse
        {
            Epoch = epoch,
            Leases = { leases },
        };

        Console.WriteLine("Broadcasting leases: {0}", response);

        foreach (var client in _clients)
        {
            client.DeliverLease(response);
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
