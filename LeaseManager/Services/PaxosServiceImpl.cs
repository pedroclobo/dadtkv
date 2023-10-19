using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LeaseManager.Frontends;
using Utils.ConfigurationParser;

namespace LeaseManager.Services;

public class PaxosServiceImpl : PaxosService.PaxosServiceBase
{
    private string _identifier;

    private int _readTimestamp;
    private int? _writeTimestamp;
    private List<Lease>? _value; // TODO: create c# class

    private PaxosLearnerFrontend _learnerFrontend;
    private ConfigurationParser _parser;
    public PaxosServiceImpl(string identifier, PaxosLearnerFrontend learnerFrontend, ConfigurationParser parser)
    {
        _identifier = identifier;

        _readTimestamp = 0;
        _writeTimestamp = null;
        _value = null;

        _learnerFrontend = learnerFrontend;
        _parser = parser;
    }
    public override Task<PromiseResponse> Prepare(PrepareRequest request, ServerCallContext context)
    {
        try
        {
            if (_parser.Suspected(_identifier).Contains(request.SenderId))
            {
                Console.WriteLine($"Ignoring prepare request: {request}");
                return Task.FromResult(new PromiseResponse { SenderId = _identifier, HasValue = false, Nack = true }); ;
            }

            Console.WriteLine($"Received prepare request: {request}");

            PromiseResponse response = new PromiseResponse { SenderId = _identifier };

            if (request.Timestamp > _readTimestamp)
            {
                // Update read timestamp
                _readTimestamp = request.Timestamp;

                if (_writeTimestamp != null && _value != null)
                {
                    response.Timestamp = (int)_readTimestamp;
                    response.Value.Add(_value);
                    response.HasValue = true;
                    response.Nack = false;
                }
                else
                {
                    response.Timestamp = request.Timestamp;
                    response.HasValue = false;
                    response.Nack = false;
                }

                Console.WriteLine($"Sending promise response: {response}");
            }
            else
            {
                response.Timestamp = _readTimestamp;
                response.HasValue = false;
                response.Nack = true;

                Console.WriteLine($"Sending NACK to prepare request: {request}");
            }

            return Task.FromResult(response);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new PromiseResponse { });

    }

    // TODO: reset variables if no majority
    public override Task<Empty> Accept(AcceptRequest request, ServerCallContext context)
    {
        try
        {
            if (_parser.Suspected(_identifier).Contains(request.SenderId))
            {
                Console.WriteLine($"Ignoring accept request: {request}");
                return Task.FromResult(new Empty());
            }

            Console.WriteLine($"Received accept request {request}");

            if (request.Timestamp >= _readTimestamp)
            {
                _writeTimestamp = request.Timestamp;
                _value = request.Value.ToList();

                AcceptedResponse response = new AcceptedResponse { Timestamp = request.Timestamp, Value = { _value } };

                // Send accepted to every learner
                _learnerFrontend.Accepted(response);

                // Reset value
                _value = null;
            }
            else
            {
                Console.WriteLine($"Ignoring accept request: {request}");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new Empty());
    }
}
