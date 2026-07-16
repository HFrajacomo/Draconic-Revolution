using System;

public class DeserializationErrorException : Exception
{
    public DeserializationErrorException(string message) : base(message) { }
}