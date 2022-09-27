using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
	public void Start(){
		MetalVeinC st = new MetalVeinC();

		ushort[] compressed = Compression.CompressStructureBlocks(st.blocks, printOut:true); 
		ushort[] decompressed = Compression.DecompressStructureBlocks(compressed);

		Debug.Log("Compressed Length: " + compressed.Length);
		Debug.Log("Decompressed Length: " + decompressed.Length);
		Debug.Log("Compression Effectiveness: " + ((float)compressed.Length/decompressed.Length)*100 + "%");
	}
}
