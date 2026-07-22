using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using Object = UnityEngine.Object;

public class InventoryLoader : BaseLoader {
	private static readonly string INVENTORY_LIST_RESPATH = "Inventory/INVENTORIES";

	private static readonly CultureInfo parsingCulture = CultureInfo.InvariantCulture;
	private static bool isClient;

	// Item Information
	private static Dictionary<InventoryType, Inventory> inventories = new Dictionary<InventoryType, Inventory>();
	private static Dictionary<InventoryType, int> inventorySizes = new Dictionary<InventoryType, int>();


	public InventoryLoader(bool client){
		isClient = client;
	}

	public override bool Load(){
		LoadInventories(isClient);

		return true;
	}

	public static Inventory GetInventory(InventoryType type){return inventories[type];}
	public static int GetInventorySize(InventoryType type){return inventorySizes[type];}

	private void LoadInventories(bool isClient){
		TextAsset textAsset;
		Wrapper<Inventory> wrapper;

		textAsset = Resources.Load<TextAsset>(INVENTORY_LIST_RESPATH);

		if(textAsset != null){
			wrapper = JsonUtility.FromJson<Wrapper<Inventory>>(JsonFormatter.RemoveComments(textAsset.text));

			foreach(Inventory inventory in wrapper.data){
				if(isClient){
					inventory.PostDeserializationSetup();
					inventories.Add(inventory.GetInventoryType(), inventory);
				}

				inventorySizes.Add(inventory.GetInventoryType(), inventory.GetLimit());
			}
		}
		else{
			throw new DeserializationErrorException($"[InventoryLoader] Failed to find inventory config json at: {INVENTORY_LIST_RESPATH}");
		}
	}
}