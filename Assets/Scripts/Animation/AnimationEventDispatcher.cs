using UnityEngine;
using System.Collections.Generic;

public class AnimationEventDispatcher : MonoBehaviour {
    public ChunkLoader cl;
    private bool isPlayer = false;
    private ulong entityID;

    public void DispatchAnimationBehaviour(string data){
        ClipIndexPair cip = JsonUtility.FromJson<ClipIndexPair>(data);
        List<AnimationBehaviour> eventList = AnimationBehaviour.Get(cip.clip);

        if(cip.index >= 0 && cip.index < eventList.Count){
            eventList[cip.index].Run(cl, this.gameObject, this.entityID, this.isPlayer);
        }
    }

    public void Init(ChunkLoader cl, ulong entity){
        this.cl = cl;
        this.entityID = entity;

        if(this.entityID == Configurations.accountID){this.isPlayer = true;}        
    }
}