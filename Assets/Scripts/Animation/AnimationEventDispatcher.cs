using UnityEngine;
using System.Collections.Generic;

public class AnimationEventDispatcher : MonoBehaviour {
    public ChunkLoader cl;
    private bool isPlayer = false;
    private ulong entityID;

    public void DispatchAnimationBehaviour(string clipName, int index){
        List<AnimationBehaviour> eventList = AnimationBehaviour.Get(clipName);

        if(index >= 0 && index < eventList.Count){
            eventList[index].Run(cl, this.gameObject, this.entityID, this.isPlayer);
        }
    }

    public void Init(ChunkLoader cl, ulong entity){
        this.cl = cl;
        this.entityID = entity;

        if(this.entityID == Configurations.accountID){this.isPlayer = true;}        
    }
}