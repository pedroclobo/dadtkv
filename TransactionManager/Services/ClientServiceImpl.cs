using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TransactionManager.Frontends;

namespace TransactionManager.Services;
public class DADTKVClientServiceImpl : DADTKVClientService.DADTKVClientServiceBase
{
    private State _state;
    private URBFrontend _urbFrontend;
    private LeaseFrontend _leaseFrontend;
    public DADTKVClientServiceImpl(State state, URBFrontend urbFrontend, LeaseFrontend leaseFrontend)
    {
        _state = state;
        _urbFrontend = urbFrontend;
        _leaseFrontend = leaseFrontend;
    }
    public async override Task<TxSubmitResponse> TxSubmit(TxSubmitRequest request, ServerCallContext context)
    {
        try
        {
            // TODO: check for lease and ask for lease

            // Perform Writes
            foreach (var dadInt in request.Write)
            {
                _state.Set(dadInt.Key, dadInt.Value);
            }

            // Perform Reads
            var values = new List<DadInt>();
            foreach (var key in request.Read)
            {
                values.Add(new DadInt { Key = key, Value = _state.Get(key) });
            }

            // Only propagate write operations
            if (request.Write.Count > 0)
            {
                await _urbFrontend.URBDeliver(request);
            }

            Console.WriteLine("Sending reply to client");

            return new TxSubmitResponse { Values = { values } };
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new TxSubmitResponse();
    }
    public override Task<StatusResponse> Status(Empty request, ServerCallContext context)
    {
        return base.Status(request, context);
    }
}
