using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace LeaseManager.Services;

public class LeaseServiceImpl : LeaseService.LeaseServiceBase
{
    private State _state;
    public LeaseServiceImpl(State state)
    {
        _state = state;
    }
    public override Task<Empty> RequestLease(LeaseRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine($"Received lease request {request}");
            _state.AddLease(request.TransactionManagerId, request.Keys.ToList());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return Task.FromResult(new Empty());
    }
}
