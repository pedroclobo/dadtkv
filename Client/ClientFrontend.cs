using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;

namespace Client;
public class ClientFrontend : Frontend<DADTKVClientService.DADTKVClientServiceClient>
{
    private string _identifier;

    public ClientFrontend(string identifier, List<Uri> serverURLs) : base(serverURLs)
    {
        _identifier = identifier;
    }
    public override DADTKVClientService.DADTKVClientServiceClient CreateClient(GrpcChannel channel)
    {
        return new DADTKVClientService.DADTKVClientServiceClient(channel);
    }
    public async Task<List<DadInteger>> TxSubmit(List<string> read, List<DadInteger> write)
    {
        try
        {
            var request = new TxSubmitRequest
            {
                ClientId = _identifier,
                Read = { read },
                Write = { write.Select(d => d.ToProtobuf()) }
            };

            var result = new List<DadInteger>();

            var response = await _clients[0].TxSubmitAsync(request);

            foreach (var value in response.Values)
            {
                result.Add(DadInteger.FromProtobuf(value));
            }

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new List<DadInteger>();
    }

    public async Task<List<StatusResponse>> Status()
    {
        try
        {
            Empty request = new Empty { };

            List<Task<StatusResponse>> tasks = new List<Task<StatusResponse>>();
            foreach (var client in _clients)
            {
                tasks.Add(Task.Run(() => client.Status(request)));
            }

            return (await Task.WhenAll(tasks)).ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new List<StatusResponse>();
    }
}
