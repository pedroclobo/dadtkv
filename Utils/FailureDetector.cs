using Google.Protobuf.Collections;
using Grpc.Core;

namespace Utils;

public class FailureDetector
{
    private string _identifier;
    private int _currentTimeSlot;
    private Dictionary<(int, string), List<string>> _suspected;
    private List<string> _faulty;
    private int? _faultyTimeslot; // timeslot in which the current process fails
    private List<string> _leaseManagerIdentifiers;

    public FailureDetector(string identifier, ConfigurationParser parser)
    {
        _identifier = identifier;
        _currentTimeSlot = 1;

        _leaseManagerIdentifiers = parser.LeaseManagerIdentifiers();
        _leaseManagerIdentifiers.Sort();

        _suspected = new();
        _faultyTimeslot = null;

        List<string> servers = parser.TransactionManagerIdentifiers();
        servers.AddRange(parser.LeaseManagerIdentifiers());

        for (int slot = 1; slot <= parser.TimeSlots; slot++)
        {
            foreach (var id in servers)
            {
                _suspected[(slot, id)] = parser.Suspected(id, slot);
                if (id == _identifier && parser.Failed(_identifier, slot))
                {
                    _faultyTimeslot = slot;
                }
            }
        }

        _faulty = new();
    }

    public void SetTimeSlot(int slot)
    {
        _currentTimeSlot = slot;
    }

    public bool CanContact(string identifier)
    {
        return !_suspected[(_currentTimeSlot, _identifier)].Contains(identifier) && !_suspected[(_currentTimeSlot, identifier)].Contains(_identifier) && !Faulty(identifier);
    }

    public void AddFaulty(string identifier)
    {
        _faulty.Add(identifier);
    }

    public bool Faulty(string identifier)
    {
        return _faulty.Contains(identifier);
    }

    public bool AmIFaulty()
    {
        return _currentTimeSlot >= _faultyTimeslot;
    }

    public bool AmILeader()
    {
        int index = 0;
        while (_suspected[(_currentTimeSlot, _identifier)].Contains(_leaseManagerIdentifiers[index]))
        {
            index = (index + 1) % _leaseManagerIdentifiers.Count;
        }

        return _identifier == _leaseManagerIdentifiers[index];
    }
}
