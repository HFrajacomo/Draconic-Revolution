using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;


public class TESTONLY : MonoBehaviour {
	void Start(){
		Debug.Log(JobsUtility.JobWorkerCount);
	}
}