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
MainSkill(1) | SecondarySkill(1) | alchemy(5) | articing(5) | bloodmancy(5) | crafting(5) | combat(5) | construction(5) | cooking(5) | enchanting(5) | farming(5) | [42]
fishing(5) | leadership(5) | mining(5) | mounting(5) | musicality(5) | naturalism(5) | smithing(5) | sorcery(5) | thievery(5) | technology(5) | [55]
transmuting(5) | witchcraft(5) | [10]
RightHand(2) | LeftHand(2) | Helmet(2) | Armor(2) | Legs(2) | Boots(2) | Ring1(2) | Ring2(2) | Ring3(2) | Ring4(2) | Amulet(2) | Cape(2) | [24]
CharacterAppearance(247) | SpecialEffects(7) *100 |

.cind file

PlayerID (8) | SeekPosition (8)
*/

public class CharacterFileHandler{
	private static string characterDirectory;
	private static string indexFileDir;

	private Stream file;
	private Stream indexFile;

	private byte[] indexArray = new byte[16];
	private byte[] buffer = new byte[1222]; // Total CharacterSheet size 

	private Dictionary<ulong, ulong> index = new Dictionary<ulong, ulong>();

	private static readonly SpecialEffect NULL_EFFECT = new SpecialEffect(EffectType.NONE);
	private SpecialEffect cachedFX;


	public CharacterFileHandler(string world){
		CharacterFileHandler.characterDirectory = (EnvironmentVariablesCentral.saveDir + world + "\\Characters\\").Replace("\\\\", "\\");
		CharacterFileHandler.indexFileDir = (EnvironmentVariablesCentral.saveDir + world + "\\Characters\\index.cind").Replace("\\\\", "\\");

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
		NetDecoder.WriteCharacterSheet(sheet, buffer, 0);

		if(CharacterExists(code)){
			this.file.Seek((long)this.index[code], SeekOrigin.Begin);
			this.file.Write(buffer, 0, buffer.Length);
		}
		else{
			AddEntryIndex((long)code, this.file.Length);
			this.index.Add(code, (ulong)this.file.Length);
			this.indexFile.Seek(0, SeekOrigin.End);
			this.indexFile.Write(this.indexArray, 0, 16);
			this.indexFile.Flush();

			this.file.Seek((long)this.index[code], SeekOrigin.Begin);
			this.file.Write(buffer, 0, buffer.Length);
		}

		this.file.Flush();
	}

	#nullable enable
	public CharacterSheet? LoadCharacterSheet(ulong code){
		if(!CharacterExists(code))
			return null;

		this.file.Seek((long)this.index[code], SeekOrigin.Begin);
		this.file.Read(buffer, 0, buffer.Length);

		return NetDecoder.ReadCharacterSheet(buffer, 0);
	}
	#nullable disable

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