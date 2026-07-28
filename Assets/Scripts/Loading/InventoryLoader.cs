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
	private static Dictionary<InventoryType, Inventory> inventories = new Dictionary<InventoryType, Inventory>();
	private static Dictionary<InventoryType, int> inventorySizes = new Dictionary<InventoryType, int>();
	private static Dictionary<string, Texture2D> inventoryIcons = new Dictionary<string, Texture2D>();


	public InventoryLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		LoadInventories(isClient);

		return true;
	}

	public static Inventory GetInventory(InventoryType type){return inventories[type].Copy();}
	public static int GetInventorySize(InventoryType type){return inventorySizes[type];}
	public static int GetColumnCount(InventoryType type){return inventories[type].columnCount;}
	public static bool GetPickupTarget(InventoryType type){return inventories[type].isPickupTarget;}
	public static Texture2D GetSlotIcon(string iconName){return inventoryIcons[iconName];}

	private void LoadInventories(bool isClient){
		TextAsset textAsset;
		Wrapper<Inventory> wrapper;
		Texture2D texture;

		textAsset = Resources.Load<TextAsset>(INVENTORY_LIST_RESPATH);

		if(textAsset != null){
			wrapper = JsonUtility.FromJson<Wrapper<Inventory>>(JsonFormatter.RemoveComments(textAsset.text));

			foreach(Inventory inventory in wrapper.data){
				inventory.PostDeserializationSetup();
				inventories.Add(inventory.GetInventoryType(), inventory);
				inventorySizes.Add(inventory.GetInventoryType(), inventory.GetLimit());

				if(isClient){
					foreach(string filepath in inventory.GetIconFilepaths()){
						texture = Resources.Load<Texture2D>($"{INVENTORY_RESPATH}{filepath}");

						if(texture == null)
							throw new DeserializationErrorException($"[InventoryLoader] Couldn't find inventory default icon {filepath} for inventory: {inventory.GetInventoryType()}");

						inventoryIcons.Add(filepath, texture);
					}
				}
			}
		}
		else{
			throw new DeserializationErrorException($"[InventoryLoader] Failed to find inventory config json at: {INVENTORY_LIST_RESPATH}");
		}
	}
}