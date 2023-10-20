using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class LeaseManagementFrontend : Frontend<LeaseManagementService.LeaseManagementServiceClient>
{
    private string _identifier;

    public LeaseManagementFrontend(string identifier, Dictionary<string, Uri> leaseManagerUrls) : base(leaseManagerUrls)
    {
        _identifier = identifier;
    }

    public override LeaseManagementService.LeaseManagementServiceClient CreateClient(GrpcChannel channel)
    {
        return new LeaseManagementService.LeaseManagementServiceClient(channel);
    }

    public void ReleaseLeases(List<string> keys)
    {
        try
        {
            Console.WriteLine($"Releasing leases for keys {string.Join(", ", keys)}");

            var request = new LeaseReleaseMessage
            {
                SenderId = _identifier,
                Keys = { keys },
            };

            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                client.ReleaseLeaseAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
