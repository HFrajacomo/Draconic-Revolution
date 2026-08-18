using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
	[SerializeField]
	public byte inventoryCode;
	[SerializeField]
	public ushort slot;
	[SerializeField]
	public PlayerInventoryManager invController;
	private bool isHovered;
	private static bool ctrl = false;

    public void OnPointerClick(PointerEventData ped){
    	if(ped.button == PointerEventData.InputButton.Right){
    		invController.RightClick(inventoryCode, slot);
    	}
    	else if(ped.button == PointerEventData.InputButton.Left){
    		invController.LeftClick(inventoryCode, slot);
    	}
    }

    public void OnPointerEnter(PointerEventData eventData){this.isHovered = true;}
    public void OnPointerExit(PointerEventData eventData){this.isHovered = false;}

    public void OnCtrl(){
        if(!InventoryButton.ctrl){
            InventoryButton.ctrl = true;
        }
        // If it's release
        else
            InventoryButton.ctrl = false;    
    }

    public void OnDrop(){
    	if(this.isHovered && MainControllerManager.InUI){
            this.invController.Drop(this.inventoryCode, (byte)this.slot, InventoryButton.ctrl);
    	}
    }


    public void SetController(PlayerInventoryManager manager){this.invController = manager;}
}
