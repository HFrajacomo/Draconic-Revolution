using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class EAPlaceBlockBehaviour : EntityActionBehaviour {
	public override void OnPrimaryPlayer(ChunkLoader cl, ItemStack its, ulong code){
		Item it = its.GetItem();

		if(this.PlaceBlock(cl, it.GetID(), (byte)(its.GetAmount()-1), cl)){
			cl.playerRaycast.lastBlockPlaced = it.GetID();

			if(its.Decrement()){
				cl.hotbarHandler.hotbar.SetNull(PlayerHotbarHandler.hotbarSlot);
			}

			cl.hotbarHandler.DrawHotbarSlot(PlayerHotbarHandler.hotbarSlot);
			cl.hotbarHandler.playerInventoryManager.DrawSlot(1, PlayerHotbarHandler.hotbarSlot);
			cl.hotbarHandler.playerInventoryManager.SendInventoryDataToServer();
		}
	}

	public override void OnSecondaryPlayer(ChunkLoader cl, ItemStack its, ulong code){OnPrimaryPlayer(cl, its, code);}

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