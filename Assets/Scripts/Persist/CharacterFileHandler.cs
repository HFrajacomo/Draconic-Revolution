using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/*
.cdat file

CharacterName(20) | Alignment(1) | Religion(1) | Race(1) | Gender(1) | Cronology(1) | [25]
Strength(Attribute 6) | Precision(6) | Vitality(6) | Evasion(6) | Magic(6) | Charisma(6) | [36]
FireRes(6) | IceRes(6) | LightningRes(6) | PoisonRes(6) | CurseRes(6) | Speed(6) | [36]
Health(DepletableAtt 5) | Mana(5) | Power(5) | Sanity(5) | Protection(5) | Weight(5) | Poise(5) | [35]
PhysicalDefense(2) | MagicalDefense(2) | DamageReduction(4) | hasBlood(1) | isWeaponDrawn(1) | isImortal(1) | [11]
MainSkill(1) | SecondarySkill(1) | alchemy(5) | bloodmancy(5) | crafting(5) | combat(5) | construction(5) | cooking(5) | enchanting(5) | farming(5) | [42]
fishing(5) | leadership(5) | mining(5) | mounting(5) | musicality(5) | naturalism(5) | smithing(5) | sorcery(5) | thievery(5) | technology(5) | thaumaturgy(5) | [55]
transmuting(5) | witchcraft(5) | [10]
RightHand(2) | LeftHand(2) | Helmet(2) | Armor(2) | Legs(2) | Boots(2) | Ring1(2) | Ring2(2) | Ring3(2) | Ring4(2) | Amulet(2) | Cape(2) | [24]
CharacterAppearance(169) | SpecialEffects(7) *100 | [869]


.cind file

PlayerID (8) | SeekPosition (8)
*/

public class CharacterFileHandler{
	private static string characterDirectory;
	private static string indexFileDir;

	private Stream file;
	private Stream indexFile;

	private byte[] indexArray = new byte[16];
	private byte[] buffer = new byte[1143]; // Total CharacterSheet size 

	private Dictionary<ulong, ulong> index = new Dictionary<ulong, ulong>();

	private static readonly SpecialEffect NULL_EFFECT = new SpecialEffect(EffectType.NONE);
	private SpecialEffect cachedFX;


	public CharacterFileHandler(string world){
		CharacterFileHandler.characterDirectory = EnvironmentVariablesCentral.saveDir + "\\" + world + "\\Characters\\";
		CharacterFileHandler.indexFileDir = EnvironmentVariablesCentral.saveDir + "\\" + world + "\\Characters\\index.cind";

        if(!Directory.Exists(CharacterFileHandler.characterDirectory))
            Directory.CreateDirectory(CharacterFileHandler.characterDirectory);

        // Opens Char file
        if(!File.Exists(CharacterFileHandler.characterDirectory + "characters.cdat"))
        	this.file = File.Open(CharacterFileHandler.characterDirectory + "characters.cdat", FileMode.Create);
        else
        	this.file = File.Open(CharacterFileHandler.characterDirectory + "characters.cdat", FileMode.Open);


        // Opens Index file
        if(File.Exists(CharacterFileHandler.indexFileDir))
            this.indexFile = File.Open(CharacterFileHandler.indexFileDir, FileMode.Open);
        else
            this.indexFile = File.Open(CharacterFileHandler.indexFileDir, FileMode.Create);

        LoadIndex();
	}

