using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void OnPlaceDelegate(int a, int b);


public class TESTONLY : MonoBehaviour
{
	public void Func(int a, int b){print(a+b);}
	public void Sub(int a, int b){print(a-b);}

	void Start(){
		OnPlaceDelegate opd = Func;
		OnPlaceDelegate opa = Sub;
		TestClass t = new TestClass(opd, opa);
		t.OnPlace(1,2);
	}
}

public class TestClass
{
    // Define a delegate type for methods that take a string parameter
    public OnPlaceDelegate del;

    // Constructor that accepts a delegate
    public TestClass(OnPlaceDelegate delegat, OnPlaceDelegate dele)
    {
        del += delegat;
        del += dele;
    }

    // Function with the same signature as the delegate that invokes the delegate
    public void OnPlace(int a, int b)
    {
        del(a,b);
    }
}


