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

		if(this.PlaceBlock(cl, blockID, it.GetID(), (byte)(connectedStack.GetAmount()-1), ea)){
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

	// Origin of the placed item is sent so the server can echo it back in case of a PlacementDenied
	private bool PlaceBlock(ChunkLoader cl, ushort blockCode, ushort itemID, byte newQuantity, EntityAction ea){
		PlayerRaycast raycast = cl.playerRaycast;
		CastCoord targetBlock = raycast.GetLastHitCoords();
		int x = targetBlock.GetWorldX();
		int y = targetBlock.GetWorldY();
		int z = targetBlock.GetWorldZ();

		// Won't happen if not raycasting something
		if(!raycast.GetCurrentHitCoords().active)
			return false;

		if(cl.GetBlock(targetBlock) != 0 || cl.placementPredictor.Has(targetBlock))
			return false;

		// Voxels that add to the terrain collider can't be placed inside players
		if(VoxelLoader.CheckAddsToCollider(blockCode) && (cl.playerMovement.OccupiesVoxel(x, y, z) || cl.client.entityHandler.IsVoxelOccupiedByPlayer(x, y, z)))
			return false;

		NetMessage message = new NetMessage(NetCode.PLACEBLOCK);
		message.PlaceBlock(targetBlock.GetChunkPos(), targetBlock.blockX, targetBlock.blockY, targetBlock.blockZ, cl.playerRaycast.facing, blockCode, ea.GetConnectedStackSlot(), newQuantity, ea.GetConnectedStackInventory(), itemID, cl.playerMovement.IsInJump(), actionSlot:PlayerHotbarHandler.attackHotbarSlot, actionID:ea.GetID());
		cl.client.Send(message);
		cl.placementPredictor.Predict(targetBlock, blockCode);
		return true;
	}
}