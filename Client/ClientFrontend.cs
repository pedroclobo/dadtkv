using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;

namespace Client;
public class ClientFrontend : Frontend<DADTKVClientService.DADTKVClientServiceClient>
{
    private string _identifier;
    private FailureDetector _failureDetector;
    private int _tmIndex;
    private List<string> _transactionManagerIdentifiers;

    public ClientFrontend(string identifier, Dictionary<string, Uri> transactionManagerUrls, FailureDetector failureDetector) : base(transactionManagerUrls)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;
        _transactionManagerIdentifiers = transactionManagerUrls.Keys.ToList();
        _transactionManagerIdentifiers.Sort();
        _tmIndex = HashString(_identifier) % GetClientCount();
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

            var response = await GetClient(_tmIndex).TxSubmitAsync(request);

            foreach (var value in response.Values)
            {
                result.Add(DadInteger.FromProtobuf(value));
            }

            return result;
        }
        catch (Grpc.Core.RpcException e)
        {
            Console.WriteLine($"Failed to send request to {GetTM()}, marking it as failed");
            _failureDetector.AddFaulty(GetTM());

            switchTM();
            Console.WriteLine($"Retrying with {GetTM()}");

            return await TxSubmit(read, write);
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
        catch (Grpc.Core.RpcException e)
        {
            Console.WriteLine($"Failed to send request to {GetTM()}, marking it as failed");
            _failureDetector.AddFaulty(GetTM());

            switchTM();
            Console.WriteLine($"Retrying with {GetTM()}");

            return await Status();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new List<StatusResponse>();
    }

    public string GetTM()
    {
        return _transactionManagerIdentifiers[_tmIndex];
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

    private void switchTM()
    {
        // TODO: this could loop forever
        while (_failureDetector.Faulty(GetTM()))
        {
            _tmIndex = (_tmIndex + 1) % GetClientCount();
        }
    }
}
