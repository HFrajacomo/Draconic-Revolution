using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

public class ItemLoader : BaseLoader {
	private static readonly string ITEM_LIST_RESPATH = "Textures/Items/ITEM_LIST";
	private static readonly string ITEM_RESPATH = "Textures/Items/";
	private static readonly string NULL_ITEM_RESPATH = "Textures/Items/BASE_NULL";

	private static readonly CultureInfo parsingCulture = CultureInfo.InvariantCulture;
	private static bool isClient;

	// Item Information
	private static Item[] itemBook;
	private static Dictionary<string, ushort> codenameToItemID = new Dictionary<string, ushort>();

	// Texture Information
	private static Dictionary<ushort, Texture2D> textureBank = new Dictionary<ushort, Texture2D>();

	// Cached Information
	private static List<string> itemEntries = new List<string>();

	// Counters
	private static int amountOfItems = 0;


	public ItemLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		ParseItemList();
		LoadItems();

		return true;
	}

	public static ushort GetID(string codename){return codenameToItemID[codename];}
	public static Item GetItem(ushort id){return itemBook[id];}
	public static Item GetItem(string codename){return itemBook[codenameToItemID[codename]];}
	public static Item GetCopy(ushort id){return itemBook[id].Copy();}
	public static Item GetCopy(string codename){return itemBook[codenameToItemID[codename]].Copy();}
	public static Texture2D GetSprite(ushort id){return textureBank[id];}
	public static Texture2D GetSprite(ItemStack its){return textureBank[its.GetItem().GetID()];}

	private void ParseItemList(){
		TextAsset textAsset = Resources.Load<TextAsset>(ITEM_LIST_RESPATH);

		if(textAsset == null){
			Debug.Log("Couldn't Locate the ITEM_LIST while loading the ItemLoader");
			Application.Quit();
		}


		foreach(string line in textAsset.text.Replace("\r", "").Split("\n")){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;
			if(line[0] == ' ')
				continue;

			itemEntries.Add(line);
			amountOfItems++;
		}

		if(amountOfItems > ushort.MaxValue){
			Debug.Log("Number of items is bigger than ushort limitation. Draconic revolution cannot deal with that amount of items");
			Application.Quit();
		}
	}

	private void LoadItems(){
		TextAsset textAsset;
		Texture2D texture;
		Item serializedItem;

		List<Item> itemList = new List<Item>();

		ushort i = 1;

		InsertNullItem(itemList);

		foreach(string item in itemEntries){
			textAsset = Resources.Load<TextAsset>($"{ITEM_RESPATH}{item}");
			texture = Resources.Load<Texture2D>($"{ITEM_RESPATH}{item}");

			if(textAsset != null && texture != null){
				serializedItem = ItemDeserializer.DeserializeItem(textAsset.text);
				serializedItem.SetID(i);
				itemList.Add(serializedItem);

				codenameToItemID.Add(item, i);
				textureBank.Add(i, texture);

				i++;
			}
			else{
				Debug.Log($"Item codename: {item} has no JSON or Texture information and wasn't loaded");
				Application.Quit();
			}
		}

		itemBook = itemList.ToArray();
	}

	private void InsertNullItem(List<Item> itemList){
		TextAsset textAsset = Resources.Load<TextAsset>(NULL_ITEM_RESPATH);
		Item serializedItem = ItemDeserializer.DeserializeItem(textAsset.text);

		itemList.Add(serializedItem);
		codenameToItemID.Add("BASE_NULL", 0);
	}

	public void RunPostDeserializationRoutine(){
		foreach(Item it in itemBook){
			it.SetupAfterSerialize(isClient);
		}
	}
}