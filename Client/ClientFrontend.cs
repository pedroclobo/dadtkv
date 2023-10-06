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

    public async Task<bool> Status()
    {
        try
        {
            var request = new Empty { };

            foreach (var client in _clients)
            {
                var response = await client.StatusAsync(request);

                if (!response.Status)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }
}
