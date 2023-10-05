using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace LeaseManager.Services;

public class LeaseServiceImpl : LeaseService.LeaseServiceBase
{
    private State _state;
    public LeaseServiceImpl(State state) {
        _state = state;
    }
    public override Task<Empty> RequestLease(LeaseRequest request, ServerCallContext context)
    {
        _state.AddLease(request.TransactionManagerId, request.Keys.ToList());

        return Task.FromResult(new Empty());
    }
}
