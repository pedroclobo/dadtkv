using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace TransactionManager.Services;

public class PaxosLearnerServiceImpl : PaxosLearnerService.PaxosLearnerServiceBase
{
    private Dictionary<int, int> _acknowledgments;
    private Dictionary<int, List<Lease>> _values;
    private int _majority;
    // TODO: Add a class to manage the lease queues
    public PaxosLearnerServiceImpl(int numberReplicas)
    {
        _acknowledgments = new();
        _values = new();
        _majority = (int)Math.Ceiling((double)numberReplicas / 2);
    }

    public List<Lease> Value(int timeSlot)
    {
        List<Lease> value = _values[timeSlot];
        _values.Remove(timeSlot);

        return value;
    }

    // TODO: locks
    public override Task<Empty> Accepted(AcceptedResponse request, ServerCallContext context)
    {
        try
        {
            int timestamp = request.Timestamp;

            // Value already accepted
            lock (_values)
            {
                if (_values.ContainsKey(timestamp))
                {
                    return Task.FromResult(new Empty());
                }
            }

            lock (_acknowledgments)
            {
                if (!_acknowledgments.ContainsKey(timestamp))
                {
                    _acknowledgments.Add(timestamp, 0);
                }
                _acknowledgments[timestamp]++;
            }

            if (_acknowledgments[timestamp] == _majority)
            {
                _acknowledgments.Remove(timestamp);
                if (!_values.ContainsKey(timestamp))
                {
                    _values.Add(timestamp, request.Value.ToList());
                }

                _values[timestamp] = request.Value.ToList();

                // TODO: Add the value to the queue

                Console.WriteLine("Received majority of accepted responses: {0}", request);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return Task.FromResult(new Empty());
    }
}
