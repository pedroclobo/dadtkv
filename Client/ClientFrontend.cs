using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;
using Utils.ConfigurationParser;

namespace Client;
public class ClientFrontend : Frontend<DADTKVClientService.DADTKVClientServiceClient>
{
    private string _identifier;
    private ConfigurationParser _parser;
    private int _TM;

    public ClientFrontend(string identifier, Dictionary<string, Uri> serverURLs, ConfigurationParser parser) : base(serverURLs)
    {
        _identifier = identifier;
        _parser = parser;
        _TM = HashString(_identifier) % GetClientCount();
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

            var response = await GetClient(_TM).TxSubmitAsync(request);

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
            foreach (var pair in GetClients())
            {
                var client = pair.Item2;
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

    public string GetTM()
    {
        return _parser.TransactionManagerIdentifiers()[_TM];
    }

    private int HashString(string s)
    {
        int hash = 0;
        foreach (char c in s)
        {
            hash += (hash * 31) + c;
        }
        return Math.Abs(hash);
    }
}
