﻿namespace Utils;

public class FailureDetector
{
    private string _identifier;
    private int _currentTimeSlot;
    private Dictionary<int, List<string>> _suspected;
    private List<string> _faulty;
    private int? _faultyTimeslot; // timeslot in which the current process fails

    public FailureDetector(string identifier, ConfigurationParser parser)
    {
        _identifier = identifier;
        _currentTimeSlot = 1;

        _suspected = new();
        _faultyTimeslot = null;
        for (int slot = 1; slot <= parser.TimeSlots; slot++)
        {
            _suspected[slot] = parser.Suspected(_identifier, slot);
            if (parser.Failed(_identifier, slot))
            {
                _faultyTimeslot = slot;
            }
        }

        _faulty = new();
    }

    public void SetTimeSlot(int slot)
    {
        _currentTimeSlot = slot;
    }

    public bool Suspected(string identifier)
    {
        return _suspected[_currentTimeSlot].Contains(identifier);
    }

    public void AddFailed(string identifier)
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
}
