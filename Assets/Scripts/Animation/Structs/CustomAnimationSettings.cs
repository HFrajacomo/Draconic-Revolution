public struct CustomAnimationSettings {
	public CustomAnimationType type;
	public float duration;
	public float pulseDuration;
	public float extraArg; // Used for further configuration

	public CustomAnimationSettings(CustomAnimationType t, float d, float pd, float ea){
		this.type = t;
		this.duration = d;
		this.pulseDuration = pd;
		this.extraArg = ea;
	}

	public CustomAnimationSettings(CustomAnimationType t, float d){
		this.type = t;
		this.duration = d;
		this.pulseDuration = 0f;
		this.extraArg = 0f;
	}
}