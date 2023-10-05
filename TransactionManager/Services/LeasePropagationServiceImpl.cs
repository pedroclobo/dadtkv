using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace TransactionManager.Services;
public class LeasePropagationServiceImpl : LeasePropagationService.LeasePropagationServiceBase
{
    public event EventHandler<LeaseEventArgs>? LeasesChanged;
    public LeasePropagationServiceImpl() { }

    public override Task<Empty> DeliverLease(LeaseResponse request, ServerCallContext context)
    {
        OnLeasesChanged(request);

        Console.WriteLine("Received lease response: {0}", request);

        return Task.FromResult(new Empty());
    }

    protected virtual void OnLeasesChanged(LeaseResponse response)
    {
        LeasesChanged?.Invoke(this, new LeaseEventArgs(response));
    }
}

public class LeaseEventArgs : EventArgs
{
    public LeaseResponse Response;

    public LeaseEventArgs(LeaseResponse response)
    {
        Response = response;
    }
}
