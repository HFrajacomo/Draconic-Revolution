using System;

public class ByteLimitException : Exception
{
    public ByteLimitException(string message) : base(message) { }
}