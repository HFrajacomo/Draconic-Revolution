using System.Collections.Generic;

/*
Persistent effects (looping SFX, VFX and the moving voxel light) created by Animation Events of the local player

Those effects are only removed by other Animation Events (e.g. the end of a Sheathe clip). If those events are skipped, like when the
Animator controllers are replaced or a state is cut, the effects would stay forever, locally and in every other client.
Each effect keeps whether it exists locally and whether its creation was sent to the Server, so ClearAll can undo exactly what is left.
*/
public class OwnedAnimationEffects {
	private struct EffectState {
		public bool local;
		public bool sent;
	}

	private Dictionary<string, EffectState> sfx = new Dictionary<string, EffectState>();
	private Dictionary<string, EffectState> vfx = new Dictionary<string, EffectState>();
	private EffectState voxelLight;

	public void SetSFX(string name, bool created, bool changedLocally, bool sent){Set(this.sfx, name, created, changedLocally, sent);}
	public void SetVFX(string name, bool created, bool changedLocally, bool sent){Set(this.vfx, name, created, changedLocally, sent);}

	public void SetVoxelLight(bool created, bool changedLocally, bool sent){
		if(changedLocally)
			this.voxelLight.local = created;
		if(sent)
			this.voxelLight.sent = created;
	}

	// Removes every effect that is still active and informs the Server about the ones it knows of
	public void ClearAll(ChunkLoader cl, ulong entityID){
		EntityID id = new EntityID(EntityType.PLAYER, entityID);
		NetMessage message;

		foreach(KeyValuePair<string, EffectState> item in this.sfx){
			if(item.Value.local)
				cl.sfx.RemoveEntitySFX(id, item.Key);

			if(item.Value.sent){
				message = new NetMessage(NetCode.ENTITYSFX);
				message.EntitySFX(EntityEffectOperation.DESTROY, new EntitySFXData(item.Key));
				cl.client.Send(message);
			}
		}

		foreach(KeyValuePair<string, EffectState> item in this.vfx){
			if(item.Value.local)
				cl.vfx.Remove(entityID, item.Key);

			if(item.Value.sent){
				message = new NetMessage(NetCode.ENTITYVFX);
				message.EntityVFX(EntityEffectOperation.DESTROY, new EntityVFXData(item.Key));
				cl.client.Send(message);
			}
		}

		if(this.voxelLight.local)
			cl.voxelLightHandler.Remove(id);

		if(this.voxelLight.sent){
			message = new NetMessage(NetCode.ENTITYVOXELLIGHT);
			message.EntityVoxelLight(EntityEffectOperation.DESTROY, 0f);
			cl.client.Send(message);
		}

		this.sfx.Clear();
		this.vfx.Clear();
		this.voxelLight = new EffectState();
	}

	private void Set(Dictionary<string, EffectState> effects, string name, bool created, bool changedLocally, bool sent){
		EffectState state;

		effects.TryGetValue(name, out state);

		if(changedLocally)
			state.local = created;
		if(sent)
			state.sent = created;

		if(state.local || state.sent)
			effects[name] = state;
		else
			effects.Remove(name);
	}
}
