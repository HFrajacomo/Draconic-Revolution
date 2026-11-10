using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class IT_PlaceBlockBehaviour : ItemBehaviour{
	public string blockName;
	private ushort blockID;

	public override void PostDeserializationSetup(bool isClient){
		this.blockID = VoxelLoader.GetBlockID(this.blockName);
	}

	public override void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		Item it = its.GetItem();

		if(this.PlaceBlock(this.blockID, it.GetID(), (byte)(its.GetAmount()-1), targetBlock, referencePoint1, referencePoint2, referencePoint3, cl)){
			if(its.Decrement()){
				cl.hotbarHandler.playerInventoryManager.SetNull(1, PlayerHotbarHandler.hotbarSlot);
			}

			cl.hotbarHandler.playerInventoryManager.SubToCounter(it.GetID(), 1);
			cl.hotbarHandler.DrawHotbarSlot(PlayerHotbarHandler.hotbarSlot);
			cl.hotbarHandler.DrawActionHotbar();
			cl.hotbarHandler.playerInventoryManager.DrawSlot(1, PlayerHotbarHandler.hotbarSlot);
			cl.hotbarHandler.playerInventoryManager.SendInventoryDataToServer();
		}
	}

	private bool PlaceBlock(ushort blockCode, ushort itemID, byte newQuantity, CastCoord targetBlock, CastCoord playerHead, CastCoord playerBody, CastCoord currentHitBlock, ChunkLoader loader){
		// Won't happen if not raycasting something or if block is in player's body or head
		if(!currentHitBlock.active || (CastCoord.Eq(targetBlock, playerHead) && VoxelLoader.CheckSolid(blockCode)) || (CastCoord.Eq(targetBlock, playerBody) && VoxelLoader.CheckSolid(blockCode))){
			return false;
		}

		if(loader.GetBlock(targetBlock) != 0)
			return false;

		NetMessage message = new NetMessage(NetCode.PLACEBLOCK);
		message.PlaceBlock(targetBlock.GetChunkPos(), targetBlock.blockX, targetBlock.blockY, targetBlock.blockZ, loader.playerRaycast.facing, blockCode, PlayerHotbarHandler.hotbarSlot, newQuantity, 1, itemID);
		loader.client.Send(message);
		return true;
	}
}