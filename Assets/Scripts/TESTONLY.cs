using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
	private static readonly int size = 100000;
	private HashSet<int> testSet = new HashSet<int>();
	private List<int> testList = new List<int>();
	private int[] testArray = new int[size];
	private bool a;

	private long currentTick;

	private void Start(){

	}
}




public struct TStruct{
	public int a;
	public int b;
	public int c;
	public int d;
	public int e;
	public int f;
	public int here;

	public TStruct(int a, int b, int c, int d, int e, int f, int here){
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
		this.e = e;
		this.f = f;
		this.here = here;
	}

	public TStruct CopyWithLess(int val){
		return new TStruct(this.a, this.b, this.c, this.d, this.e, this.f, this.here + val);
	}
}

public class TestClass{
	private int a;
	private int b;
	private int c;
	private int d;
	private int e;
	private int f;
	private int here;

	public TestClass(int a, int b, int c, int d, int e, int f, int here){
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
		this.e = e;
		this.f = f;
		this.here = here;
	}

	public void Add(int val){
		this.here += val;
	}
}