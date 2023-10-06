using Grpc.Net.Client;
using Utils;

namespace LeaseManager.Frontends;
public class LeasePropagationFrontend: Frontend<LeasePropagationService.LeasePropagationServiceClient>
{
    public LeasePropagationFrontend(List<Uri> serverURLs): base(serverURLs) { }

    public override LeasePropagationService.LeasePropagationServiceClient CreateClient(GrpcChannel channel)
    {
        return new LeasePropagationService.LeasePropagationServiceClient(channel);
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
}
