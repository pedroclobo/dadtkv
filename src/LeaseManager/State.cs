namespace LeaseManager;
public class State
{
    private Queue<Lease> _data;

    public State()
    {
        _data = new();
    }

    // TODO: filter duplicate requests
    public void AddLease(string tid, List<string> keys)
    {
        lock (_data)
        {
            _data.Enqueue(new Lease { TransactionManagerId = tid, Keys = { keys } });
        }
    }

    public List<Lease> GetLeases()
    {
        List<Lease> leases = new List<Lease>();
        List<string> keys = new();

        lock (_data)
        {
            if (_data.Count == 0)
            {
                return leases;
            }

            Lease lease = _data.Dequeue();
            leases.Add(lease);
            keys.AddRange(lease.Keys);

            while (_data.Count > 0)
            {
                List<string> leaseKeys = _data.Peek().Keys.ToList();
                if (keys.Intersect(leaseKeys).Any())
                {
                    break;
                }

                leases.Add(_data.Dequeue());
                keys.AddRange(leases.Last().Keys);
            }
        }

        return leases;
    }

    public override string ToString()
    {
        lock (_data)
        {
            return String.Join(", ", _data.Select(lease => $"{lease.TransactionManagerId}: {string.Join(", ", lease.Keys)}"));
        }
    }
}
