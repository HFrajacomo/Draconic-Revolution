using System;

public class InventoryActionNotImplementedException : Exception
{
    public InventoryActionNotImplementedException(string message) : base(message) { }
}