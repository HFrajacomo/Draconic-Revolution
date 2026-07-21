using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorStateMessageCallback : StateMachineBehaviour {
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        if(!animator.GetBool("ISPLAYER"))
            return;

        PlayerActionController.RegisterClientMessage(new AnimationData(AnimationHandler.GetStateName("BASE_Character", stateInfo), layerIndex));
    }
}
