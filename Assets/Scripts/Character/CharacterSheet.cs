using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSheet{
	// Header
	private string name;
	private Alignment alignment;
	private Race race;
	private Religion religion;
	private bool isMale;
	private byte cronology;

	// Handler
	private SpecialEffectHandler specialEffectHandler;

	// Visuals
	private CharacterAppearance characterAppearance;

	// Main Bars
	private DepletableAttribute health;
	private DepletableAttribute mana;
	private DepletableAttribute power;
	private DepletableAttribute sanity;
	private DepletableAttribute protection;
	private DepletableAttribute equipmentWeight;
	private DepletableAttribute poise;

	// Base Attributes
	private Attribute strength;
	private Attribute precision;
	private Attribute vitality;
	private Attribute evasion;
	private Attribute magic;
	private Attribute charisma;

	// Secondary Attributes
	private Attribute fireResistance;
	private Attribute coldResistance;
	private Attribute lightningResistance;
	private Attribute poisonResistance;
	private Attribute curseResistance;
	private Attribute speed;

	// Defense values
	private ushort physicalDefense;
	private ushort magicalDefense;
	private float damageReductionMultiplier;

	// Flags
	private bool hasBlood;
	private bool isWeaponDrawn;
	private bool isImortal;

	// Equipment
	private Item rightHand;
	private Item leftHand;
	private Item helmet;
	private Item armor;
	private Item legs;
	private Item boots;
	private Item ring1;
	private Item ring2;
	private Item ring3;
	private Item ring4;
	private Item amulet;
	private Item cape;

	private SkillType mainSkill;
	private SkillType secondarySkill;

	// Inventory (No need to save in .CDAT)
	private Inventory inventory = new Inventory(InventoryType.PLAYER);
	private Inventory hotbar = new Inventory(InventoryType.HOTBAR);

	// Helpers and Handlers
	private Dictionary<SkillType, SkillExp> skillDict = new Dictionary<SkillType, SkillExp>();

	// DEBUG
	private List<string> DEBUG_LIST = new List<string>();


	public CharacterSheet(){
		this.specialEffectHandler = new SpecialEffectHandler();

		this.skillDict.Add(SkillType.ALCHEMY, null);
		this.skillDict.Add(SkillType.BLOODMANCY, null);
		this.skillDict.Add(SkillType.CRAFTING, null);
		this.skillDict.Add(SkillType.COMBAT, null);
		this.skillDict.Add(SkillType.CONSTRUCTION, null);
		this.skillDict.Add(SkillType.COOKING, null);
		this.skillDict.Add(SkillType.ENCHANTING, null);
		this.skillDict.Add(SkillType.FARMING, null);
		this.skillDict.Add(SkillType.FISHING, null);
		this.skillDict.Add(SkillType.LEADERSHIP, null);
		this.skillDict.Add(SkillType.MINING, null);
		this.skillDict.Add(SkillType.MOUNTING, null);
		this.skillDict.Add(SkillType.MUSICALITY, null);
		this.skillDict.Add(SkillType.NATURALISM, null);
		this.skillDict.Add(SkillType.SMITHING, null);
		this.skillDict.Add(SkillType.SORCERY, null);
		this.skillDict.Add(SkillType.THIEVERY, null);
		this.skillDict.Add(SkillType.TECHNOLOGY, null);
		this.skillDict.Add(SkillType.THAUMATURGY, null);
		this.skillDict.Add(SkillType.TRANSMUTING, null);
		this.skillDict.Add(SkillType.WITCHCRAFT, null);

		this.rightHand = null;
		this.leftHand = null;
		this.helmet = null;
		this.armor = null;
		this.legs = null;
		this.boots = null;
		this.ring1 = null;
		this.ring2 = null;
		this.ring3 = null;
		this.ring4 = null;
		this.amulet = null;
		this.cape =  null;
	}

	// Getter

	public SkillExp GetSkill(SkillType t){return this.skillDict[t];}
	public byte GetSkillLevel(SkillType t){return this.skillDict[t].GetLevel();}
	public string GetName() {return this.name;}
	public Alignment GetAlignment() {return this.alignment;}
	public Race GetRace() {return this.race;}
	public Religion GetReligion() {return this.religion;}
	public bool GetGender() {return this.isMale;}
	public byte GetCronology() {return this.cronology;}
	public SpecialEffectHandler GetSpecialEffectHandler() {return this.specialEffectHandler;}
	public CharacterAppearance GetCharacterAppearance() {return this.characterAppearance;}
	public DepletableAttribute GetHealth() {return this.health;}
	public DepletableAttribute GetMana() {return this.mana;}
	public DepletableAttribute GetPower() {return this.power;}
	public DepletableAttribute GetSanity() {return this.sanity;}
	public DepletableAttribute GetProtection() {return this.protection;}
	public DepletableAttribute GetEquipmentWeight() {return this.equipmentWeight;}
	public Attribute GetStrength() {return this.strength;}
	public Attribute GetPrecision() {return this.precision;}
	public Attribute GetVitality() {return this.vitality;}
	public Attribute GetEvasion() {return this.evasion;}
	public Attribute GetMagic() {return this.magic;}
	public Attribute GetCharisma() {return this.charisma;}
	public Attribute GetFireResistance() {return this.fireResistance;}
	public Attribute GetColdResistance() {return this.coldResistance;}
	public Attribute GetLightningResistance() {return this.lightningResistance;}
	public Attribute GetPoisonResistance() {return this.poisonResistance;}
	public Attribute GetCurseResistance() {return this.curseResistance;}
	public Attribute GetSpeed() {return this.speed;}
	public DepletableAttribute GetPoise() {return this.poise;}
	public ushort GetPhysicalDefense() {return this.physicalDefense;}
	public ushort GetMagicalDefense() {return this.magicalDefense;}
	public float GetDamageReductionMultiplier() {return this.damageReductionMultiplier;}
	public bool HasBlood() {return this.hasBlood;}
	public bool IsWeaponDrawn() {return this.isWeaponDrawn;}
	public bool IsImortal() {return this.isImortal;}
	public Item GetRightHand() {return this.rightHand;}
	public Item GetLeftHand() {return this.leftHand;}
	public Item GetHelmet() {return this.helmet;}
	public Item GetArmor() {return this.armor;}
	public Item GetLegs() {return this.legs;}
	public Item GetBoots() {return this.boots;}
	public Item GetRing1() {return this.ring1;}
	public Item GetRing2() {return this.ring2;}
	public Item GetRing3() {return this.ring3;}
	public Item GetRing4() {return this.ring4;}
	public Item GetAmulet() {return this.amulet;}
	public Item GetCape() {return this.cape;}
	public SkillType GetMainSkill() {return this.mainSkill;}
	public SkillType GetSecondarySkill() {return this.secondarySkill;}


	// Setter

	public void SetName(string n) {this.name = n;}
	public void SetAlignment(Alignment a) {this.alignment = a;}
	public void SetRace(Race r) {this.race = r;}
	public void SetReligion(Religion r) {this.religion = r;}
	public void SetGender(bool isMale) {this.isMale = isMale;}
	public void SetCronology(byte c) {this.cronology = c;}
	public void SetSpecialEffectHandler(SpecialEffectHandler s) {this.specialEffectHandler = s;}
	public void SetCharacterAppearance(CharacterAppearance c) {this.characterAppearance = c;}
	public void SetHealth(DepletableAttribute h) {this.health = h;}
	public void SetMana(DepletableAttribute m) {this.mana = m;}
	public void SetPower(DepletableAttribute p) {this.power = p;}
	public void SetSanity(DepletableAttribute s) {this.sanity = s;}
	public void SetProtection(DepletableAttribute p) {this.protection = p;}
	public void SetEquipmentWeight(DepletableAttribute e) {this.equipmentWeight = e;}
	public void SetStrength(Attribute s) {this.strength = s;}
	public void SetPrecision(Attribute p) {this.precision = p;}
	public void SetVitality(Attribute v) {this.vitality = v;}
	public void SetEvasion(Attribute e) {this.evasion = e;}
	public void SetMagic(Attribute m) {this.magic = m;}
	public void SetCharisma(Attribute c) {this.charisma = c;}
	public void SetFireResistance(Attribute f) {this.fireResistance = f;}
	public void SetColdResistance(Attribute c) {this.coldResistance = c;}
	public void SetLightningResistance(Attribute l) {this.lightningResistance = l;}
	public void SetPoisonResistance(Attribute p) {this.poisonResistance = p;}
	public void SetCurseResistance(Attribute c) {this.curseResistance = c;}
	public void SetSpeed(Attribute s) {this.speed = s;}
	public void SetPoise(DepletableAttribute p) {this.poise = p;}
	public void SetPhysicalDefense(ushort p) {this.physicalDefense = p;}
	public void SetMagicalDefense(ushort m) {this.magicalDefense = m;}
	public void SetDamageReductionMultiplier(float d) {this.damageReductionMultiplier = d;}
	public void SetHasBlood(bool h) {this.hasBlood = h;}
	public void SetIsWeaponDrawn(bool i) {this.isWeaponDrawn = i;}
	public void SetIsImortal(bool i) {this.isImortal = i;}
	public void SetRightHand(Item r) {this.rightHand = r;}
	public void SetLeftHand(Item l) {this.leftHand = l;}
	public void SetHelmet(Item h) {this.helmet = h;}
	public void SetArmor(Item a) {this.armor = a;}
	public void SetLegs(Item l) {this.legs = l;}
	public void SetBoots(Item b) {this.boots = b;}
	public void SetRing1(Item r) {this.ring1 = r;}
	public void SetRing2(Item r) {this.ring2 = r;}
	public void SetRing3(Item r) {this.ring3 = r;}
	public void SetRing4(Item r) {this.ring4 = r;}
	public void SetAmulet(Item a) {this.amulet = a;}
	public void SetCape(Item c) {this.cape = c;}
	public void SetSkill(SkillType type, SkillExp exp) {this.skillDict[type] = exp;}
	public void SetMainSkill(SkillType m) {this.mainSkill = m;}
	public void SetSecondarySkill(SkillType s) {this.secondarySkill = s;}

	public void DebugPrint(){
		this.DEBUG_LIST.Add("Name: " + GetName());
		this.DEBUG_LIST.Add("Alignment: " + GetAlignment());
		this.DEBUG_LIST.Add("Religion: " + GetReligion());
		this.DEBUG_LIST.Add("Race: " + GetRace());
		this.DEBUG_LIST.Add("IsMale: " + GetGender());
		this.DEBUG_LIST.Add("Cronology: " + GetCronology());
		this.DEBUG_LIST.Add("STR: " + GetStrength());
		this.DEBUG_LIST.Add("PRE: " + GetPrecision());
		this.DEBUG_LIST.Add("VIT: " + GetVitality());
		this.DEBUG_LIST.Add("EVA: " + GetEvasion());
		this.DEBUG_LIST.Add("MAG: " + GetMagic());
		this.DEBUG_LIST.Add("CAR: " + GetCharisma());
		this.DEBUG_LIST.Add("FR: " + GetFireResistance());
		this.DEBUG_LIST.Add("IR: " + GetColdResistance());
		this.DEBUG_LIST.Add("LR: " + GetLightningResistance());
		this.DEBUG_LIST.Add("PR: " + GetPoisonResistance());
		this.DEBUG_LIST.Add("CR: " + GetCurseResistance());
		this.DEBUG_LIST.Add("SPD: " + GetSpeed());
		this.DEBUG_LIST.Add("Health: " + GetHealth());
		this.DEBUG_LIST.Add("Mana: " + GetMana());
		this.DEBUG_LIST.Add("Power: " + GetPower());
		this.DEBUG_LIST.Add("Sanity: " + GetSanity());
		this.DEBUG_LIST.Add("Protection: " + GetProtection());
		this.DEBUG_LIST.Add("Weight: " + GetEquipmentWeight());
		this.DEBUG_LIST.Add("Poise: " + GetPoise());
		this.DEBUG_LIST.Add("PhysDef: " + GetPhysicalDefense());
		this.DEBUG_LIST.Add("MagDef: " + GetMagicalDefense());
		this.DEBUG_LIST.Add("DmgR: " + GetDamageReductionMultiplier());
		this.DEBUG_LIST.Add("Blood: " + HasBlood());
		this.DEBUG_LIST.Add("WeaponDrawn: " + IsWeaponDrawn());
		this.DEBUG_LIST.Add("Imortal: " + IsImortal());
		this.DEBUG_LIST.Add("MainSkill: " + GetMainSkill());
		this.DEBUG_LIST.Add("SecSkill: " + GetSecondarySkill());
		this.DEBUG_LIST.Add("Alchemy: " + GetSkill(SkillType.ALCHEMY).ToString());
		this.DEBUG_LIST.Add("Bloodmancy: " + GetSkill(SkillType.BLOODMANCY).ToString());
		this.DEBUG_LIST.Add("Crafting: " + GetSkill(SkillType.CRAFTING).ToString());
		this.DEBUG_LIST.Add("Combat: " + GetSkill(SkillType.COMBAT).ToString());
		this.DEBUG_LIST.Add("Construction: " + GetSkill(SkillType.CONSTRUCTION).ToString());
		this.DEBUG_LIST.Add("Cooking: " + GetSkill(SkillType.COOKING).ToString());
		this.DEBUG_LIST.Add("Enchanting: " + GetSkill(SkillType.ENCHANTING).ToString());
		this.DEBUG_LIST.Add("Farming: " + GetSkill(SkillType.FARMING).ToString());
		this.DEBUG_LIST.Add("Fishing: " + GetSkill(SkillType.FISHING).ToString());
		this.DEBUG_LIST.Add("Leadership: " + GetSkill(SkillType.LEADERSHIP).ToString());
		this.DEBUG_LIST.Add("Mining: " + GetSkill(SkillType.MINING).ToString());
		this.DEBUG_LIST.Add("Mounting: " + GetSkill(SkillType.MOUNTING).ToString());
		this.DEBUG_LIST.Add("Musicality: " + GetSkill(SkillType.MUSICALITY).ToString());
		this.DEBUG_LIST.Add("Naturalism: " + GetSkill(SkillType.NATURALISM).ToString());
		this.DEBUG_LIST.Add("Smithing: " + GetSkill(SkillType.SMITHING).ToString());
		this.DEBUG_LIST.Add("Sorcery: " + GetSkill(SkillType.SORCERY).ToString());
		this.DEBUG_LIST.Add("Thievery: " + GetSkill(SkillType.THIEVERY).ToString());
		this.DEBUG_LIST.Add("Technology: " + GetSkill(SkillType.TECHNOLOGY).ToString());
		this.DEBUG_LIST.Add("Thaumaturgy: " + GetSkill(SkillType.THAUMATURGY).ToString());
		this.DEBUG_LIST.Add("Transmuting: " + GetSkill(SkillType.TRANSMUTING).ToString());
		this.DEBUG_LIST.Add("Witchcraft: " + GetSkill(SkillType.WITCHCRAFT).ToString());
		this.DEBUG_LIST.Add("RightHand: " + GetRightHand());
		this.DEBUG_LIST.Add("LeftHand: " + GetLeftHand());
		this.DEBUG_LIST.Add("Helmet: " + GetHelmet());
		this.DEBUG_LIST.Add("Armor: " + GetArmor());
		this.DEBUG_LIST.Add("Legs: " + GetLegs());
		this.DEBUG_LIST.Add("Boots: " + GetBoots());
		this.DEBUG_LIST.Add("Ring1: " + GetRing1());
		this.DEBUG_LIST.Add("Ring2: " + GetRing2());
		this.DEBUG_LIST.Add("Ring3: " + GetRing3());
		this.DEBUG_LIST.Add("Ring4: " + GetRing4());
		this.DEBUG_LIST.Add("Amulet: " + GetAmulet());
		this.DEBUG_LIST.Add("Cape: " + GetCape());
		this.DEBUG_LIST.Add("Appearance: " + GetCharacterAppearance());

		foreach(SpecialEffect fx in this.specialEffectHandler.GetAllEffects()){
			this.DEBUG_LIST.Add("Effect: " + fx);
		}

		foreach(string s in this.DEBUG_LIST){
			Debug.Log(s);
		}
	}
}