using UnityEngine;
using System.Collections.Generic;

public class AnimationEventDispatcher : MonoBehaviour {
    public ChunkLoader cl;
    private AnimationHandler animationHandler;
    private bool isPlayer = false;
    private ulong entityID;

    public void DispatchAnimationBehaviour(string data){
        ClipIndexPair cip = JsonUtility.FromJson<ClipIndexPair>(JsonFormatter.RemoveComments(data));
        List<AnimationBehaviour> eventList = AnimationBehaviour.Get(cip.clip);

        Debug.Log($"Dispatch: {data}");

        if(cip.index >= 0 && cip.index < eventList.Count){
            eventList[cip.index].Run(cl, this.gameObject, this.animationHandler, this.entityID, this.isPlayer);
        }
    }

    public void Init(ChunkLoader cl, AnimationHandler handler, ulong entity){
        this.cl = cl;
        this.animationHandler = handler;
        this.entityID = entity;

        if(this.entityID == Configurations.accountID){this.isPlayer = true;}        
    }
}