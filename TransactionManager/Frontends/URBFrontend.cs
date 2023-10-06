using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class URBFrontend : Frontend<URBService.URBServiceClient>
{
    private string _identifier;
    private int _majority;

    public URBFrontend(string identifier, List<Uri> serverURLs) : base(serverURLs)
    {
        _identifier = identifier;

        // Don't count with current process
        _majority = (int)Math.Floor((double)_clients.Count / 2);
    }

    public override URBService.URBServiceClient CreateClient(GrpcChannel channel)
    {
        return new URBService.URBServiceClient(channel);
    }

    public async Task URBDeliver(TxSubmitRequest request)
    {
        try
        {
            var updateId = new UpdateId
            {
                TransactionManagerId = _identifier,
                SequenceNumber = 0,
            };
            var updateIdentifier = UpdateIdentifier.FromProtobuf(updateId);

            // TODO: originate updateId, it is hardcoded to 0
            URBRequest urbRequest = new URBRequest
            {
                SenderId = _identifier,
                UpdateId = updateId,
                Write = { request.Write },
            };

            List<Task<URBResponse>> tasks = new List<Task<URBResponse>>();
            foreach (var client in _clients)
            {
                tasks.Add(Task.Run(() => client.URBDeliver(urbRequest)));
            }

            // Wait for majority of acknowledgements
            while (tasks.Count(t => t.IsCompleted) < _majority)
            {
                Task<URBResponse> completedTask = await Task.WhenAny(tasks);
            }

            var senderIds = tasks.Where(t => t.IsCompleted).Select(t => t.Result.SenderId).ToList();
            foreach (var senderId in senderIds)
            {
                Console.WriteLine("Received ACK from {0}", senderId);
            }
            Console.WriteLine("Got majority (#{0} ACKs)", _majority);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
