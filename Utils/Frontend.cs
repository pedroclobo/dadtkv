using Grpc.Net.Client;

namespace Utils;

public abstract class Frontend<T>
{
    protected List<GrpcChannel> _channels;
    protected List<T> _clients;

    public Frontend(List<string> clientUrls)
    {
        _channels = new List<GrpcChannel>();
        foreach (var serverURL in clientUrls)
        {
            _channels.Add(GrpcChannel.ForAddress(serverURL));
        }

        _clients = new List<T>();
        foreach (var channel in _channels)
        {
            _clients.Add(CreateClient(channel));
        }
    }

    public abstract T CreateClient(GrpcChannel channel);

    public void Shutdown()
    {
        foreach (var channel in _channels)
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
