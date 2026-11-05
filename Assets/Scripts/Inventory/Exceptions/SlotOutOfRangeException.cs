using System;

public class SlotOutOfRangeException : Exception
{
    public SlotOutOfRangeException(string message) : base(message) { }
}