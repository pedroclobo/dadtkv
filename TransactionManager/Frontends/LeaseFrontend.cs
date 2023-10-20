using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using System.Runtime.CompilerServices;
using TransactionManager.Services;
using Utils;

namespace TransactionManager.Frontends;
public class LeaseFrontend : Frontend<LeaseService.LeaseServiceClient>
{
    private string _identifier;

    public LeaseFrontend(string identifier, Dictionary<string, Uri> leaseManagerUrls) : base(leaseManagerUrls)
    {
        _identifier = identifier;
    }

    public override LeaseService.LeaseServiceClient CreateClient(GrpcChannel channel)
    {
        return new LeaseService.LeaseServiceClient(channel);
    }

    public void RequestLease(List<string> keys)
    {
        try
        {
            var request = new LeaseRequest
            {
                TransactionManagerId = _identifier,
                Keys = { keys },
            };

            List<Task<Empty>> tasks = new List<Task<Empty>>();
            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                Console.WriteLine($"Requesting lease for keys {string.Join(", ", keys)} to {identifier}");
                client.RequestLeaseAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
