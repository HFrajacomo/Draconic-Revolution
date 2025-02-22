using System.Collections.Generic;
using UnityEngine;

public static class DebugPlane{
    private static GameObject quad = null;

    public static void Draw(Plane plane, List<Vector3> points){
        if(quad != null){
            GameObject.Destroy(quad);
            quad = null;
        }

        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        Vector3 planePoint = FindMiddle(points);
        Quaternion rotation = Quaternion.LookRotation(plane.normal);

        quad.transform.position = planePoint;
        quad.transform.rotation = rotation;

        quad.transform.localScale = new Vector3(5, 5, 1); // Adjust as needed
    }

    private static Vector3 FindMiddle(List<Vector3> points){
        Vector3 aux = new Vector3(0,0,0);

        for(int i=0; i < points.Count; i++){
            aux += points[i];
        }

        aux = aux / points.Count;
        return aux;
    }
}