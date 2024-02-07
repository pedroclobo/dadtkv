namespace TransactionManager;

public class LeaseQueue
{
    private string _identifier;
    private Dictionary<string, Queue<string>> _queues;

    public LeaseQueue(string identifier)
    {
        _identifier = identifier;
        _queues = new();
    }

    public void AddLeases(List<Lease> leases)
    {
        lock (_queues)
        {
            foreach (Lease lease in leases)
            {
                foreach (string key in lease.Keys)
                {
                    Push(key, lease.TransactionManagerId);
                }
            }
        }
    }

    public List<string> ReleaseLeases(string releaser, List<string> keys)
    {
        List<string> released = new();

        lock (_queues)
        {
            foreach (string key in keys)
            {
                string popped = Pop(key, releaser);
                if (popped != null)
                {
                    released.Add(popped);
                }
            }
        }

        return released;
    }

    public List<string> LiberateLeases(string liberator, List<string> keys)
    {
        List<string> liberated = new();

        lock (_queues)
        {
            foreach (string key in keys)
            {
                string popped = PopIfNotOnly(key, liberator);
                if (popped != null)
                {
                    liberated.Add(popped);
                }
            }
        }
        return liberated;
    }

    public bool HasLeases(List<string> keys)
    {
        lock (_queues)
        {
            foreach (string key in keys)
            {
                if (!_queues.ContainsKey(key) || _queues[key].Count == 0 || _queues[key].Peek() != _identifier)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override string ToString()
    {
        lock (_queues)
        {
            string res = string.Join(", ", _queues.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));
            return res == "" ? "Empty" : res + '\n';

        }
    }

    private void Push(string key, string holder)
    {
        lock (_queues)
        {
            if (!_queues.ContainsKey(key))
            {
                _queues.Add(key, new());
            }

            _queues[key].Enqueue(holder);
        }
    }

    private string Pop(string key, string holder)
    {
        lock (_queues)
        {
            if (_queues.ContainsKey(key))
            {
                if (_queues[key].Dequeue() != holder)
                {
                    throw new Exception("Invalid lease release");
                }
                else
                {
                    return key;
                }
            }

            return null;
        }
    }

    private string PopIfNotOnly(string key, string value)
    {
        lock (_queues)
        {
            if (_queues.ContainsKey(key))
            {
                if (_queues[key].Peek() != value)
                {
                    throw new Exception("Invalid lease release");
                }
                else if (_queues[key].Count > 1)
                {
                    _queues[key].Dequeue();
                    return key;
                }
            }

            return null;
        }
    }
}
