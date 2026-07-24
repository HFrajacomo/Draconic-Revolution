using System;

public class MainInventoryNotFoundException : Exception
{
    public MainInventoryNotFoundException(string message) : base(message) { }
}