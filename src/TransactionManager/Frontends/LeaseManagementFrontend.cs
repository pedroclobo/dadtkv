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
            var request = new LeaseReleaseMessage
            {
                SenderId = _identifier,
                Keys = { keys },
            };

            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                if (!_failureDetector.CanContact(request.SenderId))
                {
                    Console.WriteLine($"Skipping lease release to {identifier}");
                }
                else
                {
                    Console.WriteLine($"Sending lease release request for keys {string.Join(", ", keys)} to {identifier}");
                    try
                    {
                        client.ReleaseLeaseAsync(request);
                    }
                    catch (Grpc.Core.RpcException)
                    {
                        Console.WriteLine($"Failed to send lease release for keys {string.Join(", ", keys)} to {identifier}, marking it as faulty");
                        _failureDetector.AddFaulty(identifier);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
