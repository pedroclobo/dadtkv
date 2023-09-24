using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Utils;

namespace Client;

public class Frontend
{
    private string _identifier;
    private List<GrpcChannel> _serverChannels;
    private List<DADTKVClientService.DADTKVClientServiceClient> _clients;

    public Frontend(string identifier, string serverURLs)
    {
        _identifier = identifier;

        _serverChannels = new List<GrpcChannel>();
        foreach (var serverURL in serverURLs.Split(","))
        {
            _serverChannels.Add(GrpcChannel.ForAddress(serverURL));
        }

        _clients = new List<DADTKVClientService.DADTKVClientServiceClient>();
        foreach (var channel in _serverChannels)
        {
            _clients.Add(new DADTKVClientService.DADTKVClientServiceClient(channel));
        }
    }
    public async Task<List<DadInt>> TxSubmit(List<string> read, List<DadInt> write)
    {
        var request = new TxSubmitRequest
        {
            ClientId = _identifier,
            Read = { read },
            Write = { write }
        };

        var result = new List<DadInt>();
        foreach (var client in _clients)
        {
            var response = await client.TxSubmitAsync(request);

            foreach (var value in response.Values)
            {
                result.Add(value);
            }
        }

        return result;
    }

    public async Task<bool> Status()
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

    public void Shutdown()
    {
        foreach (var channel in _serverChannels)
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
