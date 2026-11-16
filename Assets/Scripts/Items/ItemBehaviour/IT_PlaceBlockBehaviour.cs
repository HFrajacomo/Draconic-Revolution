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
		int x = targetBlock.GetWorldX();
		int y = targetBlock.GetWorldY();
		int z = targetBlock.GetWorldZ();

		// Won't happen if not raycasting something
		if(!currentHitBlock.active)
			return false;

		if(loader.GetBlock(targetBlock) != 0 || loader.placementPredictor.Has(targetBlock))
			return false;

		// Voxels that add to the terrain collider can't be placed inside players
		if(VoxelLoader.CheckAddsToCollider(blockCode) && (loader.playerMovement.OccupiesVoxel(x, y, z) || loader.client.entityHandler.IsVoxelOccupiedByPlayer(x, y, z)))
			return false;

		NetMessage message = new NetMessage(NetCode.PLACEBLOCK);
		message.PlaceBlock(targetBlock.GetChunkPos(), targetBlock.blockX, targetBlock.blockY, targetBlock.blockZ, loader.playerRaycast.facing, blockCode, PlayerHotbarHandler.hotbarSlot, newQuantity, 1, itemID, loader.playerMovement.IsInJump());
		loader.client.Send(message);
		loader.placementPredictor.Predict(targetBlock, blockCode);
		return true;
	}
}