using Grpc.Net.Client;

namespace Utils;

public abstract class Frontend<T>
{
    private Dictionary<string, GrpcChannel> _channels;
    private Dictionary<string, T> _clients;

    public Frontend(Dictionary<string, Uri> clientUrls)
    {
        _channels = new Dictionary<string, GrpcChannel>();
        _clients = new Dictionary<string, T>();

        foreach (var pair in clientUrls)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(pair.Value);
            _channels.Add(pair.Key, channel);
            _clients.Add(pair.Key, CreateClient(channel));
        }
    }

    public abstract T CreateClient(GrpcChannel channel);

    public T GetClient(string identifier)
    {
        return _clients[identifier];
    }

    public List<Tuple<string, T>> GetClients()
    {
        List<Tuple<string, T>> clients = new();

        foreach (var pair in _clients)
        {
            clients.Add(new Tuple<string, T>(pair.Key, pair.Value));
        }

        return clients;
    }

    public void Shutdown()
    {
        foreach (var channel in _channels.Values)
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
