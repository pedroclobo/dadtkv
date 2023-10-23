using Grpc.Net.Client;
using Utils;

namespace LeaseManager.Frontends;
public class PaxosLearnerFrontend : Frontend<PaxosLearnerService.PaxosLearnerServiceClient>
{
    private string _identifier;
    private FailureDetector _failureDetector;
    public PaxosLearnerFrontend(string identifier, Dictionary<string, Uri> serverURLs, FailureDetector failureDetector) : base(serverURLs)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;
    }

    public void Accepted(AcceptedResponse response)
    {
        try
        {
            foreach (var pair in GetClients())
            {
                var identifier = pair.Item1;
                var client = pair.Item2;

                if (_failureDetector.Faulty(identifier) || _failureDetector.Suspected(identifier))
                {
                    Console.WriteLine($"Skipping sending accepted response to {identifier}");
                    continue;
                }

                Console.WriteLine($"Sending accepted response {response} to {identifier}.");
                try
                {
                    client.Accepted(response);
                }
                catch (Grpc.Core.RpcException e)
                {
                    Console.WriteLine($"Failed to send accept response {response} to {identifier}, marking it as faulty");
                    _failureDetector.AddFaulty(identifier);
                }

            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public override PaxosLearnerService.PaxosLearnerServiceClient CreateClient(GrpcChannel channel)
    {
        return new PaxosLearnerService.PaxosLearnerServiceClient(channel);
    }
}
