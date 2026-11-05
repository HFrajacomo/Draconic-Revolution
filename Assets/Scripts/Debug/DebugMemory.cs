using System;
using System.Runtime.CompilerServices;

public static class DebugMemory {
	public static string GetMemoryPosition(object obj){
		unsafe
		{
			TypedReference tr = __makeref(obj);
			IntPtr ptr = **(IntPtr**)&tr;
			return ptr.ToString();
		}
	}

	public static bool SameReference(object a, object b){
		return Object.ReferenceEquals(a, b);
	}

}