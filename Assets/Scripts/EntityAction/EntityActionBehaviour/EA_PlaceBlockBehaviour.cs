using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class EA_PlaceBlockBehaviour : EntityActionBehaviour {
	public string blockName;

	public override EntityActionBehaviour Copy(){return new EA_PlaceBlockBehaviour();}

	public override void SetArguments(DualString[] arguments){
		for(int i=0; i < arguments.Length; i++){
			if(arguments[i].key == "blockName"){
				this.blockName = arguments[i].value;
				continue;
			}
		}
	}

	public override void OnPrimaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){
		ItemStack connectedStack = ea.GetItemStack(cl.playerInventoryManager);
		Item it = connectedStack.GetItem();
		ushort blockID;

		if(this.blockName == null || this.blockName == ""){
			blockID = VoxelLoader.GetBlockID(it.codename);
		}
		else{
			blockID = VoxelLoader.GetBlockID(this.blockName);
		}

		if(this.PlaceBlock(cl, blockID, (byte)(connectedStack.GetAmount()-1), cl)){
			cl.playerRaycast.lastBlockPlaced = it.GetID();
			cl.playerInventoryManager.SubToCounter(it.GetID(), 1);

			if(connectedStack.Decrement()){
				if(ea.GetConnectedStackInventory() == 1)
					cl.hotbarHandler.hotbar.SetNull(ea.GetConnectedStackSlot());

				cl.playerInventoryManager.SetNull(ea.GetConnectedStackInventory(), ea.GetConnectedStackSlot());
				
				if(cl.playerInventoryManager.GetItemCount(it.GetID()) <= 0){
					cl.hotbarHandler.actionHotbar.SetNull(PlayerHotbarHandler.attackHotbarSlot);
				}
				connectedStack = null;
			}

			if(ea.GetConnectedStackInventory() == 1)
				cl.hotbarHandler.DrawHotbarSlot(ea.GetConnectedStackSlot());

			cl.hotbarHandler.DrawActionHotbar();
			cl.hotbarHandler.playerInventoryManager.SendInventoryDataToServer();
		}
	}

	public override void OnSecondaryPlayer(ChunkLoader cl, EntityAction ea, ItemStack its, ulong code){OnPrimaryPlayer(cl, ea, its, code);}

	private bool PlaceBlock(ChunkLoader cl, ushort blockCode, byte newQuantity, ChunkLoader loader){
		PlayerRaycast raycast = cl.playerRaycast;
		CastCoord targetBlock = raycast.GetLastHitCoords();

		// Won't happen if not raycasting something or if block is in player's body or head
		if(!raycast.GetCurrentHitCoords().active || (CastCoord.Eq(targetBlock, raycast.GetPlayerHeadCoords()) && VoxelLoader.CheckSolid(blockCode)) || (CastCoord.Eq(targetBlock, raycast.GetPlayerBodyCoords()) && VoxelLoader.CheckSolid(blockCode))){
			return false;
		}

		if(cl.GetBlock(targetBlock) != 0)
			return false;

		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, targetBlock.GetChunkPos(), targetBlock.blockX, targetBlock.blockY, targetBlock.blockZ, cl.playerRaycast.facing, blockCode, ushort.MaxValue, ushort.MaxValue, slot:PlayerHotbarHandler.hotbarSlot, newQuantity:newQuantity);
		cl.client.Send(message);
		return true;
	}
}