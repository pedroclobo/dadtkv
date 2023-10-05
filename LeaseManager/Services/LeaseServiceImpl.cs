using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace LeaseManager.Services;

public class LeaseServiceImpl : LeaseService.LeaseServiceBase
{
    public LeaseServiceImpl() { }

    public override Task<Empty> RequestLease(LeaseRequest request, ServerCallContext context)
    {
        return base.RequestLease(request, context);
    }
}
