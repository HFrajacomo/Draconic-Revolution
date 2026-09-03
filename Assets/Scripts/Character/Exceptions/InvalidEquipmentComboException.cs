using System;

public class InvalidEquipmentComboException : Exception
{
    public InvalidEquipmentComboException(string message) : base(message) { }
}