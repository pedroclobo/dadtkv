using Grpc.Core;

namespace LeaseManager.Services;

public class PaxosServiceImpl: PaxosService.PaxosServiceBase
{
    private int _readTimestamp;
    private int _writeTimestamp;
    private List<Lease> _highestAccept; // TODO: create c# class

    private State _state;
    public PaxosServiceImpl(State state) {
        _readTimestamp = 0;
        _writeTimestamp = 0;
        _highestAccept = new List<Lease>();

        _state = state;
    }
    public override Task<PromiseResponse> Prepare(PrepareRequest request, ServerCallContext context)
    {
        Console.WriteLine("Received prepare request: {0}", request);

        if (request.Timestamp > _readTimestamp)
        {
            _readTimestamp = request.Timestamp;
            var response = new PromiseResponse { Timestamp = request.Timestamp, Value = { _highestAccept } };

            Console.WriteLine("Sending promise response: {0}", response);

            return Task.FromResult(response);
        }

        // TODO: ignore otherwise
        return base.Prepare(request, context);
    }

    public override Task<AcceptedResponse> Accept(AcceptRequest request, ServerCallContext context)
    {
        Console.WriteLine("Received accept request: {0}", request);

        if (request.Timestamp > _writeTimestamp)
        {
            _writeTimestamp = request.Timestamp;
            _highestAccept = request.Value.ToList();
            var response = new AcceptedResponse { Timestamp = request.Timestamp, Value = { _highestAccept } };

            Console.WriteLine("Sending accepted response: {0}", response);

            return Task.FromResult(response);
        }


        // TODO: ignore otherwise
        return base.Accept(request, context);
    }
}
