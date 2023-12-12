using System;
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

	// Skills
	private SkillExp alchemy;
	private SkillExp bloodmancy;
	private SkillExp crafting;
	private SkillExp combat;
	private SkillExp construction;
	private SkillExp cooking;
	private SkillExp enchanting;
	private SkillExp farming;
	private SkillExp fishing;
	private SkillExp leadership;
	private SkillExp mining;
	private SkillExp mounting;
	private SkillExp musicality;
	private SkillExp naturalism;
	private SkillExp smithing;
	private SkillExp sorcery;
	private SkillExp thievery;
	private SkillExp technology;
	private SkillExp thaumaturgy;
	private SkillExp transmuting;
	private SkillExp witchcraft;

	private SkillType mainSkill;
	private SkillType secondarySkill;

	// Inventory (No need to save in .CDAT)
	private Inventory inventory = new Inventory(InventoryType.PLAYER);
	private Inventory hotbar = new Inventory(InventoryType.HOTBAR);

	// Helpers and Handlers
	private Dictionary<SkillType, SkillExp> skillDict = new Dictionary<SkillType, SkillExp>();


	public CharacterSheet(){
		this.skillDict.Add(SkillType.ALCHEMY, this.alchemy);
		this.skillDict.Add(SkillType.BLOODMANCY, this.bloodmancy);
		this.skillDict.Add(SkillType.CRAFTING, this.crafting);
		this.skillDict.Add(SkillType.COMBAT, this.combat);
		this.skillDict.Add(SkillType.CONSTRUCTION, this.construction);
		this.skillDict.Add(SkillType.COOKING, this.cooking);
		this.skillDict.Add(SkillType.ENCHANTING, this.enchanting);
		this.skillDict.Add(SkillType.FARMING, this.farming);
		this.skillDict.Add(SkillType.FISHING, this.fishing);
		this.skillDict.Add(SkillType.LEADERSHIP, this.leadership);
		this.skillDict.Add(SkillType.MINING, this.mining);
		this.skillDict.Add(SkillType.MOUNTING, this.mounting);
		this.skillDict.Add(SkillType.MUSICALITY, this.musicality);
		this.skillDict.Add(SkillType.NATURALISM, this.naturalism);
		this.skillDict.Add(SkillType.SMITHING, this.smithing);
		this.skillDict.Add(SkillType.SORCERY, this.sorcery);
		this.skillDict.Add(SkillType.THIEVERY, this.thievery);
		this.skillDict.Add(SkillType.TECHNOLOGY, this.technology);
		this.skillDict.Add(SkillType.THAUMATURGY, this.thaumaturgy);
		this.skillDict.Add(SkillType.TRANSMUTING, this.transmuting);
		this.skillDict.Add(SkillType.WITCHCRAFT, this.witchcraft);
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
	public void SetAlchemy(SkillExp a) {this.alchemy = a;}
	public void SetBloodmancy(SkillExp b) {this.bloodmancy = b;}
	public void SetCrafting(SkillExp c) {this.crafting = c;}
	public void SetCombat(SkillExp c) {this.combat = c;}
	public void SetConstruction(SkillExp c) {this.construction = c;}
	public void SetCooking(SkillExp c) {this.cooking = c;}
	public void SetEnchanting(SkillExp e) {this.enchanting = e;}
	public void SetFarming(SkillExp f) {this.farming = f;}
	public void SetFishing(SkillExp f) {this.fishing = f;}
	public void SetLeadership(SkillExp l) {this.leadership = l;}
	public void SetMining(SkillExp m) {this.mining = m;}
	public void SetMounting(SkillExp m) {this.mounting = m;}
	public void SetMusicality(SkillExp m) {this.musicality = m;}
	public void SetNaturalism(SkillExp n) {this.naturalism = n;}
	public void SetSmithing(SkillExp s) {this.smithing = s;}
	public void SetSorcery(SkillExp s) {this.sorcery = s;}
	public void SetThievery(SkillExp t) {this.thievery = t;}
	public void SetTechnology(SkillExp t) {this.technology = t;}
	public void SetThaumaturgy(SkillExp t) {this.thaumaturgy = t;}
	public void SetTransmuting(SkillExp t) {this.transmuting = t;}
	public void SetWitchcraft(SkillExp w) {this.witchcraft = w;}
	public void SetMainSkill(SkillType m) {this.mainSkill = m;}
	public void SetSecondarySkill(SkillType s) {this.secondarySkill = s;}
}