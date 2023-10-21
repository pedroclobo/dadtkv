using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class LeaseManagementFrontend : Frontend<LeaseManagementService.LeaseManagementServiceClient>
{
    private string _identifier;
    private FailureDetector _failureDetector;

    public LeaseManagementFrontend(string identifier, Dictionary<string, Uri> transactionManagerUrls, FailureDetector failureDetector) : base(transactionManagerUrls)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;
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

                if (_failureDetector.Faulty(request.SenderId) || _failureDetector.Suspected(request.SenderId))
                {
                    Console.WriteLine($"Skipping lease release to {identifier}");
                }
                else
                {
                    Console.WriteLine($"Releasing leases for keys {string.Join(", ", keys)}");
                    client.ReleaseLeaseAsync(request);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
