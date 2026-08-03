using System;

public class UshortLimitException : Exception
{
    public UshortLimitException(string message) : base(message) { }
}