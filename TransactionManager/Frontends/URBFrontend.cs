using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class URBFrontend : Frontend<URBService.URBServiceClient>
{
    private string _identifier;
    private int _majority;
    private FailureDetector _failureDetector;

    public URBFrontend(string identifier, Dictionary<string, Uri> serverURLs, FailureDetector failureDetector) : base(serverURLs)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;

        // Don't count with current process
        _majority = (int)Math.Floor((double)GetClients().Count / 2);
    }

    public override URBService.URBServiceClient CreateClient(GrpcChannel channel)
    {
        return new URBService.URBServiceClient(channel);
    }

    public async Task URBDeliver(TxSubmitRequest request)
    {
        try
        {
            // TODO: originate updateId, it is hardcoded to 0
            URBRequest urbRequest = new URBRequest
            {
                SenderId = _identifier,
                Write = { request.Write },
            };

            List<Task<URBResponse>> tasks = new List<Task<URBResponse>>();
            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                if (_failureDetector.Faulty(identifier) || _failureDetector.Suspected(identifier))
                {
                    Console.WriteLine($"Skipping propagation of {urbRequest} to {identifier}");
                }
                else
                {
                    Console.WriteLine($"Propagating {urbRequest} to {identifier}");
                    tasks.Add(Task.Run(() => client.URBDeliver(urbRequest)));
                }
            }

            // TODO: timeout
            // Wait for majority of acknowledgements
            while (tasks.Count(t => t.IsCompleted) < _majority)
            {
                Task<URBResponse> completedTask = await Task.WhenAny(tasks);
            }

            var senderIds = tasks.Where(t => t.IsCompleted).Select(t => t.Result.SenderId).ToList();
            foreach (var senderId in senderIds)
            {
                Console.WriteLine($"Received ACK from {senderId}");
            }
            Console.WriteLine($"Got majority (#{_majority} ACKs)");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
