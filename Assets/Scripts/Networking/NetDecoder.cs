using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class NetDecoder
{
	private static byte[] floatBuffer = new byte[4];
	private static readonly SpecialEffect NULL_EFFECT = new SpecialEffect(EffectType.NONE);


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

	public static ClothingInfo ReadClothingInfo(byte[] data, int pos){ // Size = 39
		return new ClothingInfo(NetDecoder.ReadUshort(data, pos), NetDecoder.ReadRGB(data, pos+2), NetDecoder.ReadRGB(data, pos+14), NetDecoder.ReadRGB(data, pos+26), NetDecoder.ReadBool(data, pos+38));
	}

	public static Color ReadRGB(byte[] data, int pos){
		return new Color(System.BitConverter.ToSingle(data, pos), System.BitConverter.ToSingle(data, pos+4), System.BitConverter.ToSingle(data, pos+8));
	}

	public static CharacterAppearance ReadCharacterAppearance(byte[] data, int pos){ // Size = 247
		return new CharacterAppearance((Race)data[pos], NetDecoder.ReadRGB(data, pos+1), NetDecoder.ReadClothingInfo(data, pos+13), NetDecoder.ReadClothingInfo(data, pos+52), NetDecoder.ReadClothingInfo(data, pos+91), NetDecoder.ReadClothingInfo(data, pos+130), NetDecoder.ReadClothingInfo(data, pos+169), NetDecoder.ReadClothingInfo(data, pos+208));
	}

	public static SpecialEffect ReadSpecialEffect(byte[] data, int pos){
		return new SpecialEffect((EffectType)NetDecoder.ReadUshort(data, pos), (EffectUsecase)NetDecoder.ReadByte(data, pos+2), NetDecoder.ReadByte(data, pos+3), NetDecoder.ReadUshort(data, pos+4), NetDecoder.ReadBool(data, pos+6));
	}

	public static CharacterSheet ReadCharacterSheet(byte[] data, int pos){ // Size = 1221
		CharacterSheet cs = new CharacterSheet();
		SpecialEffect cachedFX;

		cs.SetName(NetDecoder.ReadString(data, pos, 20));
		pos += 20;
		cs.SetAlignment((Alignment)NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetReligion((Religion)NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetRace((Race)NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetGender(NetDecoder.ReadBool(data, pos));
		pos++;
		cs.SetCronology(NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetStrength(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetPrecision(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetVitality(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetEvasion(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetMagic(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetCharisma(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetFireResistance(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetColdResistance(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetLightningResistance(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetPoisonResistance(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetCurseResistance(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetSpeed(NetDecoder.ReadAttribute(data, pos));
		pos += 6;
		cs.SetHealth(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetMana(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetPower(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetSanity(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetProtection(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetEquipmentWeight(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetPoise(NetDecoder.ReadDepletableAttribute(data, pos));
		pos += 5;
		cs.SetPhysicalDefense(NetDecoder.ReadUshort(data, pos));
		pos += 2;
		cs.SetMagicalDefense(NetDecoder.ReadUshort(data, pos));
		pos += 2;
		cs.SetDamageReductionMultiplier(NetDecoder.ReadFloat(data, pos));
		pos += 4;
		cs.SetHasBlood(NetDecoder.ReadBool(data, pos));
		pos++;
		cs.SetIsWeaponDrawn(NetDecoder.ReadBool(data, pos));
		pos++;
		cs.SetIsImortal(NetDecoder.ReadBool(data, pos));
		pos++;
		cs.SetMainSkill((SkillType)NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetSecondarySkill((SkillType)NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetSkill(SkillType.ALCHEMY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.ARTIFICING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.BLOODMANCY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.CRAFTING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.COMBAT, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.CONSTRUCTION, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.COOKING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.ENCHANTING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.FARMING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.FISHING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.LEADERSHIP, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.MINING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.MOUNTING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.MUSICALITY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.NATURALISM, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.SMITHING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.SORCERY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.THIEVERY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.TECHNOLOGY, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.TRANSMUTING, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetSkill(SkillType.WITCHCRAFT, NetDecoder.ReadSkillEXP(data, pos));
		pos += 5;
		cs.SetRightHand(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetLeftHand(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetHelmet(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetArmor(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetLegs(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetBoots(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetRing1(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetRing2(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetRing3(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetRing4(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetAmulet(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetCape(ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos)));
		pos += 2;
		cs.SetCharacterAppearance(NetDecoder.ReadCharacterAppearance(data, pos));
		pos += 247;

		for(int i=0; i < 100; i++){
			cachedFX = NetDecoder.ReadSpecialEffect(data, pos);
			pos += 7;

			if(cachedFX.GetEffectType() == EffectType.NONE)
				break;

			cs.GetSpecialEffectHandler().Add(cachedFX);
		}

		return cs;
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

	// Size = 247
	public static void WriteCharacterAppearance(CharacterAppearance ca, byte[] data, int pos){
		NetDecoder.WriteByte((byte)ca.race, data, pos);
		NetDecoder.WriteRGB(ca.skinColor, data, pos+1);
		NetDecoder.WriteClothingInfo(ca.hat, data, pos+13);
		NetDecoder.WriteClothingInfo(ca.torso, data, pos+52);
		NetDecoder.WriteClothingInfo(ca.legs, data, pos+91);
		NetDecoder.WriteClothingInfo(ca.boots, data, pos+130);
		NetDecoder.WriteClothingInfo(ca.face, data, pos+169);
		NetDecoder.WriteClothingInfo(ca.face, data, pos+208);
	}

	public static void WriteSpecialEffect(SpecialEffect sfx, byte[] data, int pos){
		NetDecoder.WriteUshort((ushort)sfx.GetEffectType(), data, pos);
		NetDecoder.WriteByte((byte)sfx.GetUsecase(), data, pos+2);
		NetDecoder.WriteByte(sfx.GetTickDuration(), data, pos+3);
		NetDecoder.WriteUshort(sfx.GetTicks(), data, pos+4);
		NetDecoder.WriteBool(sfx.IsSystem(), data, pos+6);
	}

	public static void WriteZeros(int initIndex, int lastExcludedIndex, byte[] data){
		if(initIndex >= lastExcludedIndex)
			return;

		for(; initIndex < lastExcludedIndex; initIndex++){
			NetDecoder.WriteByte(0, data, initIndex);
		}
	}

	public static void WriteCharacterSheet(CharacterSheet sheet, byte[] data, int pos){
		NetDecoder.WriteString(sheet.GetName(), data, pos);
		pos += 20;
		NetDecoder.WriteByte((byte)sheet.GetAlignment(), data, pos);
		pos++;
		NetDecoder.WriteByte((byte)sheet.GetReligion(), data, pos);
		pos++;
		NetDecoder.WriteByte((byte)sheet.GetRace(), data, pos);
		pos++;
		NetDecoder.WriteBool(sheet.GetGender(), data, pos);
		pos++;
		NetDecoder.WriteByte(sheet.GetCronology(), data, pos);
		pos++;
		NetDecoder.WriteAttribute(sheet.GetStrength(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetPrecision(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetVitality(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetEvasion(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetMagic(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetCharisma(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetFireResistance(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetColdResistance(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetLightningResistance(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetPoisonResistance(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetCurseResistance(), data, pos);
		pos += 6;
		NetDecoder.WriteAttribute(sheet.GetSpeed(), data, pos);
		pos += 6;
		NetDecoder.WriteDepletableAttribute(sheet.GetHealth(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetMana(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetPower(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetSanity(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetProtection(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetEquipmentWeight(), data, pos);
		pos += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetPoise(), data, pos);
		pos += 5;
		NetDecoder.WriteUshort(sheet.GetPhysicalDefense(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetMagicalDefense(), data, pos);
		pos += 2;
		NetDecoder.WriteFloat(sheet.GetDamageReductionMultiplier(), data, pos);
		pos += 4;
		NetDecoder.WriteBool(sheet.HasBlood(), data, pos);
		pos++;
		NetDecoder.WriteBool(sheet.IsWeaponDrawn(), data, pos);
		pos++;
		NetDecoder.WriteBool(sheet.IsImortal(), data, pos);
		pos++;
		NetDecoder.WriteByte((byte)sheet.GetMainSkill(), data, pos);
		pos++;
		NetDecoder.WriteByte((byte)sheet.GetSecondarySkill(), data, pos);
		pos++;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.ALCHEMY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.ARTIFICING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.BLOODMANCY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.CRAFTING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.COMBAT), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.CONSTRUCTION), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.COOKING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.ENCHANTING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.FARMING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.FISHING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.LEADERSHIP), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MINING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MOUNTING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MUSICALITY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.NATURALISM), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.SMITHING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.SORCERY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.THIEVERY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.TECHNOLOGY), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.TRANSMUTING), data, pos);
		pos += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.WITCHCRAFT), data, pos);
		pos += 5;
		// CHANGE SIMPLE ITEM STORING TO WEAPON
		NetDecoder.WriteUshort(sheet.GetRightHand().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetLeftHand().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetHelmet().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetArmor().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetLegs().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetBoots().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetRing1().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetRing2().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetRing3().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetRing4().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetAmulet().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteUshort(sheet.GetCape().GetID(), data, pos);
		pos += 2;
		NetDecoder.WriteCharacterAppearance(sheet.GetCharacterAppearance(), data, pos);
		pos += 247;

		List<SpecialEffect> sfxList = sheet.GetSpecialEffectHandler().GetAllEffects();

		for(int i=0; i < 100; i++){
			if(sfxList.Count == 0){
				NetDecoder.WriteSpecialEffect(NULL_EFFECT, data, pos);
			}
			else{
				NetDecoder.WriteSpecialEffect(sfxList[0], data, pos);
				sfxList.RemoveAt(0);
			}

			pos += 7;
		}
	}
}
