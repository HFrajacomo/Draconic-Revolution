using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryButton : MonoBehaviour, IPointerClickHandler
{
	[SerializeField]
	public byte inventoryCode;
	[SerializeField]
	public ushort slot;
	[SerializeField]
	public InventoryUIPlayer invController;

    public void OnPointerClick(PointerEventData ped){
    	if(ped.button == PointerEventData.InputButton.Right){
    		invController.RightClick(inventoryCode, slot);
    	}
    	else if(ped.button == PointerEventData.InputButton.Left){
    		invController.LeftClick(inventoryCode, slot);
    	}
    }
}
