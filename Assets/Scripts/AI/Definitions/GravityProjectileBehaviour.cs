using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class GravityProjectileBehaviour : Behaviour
{
    public Vector3 deltaPos;

    public GravityProjectileBehaviour(Transform tr, float3 dir){
        this.SetTransform(tr);
        this.deltaPos = new Vector3(dir.x, dir.y, dir.z);
    }

    public override byte HandleBehaviour(EntityEvent ev){
        this.transform.position += deltaPos * Time.deltaTime;
        this.transform.eulerAngles = this.deltaPos.normalized;

        this.deltaPos = this.deltaPos + (Constants.GRAVITY_VECTOR * Time.deltaTime);

        return 1;
    }
}
