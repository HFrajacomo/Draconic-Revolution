using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTONLY : MonoBehaviour
{
	public ushort integer;
	
    // Start is called before the first frame update
    void Start()
    {
        VoxelMetadata vmd = new VoxelMetadata();

        print(vmd.GetMetadata(0,2,0).ToString());
        vmd.metadata[0,2,0].hp = 10;
        vmd.GetMetadata(0,2,0).InitStorage();
        vmd.metadata[0,2,0].storage.Add("test", 1);
        vmd.metadata[0,2,0].storage.Add("item", 0);
        print(vmd.GetMetadata(0,2,0).ToString());
    }
}
