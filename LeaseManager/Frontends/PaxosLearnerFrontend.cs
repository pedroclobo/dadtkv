using Grpc.Net.Client;
using Utils;
using Utils.ConfigurationParser;

namespace LeaseManager.Frontends;
public class PaxosLearnerFrontend : Frontend<PaxosLearnerService.PaxosLearnerServiceClient>
{
    private string _identifier;
    private ConfigurationParser _parser;
    public PaxosLearnerFrontend(string identifier, Dictionary<string, Uri> serverURLs, ConfigurationParser parser) : base(serverURLs)
    {
        _identifier = identifier;
        _parser = parser;
    }

    public void Accepted(AcceptedResponse response)
    {
        try
        {
            foreach (var pair in GetClients())
            {
                var identifier = pair.Item1;
                var client = pair.Item2;

                if (!_parser.Suspected(_identifier).Contains(identifier))
                {
                    Console.WriteLine($"Sending accepted response {response} to {identifier}.");
                    client.Accepted(response);
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