	public void SaveCharacterSheet(ulong code, CharacterSheet sheet){
		int count = 0;

		NetDecoder.WriteString(sheet.GetName(), buffer, count);
		count += 20;
		NetDecoder.WriteByte((byte)sheet.GetAlignment(), buffer, count);
		count++;
		NetDecoder.WriteByte((byte)sheet.GetReligion(), buffer, count);
		count++;
		NetDecoder.WriteByte((byte)sheet.GetRace(), buffer, count);
		count++;
		NetDecoder.WriteBool(sheet.GetGender(), buffer, count);
		count++;
		NetDecoder.WriteByte(sheet.GetCronology(), buffer, count);
		count++;
		NetDecoder.WriteAttribute(sheet.GetStrength(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetPrecision(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetVitality(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetEvasion(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetMagic(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetCharisma(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetFireResistance(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetColdResistance(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetLightningResistance(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetPoisonResistance(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetCurseResistance(), buffer, count);
		count += 6;
		NetDecoder.WriteAttribute(sheet.GetSpeed(), buffer, count);
		count += 6;
		NetDecoder.WriteDepletableAttribute(sheet.GetHealth(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetMana(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetPower(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetSanity(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetProtection(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetEquipmentWeight(), buffer, count);
		count += 5;
		NetDecoder.WriteDepletableAttribute(sheet.GetPoise(), buffer, count);
		count += 5;
		NetDecoder.WriteUshort(sheet.GetPhysicalDefense(), buffer, count);
		count += 2;
		NetDecoder.WriteUshort(sheet.GetMagicalDefense(), buffer, count);
		count += 2;
		NetDecoder.WriteFloat(sheet.GetDamageReductionMultiplier(), buffer, count);
		count += 4;
		NetDecoder.WriteBool(sheet.HasBlood(), buffer, count);
		count++;
		NetDecoder.WriteBool(sheet.IsWeaponDrawn(), buffer, count);
		count++;
		NetDecoder.WriteBool(sheet.IsImortal(), buffer, count);
		count++;
		NetDecoder.WriteByte((byte)sheet.GetMainSkill(), buffer, count);
		count++;
		NetDecoder.WriteByte((byte)sheet.GetSecondarySkill(), buffer, count);
		count++;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.ALCHEMY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.BLOODMANCY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.CRAFTING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.COMBAT), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.CONSTRUCTION), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.COOKING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.ENCHANTING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.FARMING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.FISHING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.LEADERSHIP), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MINING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MOUNTING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.MUSICALITY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.NATURALISM), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.SMITHING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.SORCERY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.THIEVERY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.TECHNOLOGY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.THAUMATURGY), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.TRANSMUTING), buffer, count);
		count += 5;
		NetDecoder.WriteSkillEXP(sheet.GetSkill(SkillType.WITCHCRAFT), buffer, count);
		count += 5;
		// CHANGE SIMPLE ITEM STORING TO WEAPON
		NetDecoder.WriteUshort((ushort)sheet.GetRightHand().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetLeftHand().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetHelmet().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetArmor().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetLegs().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetBoots().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetRing1().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetRing2().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetRing3().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetRing4().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetAmulet().id, buffer, count);
		count += 2;
		NetDecoder.WriteUshort((ushort)sheet.GetCape().id, buffer, count);
		count += 2;
		NetDecoder.WriteCharacterAppearance(sheet.GetCharacterAppearance(), buffer, count);
		count += 169;

		List<SpecialEffect> sfxList = sheet.GetSpecialEffectHandler().GetAllEffects();

		for(int i=0; i < 100; i++){
			if(sfxList.Count == 0){
				NetDecoder.WriteSpecialEffect(NULL_EFFECT, buffer, count);
			}
			else{
				NetDecoder.WriteSpecialEffect(sfxList[0], buffer, count);
				sfxList.RemoveAt(0);
			}

			count += 7;
		}

		if(CharacterExists(code)){
			this.file.Seek((long)this.index[code], SeekOrigin.Begin);
			this.file.Write(buffer, 0, buffer.Length);
		}
		else{
			AddEntryIndex((long)code, this.indexFile.Length);
			this.index.Add(code, (ulong)this.file.Length);
			this.indexFile.Seek(0, SeekOrigin.End);
			this.indexFile.Write(this.indexArray, 0, 16);
			this.indexFile.Flush();

			this.file.Seek((long)this.index[code], SeekOrigin.Begin);
			this.file.Write(buffer, 0, buffer.Length);
		}

		this.file.Flush();
	}

	public CharacterSheet LoadCharacterSheet(ulong code){
		if(!this.index.ContainsKey(code))
			return null;

		CharacterSheet cs = new CharacterSheet();
		int count = 0;

		this.file.Seek((long)this.index[code], SeekOrigin.Begin);
		this.file.Read(buffer, 0, buffer.Length);

		cs.SetName(NetDecoder.ReadString(buffer, count, 20));
		count += 20;
		cs.SetAlignment((Alignment)NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetReligion((Religion)NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetRace((Race)NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetGender(NetDecoder.ReadBool(buffer, count));
		count++;
		cs.SetCronology(NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetStrength(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetPrecision(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetVitality(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetEvasion(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetMagic(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetCharisma(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetFireResistance(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetColdResistance(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetLightningResistance(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetPoisonResistance(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetCurseResistance(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetSpeed(NetDecoder.ReadAttribute(buffer, count));
		count += 6;
		cs.SetHealth(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetMana(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetPower(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetSanity(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetProtection(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetEquipmentWeight(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetPoise(NetDecoder.ReadDepletableAttribute(buffer, count));
		count += 5;
		cs.SetPhysicalDefense(NetDecoder.ReadUshort(buffer, count));
		count += 2;
		cs.SetMagicalDefense(NetDecoder.ReadUshort(buffer, count));
		count += 2;
		cs.SetDamageReductionMultiplier(NetDecoder.ReadFloat(buffer, count));
		count += 4;
		cs.SetHasBlood(NetDecoder.ReadBool(buffer, count));
		count++;
		cs.SetIsWeaponDrawn(NetDecoder.ReadBool(buffer, count));
		count++;
		cs.SetIsImortal(NetDecoder.ReadBool(buffer, count));
		count++;
		cs.SetMainSkill((SkillType)NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetSecondarySkill((SkillType)NetDecoder.ReadByte(buffer, count));
		count++;
		cs.SetSkill(SkillType.ALCHEMY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.BLOODMANCY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.CRAFTING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.COMBAT, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.CONSTRUCTION, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.COOKING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.ENCHANTING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.FARMING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.FISHING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.LEADERSHIP, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.MINING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.MOUNTING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.MUSICALITY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.NATURALISM, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.SMITHING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.SORCERY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.THIEVERY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.TECHNOLOGY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.THAUMATURGY, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.TRANSMUTING, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetSkill(SkillType.WITCHCRAFT, NetDecoder.ReadSkillEXP(buffer, count));
		count += 5;
		cs.SetRightHand(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetLeftHand(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetHelmet(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetArmor(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetLegs(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetBoots(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetRing1(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetRing2(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetRing3(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetRing4(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetAmulet(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetCape(Item.GenerateItem((ItemID)NetDecoder.ReadUshort(buffer, count)));
		count += 2;
		cs.SetCharacterAppearance(NetDecoder.ReadCharacterAppearance(buffer, count));
		count += 169;

		for(int i=0; i < 100; i++){
			this.cachedFX = NetDecoder.ReadSpecialEffect(buffer, count);
			count += 7;

			if(this.cachedFX.GetEffectType() == EffectType.NONE)
				break;

			cs.GetSpecialEffectHandler().Add(this.cachedFX);
		}

		return cs;
	}

	public bool CharacterExists(ulong code){
        return this.index.ContainsKey(code);
	}

	public void Close(){
		if(this.file != null)
			this.file.Close();
		if(this.indexFile != null)
			this.indexFile.Close();
	}

	private void LoadIndex(){
		ulong a,b;

        this.indexFile.Seek(0, SeekOrigin.Begin);
        byte[] indexBuffer = new byte[this.indexFile.Length];
        this.indexFile.Read(indexBuffer, 0, (int)this.indexFile.Length);

        for(int i=0; i < this.indexFile.Length/8; i+=2){
            a = ReadUlong(indexBuffer, i*8);
            b = ReadUlong(indexBuffer, (i+1)*8);

            this.index.Add(a, b);
        }
	}

    private ulong ReadUlong(byte[] buff, int pos){
        ulong a;

        a = buff[pos];
        a = a << 8;
        a += buff[pos+1];
        a = a << 8;
        a += buff[pos+2];
        a = a << 8;
        a += buff[pos+3];
        a = a << 8;
        a += buff[pos+4];
        a = a << 8;
        a += buff[pos+5];
        a = a << 8;
        a += buff[pos+6];
        a = a << 8;
        a += buff[pos+7];

        return a;        
    }

	private void AddEntryIndex(long key, long val){
		indexArray[0] = (byte)(key >> 56);
		indexArray[1] = (byte)(key >> 48);
		indexArray[2] = (byte)(key >> 40);
		indexArray[3] = (byte)(key >> 32);
		indexArray[4] = (byte)(key >> 24);
		indexArray[5] = (byte)(key >> 16);
		indexArray[6] = (byte)(key >> 8);
		indexArray[7] = (byte)(key);
		indexArray[8] = (byte)(val >> 56);
		indexArray[9] = (byte)(val >> 48);
		indexArray[10] = (byte)(val >> 40);
		indexArray[11] = (byte)(val >> 32);
		indexArray[12] = (byte)(val >> 24);
		indexArray[13] = (byte)(val >> 16);
		indexArray[14] = (byte)(val >> 8);
		indexArray[15] = (byte)(val);
	}
}