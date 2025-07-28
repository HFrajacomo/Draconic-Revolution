using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Wrapper<T>{
	public T[] data;

	public Wrapper(T[] data){
		this.data = data;
	}

	public Wrapper(T data){
		this.data = new T[]{data};
	}
}