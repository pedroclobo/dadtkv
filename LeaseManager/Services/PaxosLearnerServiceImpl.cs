using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Utils.ConfigurationParser;

namespace LeaseManager.Services;

public class PaxosLearnerServiceImpl : PaxosLearnerService.PaxosLearnerServiceBase
{
    private string _identifier;
    private Dictionary<int, int> _acknowledgments;
    private Dictionary<int, List<Lease>> _values;
    private int _majority;
    private ConfigurationParser _parser;
    public PaxosLearnerServiceImpl(string identifier, int numberReplicas, ConfigurationParser parser)
    {
        _acknowledgments = new();
        _values = new();
        _majority = (int)Math.Floor((double)numberReplicas / 2);
        _parser = parser;
        _identifier = identifier;
    }

    public List<Lease> Value(int timeSlot)
    {
        List<Lease> value = _values[timeSlot];
        _values.Remove(timeSlot);

        return value;
    }

    public override Task<Empty> Accepted(AcceptedResponse request, ServerCallContext context)
    {
        try
        {
            if (_parser.Suspected(_identifier).Contains(request.SenderId))
            {
                Console.WriteLine($"Ignoring accepted response from {request.SenderId}");
                return Task.FromResult(new Empty());
            }

            int timestamp = request.Timestamp;

            // Value already accepted
            if (_values.ContainsKey(timestamp))
            {
                return Task.FromResult(new Empty());
            }

            if (!_acknowledgments.ContainsKey(timestamp))
            {
                _acknowledgments.Add(timestamp, 0);
            }
            _acknowledgments[timestamp]++;

            // Accept consensus value
            if (_acknowledgments[timestamp] >= _majority)
            {
                _acknowledgments.Remove(timestamp);
                if (!_values.ContainsKey(timestamp))
                {
                    _values.Add(timestamp, request.Value.ToList());
                }
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
