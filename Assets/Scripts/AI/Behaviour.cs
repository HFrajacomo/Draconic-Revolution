using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Behaviour
{
    public Transform transform;

    public abstract byte HandleBehaviour(EntityEvent ev);
    public void SetTransform(Transform tr){this.transform = tr;}
}
