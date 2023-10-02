using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace TransactionManager.Services;
public class DADTKVClientServiceImpl : DADTKVClientService.DADTKVClientServiceBase
{
    private State _state;
    public DADTKVClientServiceImpl()
    {
        _state = new State();
    }
    public override Task<TxSubmitResponse> TxSubmit(TxSubmitRequest request, ServerCallContext context)
    {
        try
        {
            // Perform Writes
            foreach (var dadInt in request.Write)
            {
                _state.Add(dadInt.Key, dadInt.Value);
            }

            // Perform Reads
            var values = new List<DadInt>();
            foreach (var key in request.Read)
            {
                values.Add(new DadInt { Key = key, Value = _state.Get(key) });
            }

            return Task.FromResult(new TxSubmitResponse { Values = { values } });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new TxSubmitResponse());
    }
    public override Task<StatusResponse> Status(Empty request, ServerCallContext context)
    {
        return base.Status(request, context);
    }
}
