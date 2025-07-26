using System;
using UnityEditor.Animations;

[Serializable]
public class AnimationTransitionSettings {
	public string layer;
	public string sourceState;
	public string destinationState;
	public bool hasExitTime;
	public float exitTime;
	public float duration;
	public float offset;
	public bool canTransitionToSelf;
	public string interruptionSource;
	public AnimationTransitionConditionSettings[] conditions;
	private bool isFromAny = false;
	private TransitionInterruptionSource interruption;

	public AnimatorStateTransition Build(){
		AnimatorStateTransition ast = new AnimatorStateTransition();

		ast.canTransitionToSelf = this.canTransitionToSelf;
		ast.duration = this.duration;
		ast.exitTime = this.exitTime;
		ast.hasExitTime = this.hasExitTime;
		ast.interruptionSource = this.interruption;
		ast.offset = this.offset;
		ast.name = $"{this.sourceState} -> {this.destinationState}";

		return ast;
	}

	public void Copy(AnimatorController controller, AnimatorStateTransition other){
		other.canTransitionToSelf = this.canTransitionToSelf;
		other.duration = this.duration;
		other.exitTime = this.exitTime;
		other.hasExitTime = this.hasExitTime;
		other.interruptionSource = this.interruption;
		other.offset = this.offset;
		other.name = $"{this.sourceState} -> {this.destinationState}";

		if(this.conditions != null){
			foreach(AnimationTransitionConditionSettings condition in this.conditions){
				other.AddCondition(condition.GetMode(), condition.threshold, condition.parameter);

				if(!CheckControllerHasParameter(condition.parameter, controller)){
					controller.AddParameter(condition.BuildParameter());
				}
			}
		}
	}

	public bool IsTransitionFromAny(){return this.isFromAny;}

	public TransitionInterruptionSource GetInterruption(){return this.interruption;}

	public void PostDeserializationSetup(){
		switch(this.interruptionSource){
			case "none":
				this.interruption = TransitionInterruptionSource.None;
				break;
			case "source":
				this.interruption = TransitionInterruptionSource.Source;
				break;
			case "destination":
				this.interruption = TransitionInterruptionSource.Destination;
				break;
			case "source then destination":
				this.interruption = TransitionInterruptionSource.SourceThenDestination;
				break;
			case "destination then source":
				this.interruption = TransitionInterruptionSource.DestinationThenSource;
				break;
			default:
				this.interruption = TransitionInterruptionSource.None;
				break;
		}

		if(this.sourceState == "any")
			this.isFromAny = true;

		if(this.conditions != null){
			for(int i=0; i < this.conditions.Length; i++){
				this.conditions[i].PostDeserializationSetup();
			}
		}

		if(this.layer == "")
			this.layer = "Base Layer";
	}

	private bool CheckControllerHasParameter(string name, AnimatorController controller){
		for(int i=0; i < controller.parameters.Length; i++){
			if(controller.parameters[i].name == name){
				return true;
			}
		}
		return false;
	}
}
