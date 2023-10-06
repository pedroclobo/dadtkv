namespace LeaseManager;
public class State
{
    private Dictionary<string, Queue<List<string>>> _data;

    public State()
    {
        _data = new Dictionary<string, Queue<List<string>>>();
    }

    public void AddLease(string tid, List<string> keys)
    {
        if (!_data.ContainsKey(tid))
        {
            _data.Add(tid, new Queue<List<string>>());
        }
        _data[tid].Enqueue(keys);
    }
    public List<string> GetLease(string tid)
    {
        if (!_data.ContainsKey(tid))
        {
            throw new Exception("Transaction Manager not found: " + tid);
        }
        return _data[tid].Peek();
    }

    // TODO: handle conflicts
    public List<Lease> GetLeases()
    {
        List<Lease> leases = new List<Lease>();

        foreach (var tid in _data.Keys)
        {
            if (_data[tid].Count > 0)
            {
                leases.Add(new Lease { TransactionManagerId = tid, Keys = { _data[tid].Peek() } });
                _data[tid].Dequeue();
            }
        }

        return leases;
    }
}
