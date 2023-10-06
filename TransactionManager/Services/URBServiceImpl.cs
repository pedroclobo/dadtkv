using Grpc.Core;

namespace TransactionManager.Services;
public class URBServiceImpl : URBService.URBServiceBase
{
    private string _identifier;
    private State _state;
    public URBServiceImpl(string identifier, State state)
    {
        _identifier = identifier;
        _state = state;
    }
    public override Task<URBResponse> URBDeliver(URBRequest request, ServerCallContext context)
    {
        try
        {
            Console.WriteLine("Received URB broadcast from {0}", request.SenderId);

            // Perform Writes
            lock (_state)
            {
                foreach (var dadInt in request.Write)
                {
                    Console.WriteLine("Setting key {0} to value {1}", dadInt.Key, dadInt.Value);
                    _state.Set(dadInt.Key, dadInt.Value);
                }
            }

            return Task.FromResult(new URBResponse { SenderId = _identifier, UpdateId = request.UpdateId });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new URBResponse { });
    }
}
