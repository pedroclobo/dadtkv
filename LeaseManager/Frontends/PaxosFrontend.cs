using Grpc.Net.Client;
using Utils;

namespace LeaseManager.Frontends;
public class PaxosFrontend : Frontend<PaxosService.PaxosServiceClient>
{
    private int _majority;

    private State _state;

    private int _writeTimestamp;
    private List<Lease> _value;

    private LeasePropagationFrontend _leasePropagationFrontend;
    public PaxosFrontend(State state, List<string> serverURLs, LeasePropagationFrontend leasePropagationFrontend) : base(serverURLs)
    {
        _state = state;
        _leasePropagationFrontend = leasePropagationFrontend;

        _writeTimestamp = 1;
        _value = new List<Lease>();

        // Don't count with current process
        _majority = (int)Math.Floor((double)_clients.Count / 2);
    }

    public override PaxosService.PaxosServiceClient CreateClient(GrpcChannel channel)
    {
        return new PaxosService.PaxosServiceClient(channel);
    }

    public async Task Paxos()
    {
        await PrepareDeliver();
        await AcceptDeliver();
        _leasePropagationFrontend.BroadcastLeases(_writeTimestamp, _value);
    }

    public async Task PrepareDeliver()
    {
        PrepareRequest request = new PrepareRequest
        {
            Timestamp = _writeTimestamp,
        };

        Console.WriteLine("Sending prepare request: {0}", request);

        List<Task<PromiseResponse>> tasks = new List<Task<PromiseResponse>>();
        foreach (var client in _clients)
        {
            tasks.Add(Task.Run(() => client.Prepare(request)));
        }

        // Wait for majority of acknowledgements
        while (tasks.Count(t => t.IsCompleted) < _majority)
        {
            Task<PromiseResponse> completedTask = await Task.WhenAny(tasks);
        }

        Console.WriteLine("Received majority of promises");

        // Update write timestamp and value
        var response = tasks.Select(t => t.Result).OrderByDescending(r => r.Timestamp).First();
        if (response.Timestamp > _writeTimestamp)
        {
            _writeTimestamp = response.Timestamp;
            _value = response.Value.ToList();
        }
    }

    public async Task AcceptDeliver()
    {
        lock (_state)
        {
            _value = _state.GetLeases();
        }

        AcceptRequest request = new AcceptRequest
        {
            Timestamp = _writeTimestamp,
            Value = { _value },
        };

        Console.WriteLine("Sending accept request: {0}", request);

        List<Task<AcceptedResponse>> tasks = new List<Task<AcceptedResponse>>();
        foreach (var client in _clients)
        {
            tasks.Add(Task.Run(() => client.Accept(request)));
        }

        // Wait for majority of acknowledgements
        while (tasks.Count(t => t.IsCompleted) < _majority)
        {
            Task<AcceptedResponse> completedTask = await Task.WhenAny(tasks);
        }
        _writeTimestamp++;

        Console.WriteLine("Received majority of accepted");
    }
}
