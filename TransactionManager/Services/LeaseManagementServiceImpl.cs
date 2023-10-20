using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace TransactionManager.Services;

public class LeaseManagementServiceImpl: LeaseManagementService.LeaseManagementServiceBase
{
    private string _identifier;
    private LeaseQueue _leaseQueue;

    public LeaseManagementServiceImpl(string identifier, LeaseQueue leaseQueue)
    {
        _identifier = identifier;
        _leaseQueue = leaseQueue;
    }

    public override Task<Empty> ReleaseLease(LeaseReleaseMessage request, ServerCallContext context)
    {
        Console.WriteLine($"Received lease release request: {request}");

        lock (_leaseQueue)
        {
            _leaseQueue.ReleaseLeases(request.SenderId, request.Keys.ToList());
        }

        return Task.FromResult(new Empty());
    }
}
