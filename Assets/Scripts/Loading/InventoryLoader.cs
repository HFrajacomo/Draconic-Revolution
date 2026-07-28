using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

public class InventoryLoader : BaseLoader {
	private static readonly string INVENTORY_LIST_RESPATH = "Inventory/INVENTORIES";
	private static readonly string INVENTORY_RESPATH = "Inventory/";

	private static readonly CultureInfo parsingCulture = CultureInfo.InvariantCulture;
	private static bool isClient;

	// Item Information
	private static Dictionary<string, Inventory> inventories = new Dictionary<string, Inventory>();
	private static Dictionary<byte, Inventory> inventoriesByID = new Dictionary<byte, Inventory>();
	private static Dictionary<int, int> inventorySizes = new Dictionary<int, int>();
	private static Dictionary<string, Texture2D> inventoryIcons = new Dictionary<string, Texture2D>();


	public InventoryLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		LoadInventories(isClient);

		return true;
	}

	public static Inventory GetInventory(string type){return inventories[type].Copy();}
	public static Inventory GetInventory(byte id){return inventoriesByID[id].Copy();}
	public static byte GetInventoryID(string type){return inventories[type].GetID();}
	public static int GetInventorySize(byte id){return inventorySizes[id];}
	public static int GetInventorySize(string name){return inventorySizes[inventories[name].GetID()];}
	public static int GetColumnCount(string type){return inventories[type].columnCount;}
	public static bool GetPickupTarget(byte id){return inventoriesByID[id].isPickupTarget;}
	public static Texture2D GetSlotIcon(string iconName){return inventoryIcons[iconName];}

	private void LoadInventories(bool isClient){
		TextAsset textAsset;
		Wrapper<Inventory> wrapper;
		Texture2D texture;

		textAsset = Resources.Load<TextAsset>(INVENTORY_LIST_RESPATH);
		byte inventoryID = 0;

		if(textAsset != null){
			wrapper = JsonUtility.FromJson<Wrapper<Inventory>>(JsonFormatter.RemoveComments(textAsset.text));

			foreach(Inventory inventory in wrapper.data){
				inventory.PostDeserializationSetup(inventoryID);
				inventories.Add(inventory.GetInventoryType(), inventory);
				inventoriesByID.Add(inventoryID, inventory);
				inventorySizes.Add(inventoryID, inventory.GetLimit());

				if(isClient){
					foreach(string filepath in inventory.GetIconFilepaths()){
						texture = Resources.Load<Texture2D>($"{INVENTORY_RESPATH}{filepath}");

						if(texture == null)
							throw new DeserializationErrorException($"[InventoryLoader] Couldn't find inventory default icon {filepath} for inventory: {inventory.GetInventoryType()}");

						inventoryIcons.Add(filepath, texture);
					}
				}

				inventoryID++;
			}
		}
		else{
			throw new DeserializationErrorException($"[InventoryLoader] Failed to find inventory config json at: {INVENTORY_LIST_RESPATH}");
		}
	}
}