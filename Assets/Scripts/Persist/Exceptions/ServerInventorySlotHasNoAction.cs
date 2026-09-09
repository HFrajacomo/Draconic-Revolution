using System;

public class ServerInventorySlotHasNoAction : Exception
{
    public ServerInventorySlotHasNoAction(string message) : base(message) { }
}