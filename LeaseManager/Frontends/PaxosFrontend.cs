using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Utils;

namespace LeaseManager.Frontends;
public class PaxosFrontend : Frontend<PaxosService.PaxosServiceClient>
{
    private int _majority;

    private string _identifier;
    private State _state;

    private int _sequenceNumber;
    private List<Lease>? _value;
    private List<Lease>? _acceptedValue;

    private FailureDetector _failureDetector;

    private PaxosLearnerFrontend _learnerFrontend;
    public PaxosFrontend(string identifier, State state, Dictionary<string, Uri> serverURLs, Dictionary<string, Uri> tmURls, FailureDetector failureDetector) : base(serverURLs)
    {
        _identifier = identifier;
        _state = state;

        _sequenceNumber = 1;
        _value = null;
        _acceptedValue = null;

        // Don't count with current process
        _majority = (int)Math.Floor((double)GetClients().Count / 2);

        _failureDetector = failureDetector;

        // All TM's and other LM's are learners
        var urls = new Dictionary<string, Uri>(serverURLs);
        foreach (var pair in tmURls)
        {
            urls.Add(pair.Key, pair.Value);
        }
        _learnerFrontend = new PaxosLearnerFrontend(identifier, urls, failureDetector);
    }

    public override PaxosService.PaxosServiceClient CreateClient(GrpcChannel channel)
    {
        return new PaxosService.PaxosServiceClient(channel);
    }

    public async Task Paxos(int timeSlot)
    {
        try
        {
            if (await PrepareDeliver(timeSlot))
            {
                AcceptDeliver(timeSlot);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task<bool> PrepareDeliver(int timeSlot)
    {
        try
        {
            PrepareRequest request = new PrepareRequest
            {
                SenderId = _identifier,
                Timestamp = timeSlot,
            };

            List<Task<PromiseResponse>> tasks = new List<Task<PromiseResponse>>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(10000); // TODO: 10 second timeout

            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                if (_failureDetector.Faulty(identifier) || _failureDetector.Suspected(identifier))
                {
                    Console.WriteLine($"Skipping prepare request to {identifier}");
                    continue;
                }

                Console.WriteLine($"Sending prepare request {request} to {identifier}");

                try
                {
                    tasks.Add(Task.Run(() => client.Prepare(request)));
                } catch (Grpc.Core.RpcException e)
                {
                    Console.WriteLine($"Failed to send prepare request {request} to {identifier}, marking it as faulty");
                    _failureDetector.AddFaulty(identifier);
                }
            }

            // Wait for majority of positive promises
            int positive = 0;
            while (positive < _majority)
            {
                Task<PromiseResponse> completedTask = await Task.WhenAny(tasks);

                Console.WriteLine($"Received promise response {completedTask.Result}");

                if (completedTask.IsCanceled)
                {
                    Console.WriteLine("Prepare request timed out");
                    return false;
                }
                if (completedTask.Result.Nack)
                {
                    Console.WriteLine($"Received NACK {completedTask.Result}");
                    return false;
                }
                else
                {
                    positive++;
                }
            }

            Console.WriteLine("Received majority of promises");

            // Adopt value associated with highest promise (need to check if hasValue is set to true)
            List<PromiseResponse> responses = tasks.Where(t => t.Result.HasValue).Select(t => t.Result).OrderByDescending(r => r.Timestamp).ToList();
            if (responses.Count() > 0 && responses[0].Timestamp > _sequenceNumber)
            {
                Console.WriteLine($"Adopting value from promise: {responses[0]}");
                _sequenceNumber = responses[0].Timestamp;
                _value = responses[0].Value.ToList();
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public void AcceptDeliver(int timeSlot)
    {
        try
        {
            // If there was no value in the promise replys, we choose our own value
            if (_value == null)
            {
                lock (_state)
                {
                    _value = _state.GetLeases();
                }
            }

            AcceptRequest request = new AcceptRequest
            {
                SenderId = _identifier,
                Timestamp = timeSlot,
                Value = { _value },
            };

            List<Task<Empty>> tasks = new();
            foreach (var pair in GetClients())
            {
                var identifier = pair.Item1;
                var client = pair.Item2;

                if (_failureDetector.Faulty(identifier) || _failureDetector.Suspected(identifier))
                {
                    Console.WriteLine($"Skipping sending accept request to {identifier}");
                    continue;
                }
                Console.WriteLine($"Sending accept request {request} to {identifier}.");

                try
                {
                    tasks.Add(Task.Run(() => client.Accept(request)));
                } catch (Grpc.Core.RpcException e)
                {
                    Console.WriteLine($"Failed to send accept request {request} to {identifier}, marking it as faulty");
                    _failureDetector.AddFaulty(identifier);
                }
            }

            // TODO: Update accepted value
            // _acceptedValue = tasks.Select(t => t.Result).OrderByDescending(r => r.Timestamp).First().Value.ToList();

            // Reset value
            _value = null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
