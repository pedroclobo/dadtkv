using Grpc.Core;
using Utils;

namespace TransactionManager.Services;
public class URBServiceImpl : URBService.URBServiceBase
{
    private string _identifier;
    private State _state;
    private FailureDetector _failureDetector;
    public URBServiceImpl(string identifier, State state, FailureDetector failureDetector)
    {
        _identifier = identifier;
        _state = state;
        _failureDetector = failureDetector;
    }
    public override Task<URBResponse> URBDeliver(URBRequest request, ServerCallContext context)
    {
        try
        {
            if (!_failureDetector.CanContact(request.SenderId))
            {
                Console.WriteLine($"Ignoring URB broadcast from {request.SenderId}");
                return Task.FromResult(new URBResponse { SenderId = _identifier });
            }

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

            return Task.FromResult(new URBResponse { SenderId = _identifier });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Task.FromResult(new URBResponse { });
    }
}
