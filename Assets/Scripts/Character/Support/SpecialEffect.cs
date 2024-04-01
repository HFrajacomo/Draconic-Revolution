using System;

public class SpecialEffect {
	private EffectType type;
	private EffectUsecase usecase;

	private byte tickDuration;
	private ushort amountTicks;
	private EntityID source;
	private bool showToPlayer; // NO SAVE
	private bool isSystem;


	// Basic for system operations
	public SpecialEffect(EffectType t){
		this.type = t; this.tickDuration = 0; this.amountTicks = 0; this.showToPlayer = false; this.isSystem = true;
		this.usecase = EffectUsecase.SYSTEM;
	}

	// Basic for duration
	public SpecialEffect(EffectType t, byte td, ushort ticks, bool system){
		this.type = t; this.tickDuration = td; this.amountTicks = ticks; this.isSystem = system;
		this.usecase = EffectUsecase.TURN_BASED;
	}

	public SpecialEffect(EffectType t, EffectUsecase use, byte td, ushort ticks, bool system){
		this.type = t; this.tickDuration = td; this.amountTicks = ticks; this.isSystem = system;
		this.usecase = use;
	}

	public EffectType GetEffectType(){return this.type;}
	public EffectUsecase GetUsecase(){return this.usecase;}
	public byte GetTickDuration(){return this.tickDuration;}
	public ushort GetTicks(){return this.amountTicks;}
	public EntityID GetSource(){return this.source;}
	public bool GetShow(){return this.showToPlayer;}
	public bool IsSystem(){return this.isSystem;}

	// Returns true if they have the same type and usecase
	public bool Equals(SpecialEffect e){return this.type == e.GetEffectType();}

	public override string ToString(){return $"{type} | {usecase} | {tickDuration} | {amountTicks} | {isSystem}";}
}