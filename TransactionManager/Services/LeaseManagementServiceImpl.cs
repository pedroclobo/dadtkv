using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Utils;

namespace TransactionManager.Services;

public class LeaseManagementServiceImpl: LeaseManagementService.LeaseManagementServiceBase
{
    private string _identifier;
    private FailureDetector _failureDetector;
    private LeaseQueue _leaseQueue;

    public LeaseManagementServiceImpl(string identifier, FailureDetector failureDetector, LeaseQueue leaseQueue)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;
        _leaseQueue = leaseQueue;
    }

    public override Task<Empty> ReleaseLease(LeaseReleaseMessage request, ServerCallContext context)
    {
        if (_failureDetector.Faulty(request.SenderId) || _failureDetector.Suspected(request.SenderId))
        {
            Console.WriteLine($"Ignoring lease release request: {request}");
            return Task.FromResult(new Empty());
        }

        Console.WriteLine($"Received lease release request: {request}");

        lock (_leaseQueue)
        {
            _leaseQueue.ReleaseLeases(request.SenderId, request.Keys.ToList());
        }

        return Task.FromResult(new Empty());
    }
}
