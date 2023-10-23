using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TransactionManager.Frontends;
using Utils;

namespace TransactionManager.Services;
public class DADTKVClientServiceImpl : DADTKVClientService.DADTKVClientServiceBase
{
    private string _identifier;
    private State _state;
    private FailureDetector _failureDetector;
    private URBFrontend _urbFrontend;
    private LeaseFrontend _leaseFrontend;
    private LeaseQueue _leaseQueue;
    private LeaseManagementFrontend _leaseManagementFrontend;

    public DADTKVClientServiceImpl(string identifier, State state, FailureDetector failureDetector, URBFrontend urbFrontend, LeaseFrontend leaseFrontend, LeaseManagementFrontend leaseManagementFrontend, LeaseQueue leaseQueue)
    {
        _identifier = identifier;
        _state = state;
        _failureDetector = failureDetector;
        _urbFrontend = urbFrontend;
        _leaseFrontend = leaseFrontend;
        _leaseManagementFrontend = leaseManagementFrontend;
        _leaseQueue = leaseQueue;
    }

    public async override Task<TxSubmitResponse> TxSubmit(TxSubmitRequest request, ServerCallContext context)
    {
        try
        {
            // Gather all keys used in request
            var keys = request.Read.ToList();
            keys.AddRange(request.Write.Select(dadInt => dadInt.Key));

            Console.WriteLine("Received request {0}", request);

            // Ask for leases if needed
            lock (_leaseQueue)
            {
                if (!_leaseQueue.HasLeases(keys))
                {
                    _leaseFrontend.RequestLease(keys);
                }
            }

            // TODO: Wait for key leases
            // Implement condition variable
            while (!_leaseQueue.HasLeases(keys))
            {
                await Task.Delay(100);
            }

            lock (_leaseQueue)
            {
                // Only propagate write operations
                // TODO: await
                if (request.Write.Count > 0)
                {
                    _urbFrontend.URBDeliver(request);
                }

                // Perform Writes
                foreach (var dadInt in request.Write)
                {
                    _state.Set(dadInt.Key, dadInt.Value);
                }

                // Perform Reads
                var values = new List<DadInt>();
                foreach (var key in request.Read)
                {
                    values.Add(new DadInt { Key = key, Value = _state.Get(key) });
                }

                // Liberate leases if someone else wants them
                List<string> liberatedLeases = _leaseQueue.LiberateLeases(_identifier, keys);
                if (liberatedLeases.Count > 0)
                {
                    _leaseManagementFrontend.ReleaseLeases(liberatedLeases);
                }

                Console.WriteLine("Sending reply to client");

                return new TxSubmitResponse { Values = { values } };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new TxSubmitResponse();
    }

    public override Task<StatusResponse> Status(Empty request, ServerCallContext context)
    {
        try
        {
            return Task.FromResult(new StatusResponse
            {
                ServerId = _identifier,
                Values = { _state.ToDadInt() },
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new StatusResponse
        {
            ServerId = _identifier,
            Values = { new List<DadInt>() },
        });
    }
}
