using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

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

	public static ulong ReadUlong(byte[] data, int pos){
		ulong result = data[pos];
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
		string result = System.Text.Encoding.UTF8.GetString(data, pos, size);
		return result;
	}

	public static ChunkPos ReadChunkPos(byte[] data, int pos){
		return new ChunkPos(NetDecoder.ReadInt(data, pos), NetDecoder.ReadInt(data, pos+4), NetDecoder.ReadByte(data, pos+8));
	}

	public static Attribute ReadAttribute(byte[] data, int pos){
		return new Attribute(NetDecoder.ReadShort(data, pos), NetDecoder.ReadFloat(data, pos+2));
	}

	public static DepletableAttribute ReadDepletableAttribute(byte[] data, int pos){
		return new DepletableAttribute(NetDecoder.ReadUshort(data, pos), NetDecoder.ReadUshort(data, pos+2), NetDecoder.ReadBool(data, pos+4));
	}


	public static SkillExp ReadSkillEXP(byte[] data, int pos){
		return new SkillExp(data[pos], NetDecoder.ReadInt(data, pos+1));
	}

	public static ClothingInfo ReadClothingInfo(byte[] data, int pos){
		return new ClothingInfo(NetDecoder.ReadUshort(data, pos), NetDecoder.ReadRGB(data, pos+2), NetDecoder.ReadRGB(data, pos+14), NetDecoder.ReadRGB(data, pos+26), NetDecoder.ReadBool(data, pos+38));
	}

	public static Color ReadRGB(byte[] data, int pos){
		return new Color(System.BitConverter.ToSingle(data, pos), System.BitConverter.ToSingle(data, pos+4), System.BitConverter.ToSingle(data, pos+8));
	}

	public static CharacterAppearance ReadCharacterAppearance(byte[] data, int pos){
		return new CharacterAppearance((Race)data[pos], NetDecoder.ReadRGB(data, pos+1), NetDecoder.ReadClothingInfo(data, pos+13), NetDecoder.ReadClothingInfo(data, pos+52), NetDecoder.ReadClothingInfo(data, pos+91), NetDecoder.ReadClothingInfo(data, pos+130));
	}

	public static SpecialEffect ReadSpecialEffect(byte[] data, int pos){
		return new SpecialEffect((EffectType)NetDecoder.ReadUshort(data, pos), (EffectUsecase)NetDecoder.ReadByte(data, pos+2), NetDecoder.ReadByte(data, pos+3), NetDecoder.ReadUshort(data, pos+4), NetDecoder.ReadBool(data, pos+6));
	}

	public static float ReadFloat(byte[] data, int pos){
		float result = System.BitConverter.ToSingle(data, pos);
		return result;
	}

	public static float3 ReadFloat3(byte[] data, int pos){
		return new float3(NetDecoder.ReadFloat(data, pos), NetDecoder.ReadFloat(data, pos+4), NetDecoder.ReadFloat(data, pos+8));
	}

	public static bool ReadBool(byte[] data, int pos){
		if(data[pos] == 0)
			return false;
		return true;
	}

	public static byte ReadByte(byte[] data, int pos){
		return data[pos];
	}

	public static void WriteRGB(Color c, byte[] data, int pos){
		NetDecoder.WriteFloat(c.r, data, pos);
		NetDecoder.WriteFloat(c.g, data, pos+4);
		NetDecoder.WriteFloat(c.b, data, pos+8);
	}

	public static void WriteFloat(float a, byte[] data, int pos){
		NetDecoder.floatBuffer = BitConverter.GetBytes(a);

		data[pos] = floatBuffer[0];
		data[pos+1] = floatBuffer[1];
		data[pos+2] = floatBuffer[2];
		data[pos+3] = floatBuffer[3];
	}

	public static void WriteFloat3(float3 f, byte[] data, int pos){
		NetDecoder.WriteFloat(f.x, data, pos);
		NetDecoder.WriteFloat(f.y, data, pos+4);
		NetDecoder.WriteFloat(f.z, data, pos+8);
	}

	public static void WriteFloat3(float f, float f2, float f3, byte[] data, int pos){
		NetDecoder.WriteFloat(f, data, pos);
		NetDecoder.WriteFloat(f2, data, pos+4);
		NetDecoder.WriteFloat(f3, data, pos+8);
	}

	public static void WriteFloat3(Vector3 v, byte[] data, int pos){
		NetDecoder.WriteFloat(v.x, data, pos);
		NetDecoder.WriteFloat(v.y, data, pos+4);
		NetDecoder.WriteFloat(v.z, data, pos+8);		
	}

	public static void WriteBool(bool a, byte[] data, int pos){
		if(a)
			data[pos] = 1;
		else
			data[pos] = 0;
	}

	public static void WriteUshort(ushort a, byte[] data, int pos){
		data[pos] = (byte)(a >> 8);
		data[pos+1] = (byte)a;
	}

	public static void WriteShort(short a, byte[] data, int pos){
		data[pos] = (byte)(a >> 8);
		data[pos+1] = (byte)a;
	}

	public static void WriteInt(int a, byte[] data, int pos){
		data[pos] = (byte)(a >> 24);
		data[pos+1] = (byte)(a >> 16);
		data[pos+2] = (byte)(a >> 8);
		data[pos+3] = (byte)a;
	}

	public static void WriteUint(uint a, byte[] data, int pos){
		data[pos] = (byte)(a >> 24);
		data[pos+1] = (byte)(a >> 16);
		data[pos+2] = (byte)(a >> 8);
		data[pos+3] = (byte)a;
	}

	public static void WriteLong(long a, byte[] data, int pos){
		data[pos] = (byte)(a >> 56);
		data[pos+1] = (byte)(a >> 48);
		data[pos+2] = (byte)(a >> 40);
		data[pos+3] = (byte)(a >> 32);
		data[pos+4] = (byte)(a >> 24);
		data[pos+5] = (byte)(a >> 16);
		data[pos+6] = (byte)(a >> 8);
		data[pos+7] = (byte)a;
	}

	public static void WriteLong(ulong a, byte[] data, int pos){
		data[pos] = (byte)(a >> 56);
		data[pos+1] = (byte)(a >> 48);
		data[pos+2] = (byte)(a >> 40);
		data[pos+3] = (byte)(a >> 32);
		data[pos+4] = (byte)(a >> 24);
		data[pos+5] = (byte)(a >> 16);
		data[pos+6] = (byte)(a >> 8);
		data[pos+7] = (byte)a;
	}

	public static void WriteByte(byte a, byte[] data, int pos){
		data[pos] = a;
	}

	public static void WriteByteArray(byte[] array, byte[] data, int pos){
		Array.Copy(array, 0, data, pos, array.Length);
	}

	public static void WriteString(string a, byte[] data, int pos){
		for(int i=0; i < a.Length; i++){
			data[pos+i] = (byte)a[i];
		}
	}

	public static void WriteChunkPos(ChunkPos cp, byte[] data, int pos){
		NetDecoder.WriteInt(cp.x, data, pos);
		NetDecoder.WriteInt(cp.z, data, pos+4);
		NetDecoder.WriteByte(cp.y, data, pos+8);
	}

	public static void WriteAttribute(Attribute att, byte[] data, int pos){
		NetDecoder.WriteShort(att.GetBase(), data, pos);
		NetDecoder.WriteFloat(att.GetMultiplier(), data, pos+2);
	}

	public static void WriteDepletableAttribute(DepletableAttribute att, byte[] data, int pos){
		NetDecoder.WriteUshort(att.GetCurrentValue(), data, pos);
		NetDecoder.WriteUshort(att.GetMaximumValue(), data, pos+2);
		NetDecoder.WriteBool(att.GetZeroFlag(), data, pos+4);
	}

	public static void WriteSkillEXP(SkillExp x, byte[] data, int pos){
		NetDecoder.WriteByte(x.GetLevel(), data, pos);
		NetDecoder.WriteInt(x.GetCurrentExp(), data, pos+1);
	}

	public static void WriteClothingInfo(ClothingInfo i, byte[] data, int pos){
		NetDecoder.WriteUshort(i.code, data, pos);
		NetDecoder.WriteRGB(i.primary, data, pos+2);
		NetDecoder.WriteRGB(i.secondary, data, pos+14);
		NetDecoder.WriteRGB(i.terciary, data, pos+26);
		NetDecoder.WriteBool(i.isMale, data, pos+38);
	}

	public static void WriteCharacterAppearance(CharacterAppearance ca, byte[] data, int pos){
		NetDecoder.WriteByte((byte)ca.race, data, pos);
		NetDecoder.WriteRGB(ca.skinColor, data, pos+1);
		NetDecoder.WriteClothingInfo(ca.hat, data, pos+13);
		NetDecoder.WriteClothingInfo(ca.torso, data, pos+52);
		NetDecoder.WriteClothingInfo(ca.legs, data, pos+91);
		NetDecoder.WriteClothingInfo(ca.boots, data, pos+130);
	}

	public static void WriteSpecialEffect(SpecialEffect sfx, byte[] data, int pos){
		NetDecoder.WriteUshort((ushort)sfx.GetEffectType(), data, pos);
		NetDecoder.WriteByte((byte)sfx.GetUsecase(), data, pos+2);
		NetDecoder.WriteByte(sfx.GetTickDuration(), data, pos+3);
		NetDecoder.WriteUshort(sfx.GetTicks(), data, pos+4);
		NetDecoder.WriteBool(sfx.IsSystem(), data, pos+6);
	}
}
