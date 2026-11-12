using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Unity.Mathematics;

/*
Binary encoding used by network messages and by save files
	Methods used by save files (CharacterFileHandler, EntityFileHandler, PlayerData, inventories, region files) must keep their exact byte layout
	Compact encodings that only travel through the network have their own methods (LocalPos, Angle, String8/16, *Net, *Compact)
*/
public static class NetDecoder
{
	private static readonly SpecialEffect NULL_EFFECT = new SpecialEffect(EffectType.NONE);
	private static readonly int CHARACTER_NAME_SIZE = 20;
	private static readonly int MAX_SAVED_EFFECTS = 100;

	// Reinterprets floats as ints without allocating
	[StructLayout(LayoutKind.Explicit)]
	private struct FloatBits{
		[FieldOffset(0)] public float f;
		[FieldOffset(0)] public int i;
	}


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
		if(size == 0)
			return "";

		return Encoding.UTF8.GetString(data, pos, size);
	}

	// Reads a UTF-8 string prefixed by its size in bytes (1 byte)
	public static string ReadString8(byte[] data, int pos, out int bytesRead){
		int size = data[pos];

		bytesRead = 1 + size;
		return ReadString(data, pos+1, size);
	}

	// Reads a UTF-8 string prefixed by its size in bytes (2 bytes)
	public static string ReadString16(byte[] data, int pos, out int bytesRead){
		int size = ReadUshort(data, pos);

		bytesRead = 2 + size;
		return ReadString(data, pos+2, size);
	}

	// Reads a zero-padded UTF-8 string of a fixed size
	public static string ReadFixedString(byte[] data, int pos, int length){
		int size = 0;

		while(size < length && data[pos+size] != 0)
			size++;

		return ReadString(data, pos, size);
	}

	public static ChunkPos ReadChunkPos(byte[] data, int pos){
		return new ChunkPos(NetDecoder.ReadInt(data, pos), NetDecoder.ReadInt(data, pos+4), NetDecoder.ReadByte(data, pos+8));
	}

	// Reads a voxel coordinate inside a chunk written by WriteLocalPos
	public static void ReadLocalPos(byte[] data, int pos, out int x, out int y, out int z){
		x = data[pos] >> 4;
		z = data[pos] & 0x0F;
		y = data[pos+1];
	}

	// Reads an angle in degrees written by WriteAngle
	public static float ReadAngle(byte[] data, int pos){
		return NetDecoder.ReadUshort(data, pos) * (360f / 65536f);
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

	// Network only: colors with 8 bits per channel
	public static Color ReadRGB8(byte[] data, int pos){
		return new Color(data[pos] / 255f, data[pos+1] / 255f, data[pos+2] / 255f);
	}

	public static ClothingInfo ReadClothingInfoCompact(byte[] data, int pos){ // Size = 12
		return new ClothingInfo(NetDecoder.ReadUshort(data, pos), NetDecoder.ReadRGB8(data, pos+2), NetDecoder.ReadRGB8(data, pos+5), NetDecoder.ReadRGB8(data, pos+8), NetDecoder.ReadBool(data, pos+11));
	}

	public static CharacterAppearance ReadCharacterAppearanceCompact(byte[] data, int pos){ // Size = 76
		return new CharacterAppearance((Race)data[pos], NetDecoder.ReadRGB8(data, pos+1), NetDecoder.ReadClothingInfoCompact(data, pos+4), NetDecoder.ReadClothingInfoCompact(data, pos+16), NetDecoder.ReadClothingInfoCompact(data, pos+28), NetDecoder.ReadClothingInfoCompact(data, pos+40), NetDecoder.ReadClothingInfoCompact(data, pos+52), NetDecoder.ReadClothingInfoCompact(data, pos+64));
	}

	public static SpecialEffect ReadSpecialEffect(byte[] data, int pos){
		return new SpecialEffect((EffectType)NetDecoder.ReadUshort(data, pos), (EffectUsecase)NetDecoder.ReadByte(data, pos+2), NetDecoder.ReadByte(data, pos+3), NetDecoder.ReadUshort(data, pos+4), NetDecoder.ReadBool(data, pos+6));
	}

	// Save file layout. Size = 1203
	public static CharacterSheet ReadCharacterSheet(byte[] data, int pos){
		CharacterSheet cs = new CharacterSheet();
		SpecialEffect cachedFX;

		cs.SetName(NetDecoder.ReadFixedString(data, pos, CHARACTER_NAME_SIZE));
		pos += CHARACTER_NAME_SIZE;
		pos = ReadSheetBody(cs, data, pos);
		cs.SetCharacterAppearance(NetDecoder.ReadCharacterAppearance(data, pos));
		pos += 247;
		cs.SetHotbarSlot(NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetAttackHotbarSlot(NetDecoder.ReadByte(data, pos));
		pos++;

		for(int i=0; i < MAX_SAVED_EFFECTS; i++){
			cachedFX = NetDecoder.ReadSpecialEffect(data, pos);
			pos += 7;

			if(cachedFX.GetEffectType() == EffectType.NONE)
				break;

			cs.GetSpecialEffectHandler().Add(cachedFX);
		}

		return cs;
	}

	// Network layout written by WriteCharacterSheetNet
	public static CharacterSheet ReadCharacterSheetNet(byte[] data, int pos, bool compactAppearance){
		CharacterSheet cs = new CharacterSheet();
		int bytesRead;
		int effectCount;

		cs.SetName(NetDecoder.ReadString8(data, pos, out bytesRead));
		pos += bytesRead;
		pos = ReadSheetBody(cs, data, pos);

		if(compactAppearance){
			cs.SetCharacterAppearance(NetDecoder.ReadCharacterAppearanceCompact(data, pos));
			pos += 76;
		}
		else{
			cs.SetCharacterAppearance(NetDecoder.ReadCharacterAppearance(data, pos));
			pos += 247;
		}

		cs.SetHotbarSlot(NetDecoder.ReadByte(data, pos));
		pos++;
		cs.SetAttackHotbarSlot(NetDecoder.ReadByte(data, pos));
		pos++;

		effectCount = data[pos];
		pos++;

		for(int i=0; i < effectCount; i++){
			cs.GetSpecialEffectHandler().Add(NetDecoder.ReadSpecialEffect(data, pos));
			pos += 7;
		}

		return cs;
	}

	// Every CharacterSheet field between the name and the appearance. Returns the position after it. Size = 234
	private static int ReadSheetBody(CharacterSheet cs, byte[] data, int pos){
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
		cs.SetBattleStyleCode(NetDecoder.ReadInt(data, pos));
		pos += 4;
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

		return pos;
	}

	public static float ReadFloat(byte[] data, int pos){
		return System.BitConverter.ToSingle(data, pos);
	}

	public static float3 ReadFloat3(byte[] data, int pos){
		return new float3(NetDecoder.ReadFloat(data, pos), NetDecoder.ReadFloat(data, pos+4), NetDecoder.ReadFloat(data, pos+8));
	}

	public static Item ReadItem(byte[] data, int pos){
		return ItemLoader.GetCopy(NetDecoder.ReadUshort(data, pos));
	}

	// Reads a deep copy of an item
	public static Weapon ReadWeapon(byte[] data, int pos){
		Weapon weap;

		ushort id = NetDecoder.ReadUshort(data, pos);
		uint durability = NetDecoder.ReadUint(data, pos+2);
		byte refineLevel = NetDecoder.ReadByte(data, pos+6);
		byte extraEffect = NetDecoder.ReadByte(data, pos+7);

		weap = (Weapon)ItemLoader.GetCopy(id);
		weap.SetDurability(durability);
		weap.SetRefineLevel(refineLevel);
		weap.SetExtraEffects((EnchantmentType)extraEffect);

		return weap;
	}

	// Reads a deep copy of a SubWeapon
	public static SubWeapon ReadSubWeapon(byte[] data, int pos){
		SubWeapon sub;

		ushort id = NetDecoder.ReadUshort(data, pos);
		uint durability = NetDecoder.ReadUint(data, pos+2);

		sub = (SubWeapon)ItemLoader.GetCopy(id);
		sub.SetDurability(durability);

		return sub;
	}

	// Reads an item written by WriteItemDeep
	public static Item ReadItemDeep(byte[] data, int pos, out int bytesRead){
		switch((MemoryStorageType)data[pos]){
			case MemoryStorageType.WEAPON:
				bytesRead = 9;
				return NetDecoder.ReadWeapon(data, pos+1);
			case MemoryStorageType.SUBWEAPON:
				bytesRead = 7;
				return NetDecoder.ReadSubWeapon(data, pos+1);
			default:
				bytesRead = 3;
				return NetDecoder.ReadItem(data, pos+1);
		}
	}

	public static EntityAction ReadAction(byte[] data, int pos){
		EntityAction ea;

		ushort id = NetDecoder.ReadUshort(data, pos);
		bool connectedToStack = NetDecoder.ReadBool(data, pos+2);
		ushort currentCooldown = NetDecoder.ReadUshort(data, pos+3);
		ushort totalCooldown = NetDecoder.ReadUshort(data, pos+5);
		byte csi = NetDecoder.ReadByte(data, pos+7);
		byte css = NetDecoder.ReadByte(data, pos+8);

		ea = ActionLoader.GetAction(id);
		ea.SetMemoryData(connectedToStack, currentCooldown, totalCooldown, csi, css);
		return ea;
	}

	public static bool ReadBool(byte[] data, int pos){
		if(data[pos] == 0)
			return false;
		return true;
	}

	public static byte ReadByte(byte[] data, int pos){
		return data[pos];
	}

	// Reads an EntitySFXData written by WriteEntitySFXData
	public static EntitySFXData ReadEntitySFXData(byte[] data, int pos){
		int bytesRead;
		bool isLoop = NetDecoder.ReadBool(data, pos);
		bool hasAnchor = NetDecoder.ReadBool(data, pos+1);
		BoneAnchorType anchor = (BoneAnchorType)NetDecoder.ReadByte(data, pos+2);
		float offsetX = NetDecoder.ReadFloat(data, pos+3);
		float offsetY = NetDecoder.ReadFloat(data, pos+7);
		float offsetZ = NetDecoder.ReadFloat(data, pos+11);
		string name = NetDecoder.ReadString8(data, pos+15, out bytesRead);

		return new EntitySFXData(name, isLoop, hasAnchor, anchor, offsetX, offsetY, offsetZ);
	}

	// Returns the amount of bytes written
	public static int WriteEntitySFXData(EntitySFXData sfx, byte[] data, int pos){
		NetDecoder.WriteBool(sfx.isLoop, data, pos);
		NetDecoder.WriteBool(sfx.hasAnchor, data, pos+1);
		NetDecoder.WriteByte((byte)sfx.anchor, data, pos+2);
		NetDecoder.WriteFloat(sfx.offsetX, data, pos+3);
		NetDecoder.WriteFloat(sfx.offsetY, data, pos+7);
		NetDecoder.WriteFloat(sfx.offsetZ, data, pos+11);

		return 15 + NetDecoder.WriteString8(sfx.name, data, pos+15);
	}

	// Reads an EntityVFXData written by WriteEntityVFXData
	public static EntityVFXData ReadEntityVFXData(byte[] data, int pos){
		int nameSize, pathSize;
		BoneAnchorType anchor = (BoneAnchorType)NetDecoder.ReadByte(data, pos);
		float offsetX = NetDecoder.ReadFloat(data, pos+1);
		float offsetY = NetDecoder.ReadFloat(data, pos+5);
		float offsetZ = NetDecoder.ReadFloat(data, pos+9);
		float rotationX = NetDecoder.ReadFloat(data, pos+13);
		float rotationY = NetDecoder.ReadFloat(data, pos+17);
		float rotationZ = NetDecoder.ReadFloat(data, pos+21);
		string name = NetDecoder.ReadString8(data, pos+25, out nameSize);
		string prefabPath = NetDecoder.ReadString8(data, pos+25+nameSize, out pathSize);

		return new EntityVFXData(name, anchor, offsetX, offsetY, offsetZ, rotationX, rotationY, rotationZ, prefabPath);
	}

	// Returns the amount of bytes written
	public static int WriteEntityVFXData(EntityVFXData vfx, byte[] data, int pos){
		int nameSize;

		NetDecoder.WriteByte((byte)vfx.anchor, data, pos);
		NetDecoder.WriteFloat(vfx.offsetX, data, pos+1);
		NetDecoder.WriteFloat(vfx.offsetY, data, pos+5);
		NetDecoder.WriteFloat(vfx.offsetZ, data, pos+9);
		NetDecoder.WriteFloat(vfx.rotationX, data, pos+13);
		NetDecoder.WriteFloat(vfx.rotationY, data, pos+17);
		NetDecoder.WriteFloat(vfx.rotationZ, data, pos+21);
		nameSize = NetDecoder.WriteString8(vfx.name, data, pos+25);

		return 25 + nameSize + NetDecoder.WriteString8(vfx.prefabPath, data, pos+25+nameSize);
	}

	public static void WriteRGB(Color c, byte[] data, int pos){
		NetDecoder.WriteFloat(c.r, data, pos);
		NetDecoder.WriteFloat(c.g, data, pos+4);
		NetDecoder.WriteFloat(c.b, data, pos+8);
	}

	// Network only: colors with 8 bits per channel
	public static void WriteRGB8(Color c, byte[] data, int pos){
		data[pos] = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
		data[pos+1] = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
		data[pos+2] = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
	}

	// Same bytes as BitConverter.GetBytes (little-endian), without allocating
	public static void WriteFloat(float a, byte[] data, int pos){
		FloatBits bits = new FloatBits();
		bits.f = a;

		data[pos] = (byte)bits.i;
		data[pos+1] = (byte)(bits.i >> 8);
		data[pos+2] = (byte)(bits.i >> 16);
		data[pos+3] = (byte)(bits.i >> 24);
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

	// Network only: an angle in degrees with a precision of 360/65536 degrees. Size = 2
	public static void WriteAngle(float degrees, byte[] data, int pos){
		NetDecoder.WriteUshort((ushort)(Mathf.RoundToInt(Mathf.Repeat(degrees, 360f) * (65536f / 360f)) & 0xFFFF), data, pos);
	}

	// Network only: a voxel coordinate inside a chunk. Size = 2
	public static void WriteLocalPos(int x, int y, int z, byte[] data, int pos){
		data[pos] = (byte)(((x & 0x0F) << 4) | (z & 0x0F));
		data[pos+1] = (byte)y;
	}

	public static void WriteItem(Item it, byte[] data, int pos){
		NetDecoder.WriteUshort(it.GetID(), data, pos);
	}

	// Writes a deep copy of an item, not just ID
	public static int WriteItemDeep(Item it, byte[] data, int pos){
		if(it == null)
			it = ItemLoader.GetItem(0);

		NetDecoder.WriteByte((byte)it.GetMemoryStorageType(), data, pos);

		switch(it.GetMemoryStorageType()){
			case MemoryStorageType.ITEM:
				return HelperItem(it, data, pos+1);
			case MemoryStorageType.WEAPON:
				return HelperWeapon((Weapon)it, data, pos+1);
			case MemoryStorageType.SUBWEAPON:
				return HelperSubWeapon((SubWeapon)it, data, pos+1);
			default:
				return HelperItem(it, data, pos+1);
		}
	}
	private static int HelperItem(Item it, byte[] data, int pos){
		NetDecoder.WriteUshort(it.GetID(), data, pos);

		return 3;
	}
	private static int HelperWeapon(Weapon it, byte[] data, int pos){
		NetDecoder.WriteUshort(it.GetID(), data, pos);
		NetDecoder.WriteUint(it.currentDurability, data, pos+2);
		NetDecoder.WriteByte(it.refineLevel, data, pos+6);
		NetDecoder.WriteByte((byte)it.extraEffect, data, pos+7);

		return 9;
	}
	// A single SubWeapon is sent, so no quantity is needed
	private static int HelperSubWeapon(SubWeapon it, byte[] data, int pos){
		NetDecoder.WriteUshort(it.GetID(), data, pos);
		NetDecoder.WriteUint(it.currentDurability, data, pos+2);

		return 7;
	}

	public static int WriteAction(EntityAction ea, byte[] data, int pos){
		return ea.ConvertToMemory(data, pos);
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

	// Writes a UTF-8 string prefixed by its size in bytes (1 byte). Strings longer than 255 bytes are truncated. Returns the amount of bytes written
	public static int WriteString8(string a, byte[] data, int pos){
		int size = WriteTruncatedUTF8(a, data, pos+1, byte.MaxValue);

		data[pos] = (byte)size;
		return 1 + size;
	}

	// Writes a UTF-8 string prefixed by its size in bytes (2 bytes). Strings longer than 65535 bytes are truncated. Returns the amount of bytes written
	public static int WriteString16(string a, byte[] data, int pos){
		int size = WriteTruncatedUTF8(a, data, pos+2, ushort.MaxValue);

		NetDecoder.WriteUshort((ushort)size, data, pos);
		return 2 + size;
	}

	// Writes a UTF-8 string in a fixed size, padded with zeros
	public static void WriteFixedString(string a, byte[] data, int pos, int length){
		int size = WriteTruncatedUTF8(a, data, pos, length);

		for(int i=size; i < length; i++)
			data[pos+i] = 0;
	}

	// Writes a string as UTF-8, cutting it before maxBytes without splitting characters. Returns the amount of bytes written
	private static int WriteTruncatedUTF8(string a, byte[] data, int pos, int maxBytes){
		if(string.IsNullOrEmpty(a))
			return 0;

		byte[] encoded = Encoding.UTF8.GetBytes(a);
		int size = encoded.Length;

		if(size > maxBytes){
			size = maxBytes;

			// Continuation bytes of a multi-byte character start with 10xxxxxx
			while(size > 0 && (encoded[size] & 0xC0) == 0x80)
				size--;
		}

		Array.Copy(encoded, 0, data, pos, size);
		return size;
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
		NetDecoder.WriteClothingInfo(ca.hair, data, pos+208);
	}

	// Network only. Size = 12
	public static void WriteClothingInfoCompact(ClothingInfo i, byte[] data, int pos){
		NetDecoder.WriteUshort(i.code, data, pos);
		NetDecoder.WriteRGB8(i.primary, data, pos+2);
		NetDecoder.WriteRGB8(i.secondary, data, pos+5);
		NetDecoder.WriteRGB8(i.terciary, data, pos+8);
		NetDecoder.WriteBool(i.isMale, data, pos+11);
	}

	// Network only. Size = 76
	public static void WriteCharacterAppearanceCompact(CharacterAppearance ca, byte[] data, int pos){
		NetDecoder.WriteByte((byte)ca.race, data, pos);
		NetDecoder.WriteRGB8(ca.skinColor, data, pos+1);
		NetDecoder.WriteClothingInfoCompact(ca.hat, data, pos+4);
		NetDecoder.WriteClothingInfoCompact(ca.torso, data, pos+16);
		NetDecoder.WriteClothingInfoCompact(ca.legs, data, pos+28);
		NetDecoder.WriteClothingInfoCompact(ca.boots, data, pos+40);
		NetDecoder.WriteClothingInfoCompact(ca.face, data, pos+52);
		NetDecoder.WriteClothingInfoCompact(ca.hair, data, pos+64);
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

	// Save file layout. Size = 1203
	public static void WriteCharacterSheet(CharacterSheet sheet, byte[] data, int pos){
		NetDecoder.WriteFixedString(sheet.GetName(), data, pos, CHARACTER_NAME_SIZE);
		pos += CHARACTER_NAME_SIZE;
		pos = WriteSheetBody(sheet, data, pos);
		NetDecoder.WriteCharacterAppearance(sheet.GetCharacterAppearance(), data, pos);
		pos += 247;
		NetDecoder.WriteByte(sheet.GetHotbarSlot(), data, pos);
		pos++;
		NetDecoder.WriteByte(sheet.GetAttackHotbarSlot(), data, pos);
		pos++;

		List<SpecialEffect> sfxList = sheet.GetSpecialEffectHandler().GetAllEffects();

		for(int i=0; i < MAX_SAVED_EFFECTS; i++){
			if(i < sfxList.Count)
				NetDecoder.WriteSpecialEffect(sfxList[i], data, pos);
			else
				NetDecoder.WriteSpecialEffect(NULL_EFFECT, data, pos);

			pos += 7;
		}
	}

	/*
	Network layout of a CharacterSheet. Returns the amount of bytes written
		[Name (string8)] | Body (234) | Appearance (247, or 76 if compact) | [Hotbar][AttackHotbar][EffectCount] | EffectCount x SpecialEffect (7)
	*/
	public static int WriteCharacterSheetNet(CharacterSheet sheet, byte[] data, int pos, bool compactAppearance){
		int initialPos = pos;
		int effectCount;

		pos += NetDecoder.WriteString8(sheet.GetName(), data, pos);
		pos = WriteSheetBody(sheet, data, pos);

		if(compactAppearance){
			NetDecoder.WriteCharacterAppearanceCompact(sheet.GetCharacterAppearance(), data, pos);
			pos += 76;
		}
		else{
			NetDecoder.WriteCharacterAppearance(sheet.GetCharacterAppearance(), data, pos);
			pos += 247;
		}

		NetDecoder.WriteByte(sheet.GetHotbarSlot(), data, pos);
		pos++;
		NetDecoder.WriteByte(sheet.GetAttackHotbarSlot(), data, pos);
		pos++;

		List<SpecialEffect> sfxList = sheet.GetSpecialEffectHandler().GetAllEffects();
		effectCount = Math.Min(sfxList.Count, byte.MaxValue);

		NetDecoder.WriteByte((byte)effectCount, data, pos);
		pos++;

		for(int i=0; i < effectCount; i++){
			NetDecoder.WriteSpecialEffect(sfxList[i], data, pos);
			pos += 7;
		}

		return pos - initialPos;
	}

	// Every CharacterSheet field between the name and the appearance. Returns the position after it. Size = 234
	private static int WriteSheetBody(CharacterSheet sheet, byte[] data, int pos){
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
		NetDecoder.WriteInt(sheet.GetBattleStyleCode(), data, pos);
		pos += 4;
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

		return pos;
	}
}
