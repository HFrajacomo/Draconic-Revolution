using System;
using UnityEngine;

[Serializable]
public class PolymorphicContainer<T> {
    public string type;
    public string json; // String-like variable

    public T Get(){return (T)JsonUtility.FromJson(this.json, GetClassType());}

    private Type GetClassType(){return Type.GetType(this.type);}
}
