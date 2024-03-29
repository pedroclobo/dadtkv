﻿using Grpc.Net.Client;
using Utils;

namespace TransactionManager.Frontends;
public class URBFrontend : Frontend<URBService.URBServiceClient>
{
    private string _identifier;
    private int _majority;
    private FailureDetector _failureDetector;

    // Timeout in seconds
    private static int TIMEOUT = 5;

    public URBFrontend(string identifier, Dictionary<string, Uri> serverURLs, FailureDetector failureDetector) : base(serverURLs)
    {
        _identifier = identifier;
        _failureDetector = failureDetector;

        // Don't count with current process
        _majority = (int)Math.Floor((double)GetClients().Count / 2);
    }

    public override URBService.URBServiceClient CreateClient(GrpcChannel channel)
    {
        return new URBService.URBServiceClient(channel);
    }

    public async Task<bool> URBDeliver(TxSubmitRequest request)
    {
        try
        {
            URBRequest urbRequest = new URBRequest
            {
                SenderId = _identifier,
                Write = { request.Write },
            };

            // Send request to propagate state to all servers
            List<Task<URBResponse>> tasks = new List<Task<URBResponse>>();
            foreach (var pair in GetClients())
            {
                string identifier = pair.Item1;
                var client = pair.Item2;

                if (!_failureDetector.CanContact(identifier))
                {
                    Console.WriteLine($"Skipping propagation of {urbRequest} to {identifier}");
                }
                else
                {
                    Console.WriteLine($"Propagating {urbRequest} to {identifier}");
                    try
                    {
                        tasks.Add(Task.Run(() => client.URBDeliver(urbRequest)));
                    }
                    catch (Grpc.Core.RpcException)
                    {
                        Console.WriteLine($"Failed to propagate {urbRequest} to {identifier}, marking it as faulty");
                        _failureDetector.AddFaulty(identifier);
                    }
                }
            }

            // Wait for majority of acknowledgements
            // Abort if timeout is reached
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT)))
            {
                while (tasks.Count(t => t.IsCompleted) < _majority)
                {
                    Task<URBResponse> completedTask = await Task.WhenAny(tasks);
                    if (completedTask.IsCompleted)
                    {
                        Console.WriteLine($"Received ACK from {completedTask.Result.SenderId}");
                    }
                    else
                    {
                        Console.WriteLine($"Timeout reached, aborting");
                        cts.Cancel();
                        return false;
                    }
                }

                Console.WriteLine($"Got majority (#{_majority} ACKs)");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return true;
    }
}
