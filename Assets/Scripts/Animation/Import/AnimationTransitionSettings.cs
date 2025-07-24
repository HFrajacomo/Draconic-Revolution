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
	private TransitionInterruptionSource interruption;

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
	}
}
