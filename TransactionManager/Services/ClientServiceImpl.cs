using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace TransactionManager.Services;
public class DADTKVClientServiceImpl : DADTKVClientService.DADTKVClientServiceBase
{
    public override Task<StatusResponse> Status(Empty request, ServerCallContext context)
    {
        return base.Status(request, context);
    }

    public override Task<TxSubmitResponse> TxSubmit(TxSubmitRequest request, ServerCallContext context)
    {
        return base.TxSubmit(request, context);
    }
}
