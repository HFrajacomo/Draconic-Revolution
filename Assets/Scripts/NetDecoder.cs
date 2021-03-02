using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetDecoder
{
	private static byte[] floatBuffer = new byte[4];


	public static ushort ReadUshort(byte[] data, int pos){
		ushort result = data[pos];
		result = (ushort)(result << 8);
		result += data[pos+1];

		return result;
	}

	public static short ReadShort(byte[] data, int pos){
		short result = data[pos];
		result = (short)(result << 8);
		result += data[pos+1];

		return result;
	}

	public static int ReadInt(byte[] data, int pos){
		int result = data[pos];
		result = result << 8;
		result += data[pos+1];
		result = result << 8;
		result += data[pos+2];
		result = result << 8;
		result += data[pos+3];

		return result;
	}

	public static uint ReadUint(byte[] data, int pos){
		uint result = data[pos];
		result = result << 8;
		result += data[pos+1];
		result = result << 8;
		result += data[pos+2];
		result = result << 8;
		result += data[pos+3];

		return (uint)result;
	}

	public static long ReadLong(byte[] data, int pos){
		long result = data[pos];
		result = result << 8;
		result += data[pos+1];
		result = result << 8;
		result += data[pos+2];
		result = result << 8;
		result += data[pos+3];
		result = result << 8;
		result += data[pos+4];
		result = result << 8;
		result += data[pos+5];
		result = result << 8;
		result += data[pos+6];
		result = result << 8;
		result += data[pos+7];

		return result;
	}

	public static string ReadString(byte[] data, int pos, int size){
		string result = Convert.ToBase64String(data, pos, size);
		return result;
	}

	public static ChunkPos ReadChunkPos(byte[] data, int pos){
		return new ChunkPos(NetDecoder.ReadInt(data, pos), NetDecoder.ReadInt(data, pos+4));
	}

	public static float ReadFloat(byte[] data, int pos){
		float result = System.BitConverter.ToSingle(data, pos);
		return result;
	}

	public static void WriteFloat(float a, byte[] data, int pos){
		NetDecoder.floatBuffer = BitConverter.GetBytes(a);

		data[pos] = floatBuffer[0];
		data[pos+1] = floatBuffer[1];
		data[pos+2] = floatBuffer[2];
		data[pos+3] = floatBuffer[3];
	}

	public static void WriteUshort(ushort a, byte[] data, int pos){
		data[pos] = (byte)(a << 8);
		data[pos+1] = (byte)a;
	}

	public static void WriteShort(short a, byte[] data, int pos){
		data[pos] = (byte)(a << 8);
		data[pos+1] = (byte)a;
	}

	public static void WriteInt(int a, byte[] data, int pos){
		data[pos] = (byte)(a << 24);
		data[pos+1] = (byte)(a << 16);
		data[pos+2] = (byte)(a << 8);
		data[pos+3] = (byte)a;
	}

	public static void WriteUint(uint a, byte[] data, int pos){
		data[pos] = (byte)(a << 24);
		data[pos+1] = (byte)(a << 16);
		data[pos+2] = (byte)(a << 8);
		data[pos+3] = (byte)a;
	}

	public static void WriteLong(long a, byte[] data, int pos){
		data[pos] = (byte)(a << 56);
		data[pos+1] = (byte)(a << 48);
		data[pos+2] = (byte)(a << 40);
		data[pos+3] = (byte)(a << 32);
		data[pos+4] = (byte)(a << 24);
		data[pos+5] = (byte)(a << 16);
		data[pos+6] = (byte)(a << 8);
		data[pos+7] = (byte)a;
	}

	public static void WriteString(string a, byte[] data, int pos){
		for(int i=0; i < a.Length; i++){
			data[pos+i] = (byte)a[i];
		}
	}

	public static void WriteChunkPos(ChunkPos cp, byte[] data, int pos){
		NetDecoder.WriteInt(cp.x, data, pos);
		NetDecoder.WriteInt(cp.z, data, pos+4);
	}
}
