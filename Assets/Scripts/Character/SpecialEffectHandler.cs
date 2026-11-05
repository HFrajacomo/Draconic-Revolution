using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEffectHandler{
	private HashSet<SpecialEffect>[] effects;

	public SpecialEffectHandler(){
		this.effects = new HashSet<SpecialEffect>[Enum.GetNames(typeof(EffectUsecase)).Length];
	}

	#nullable enable
	public HashSet<SpecialEffect>? GetList(EffectUsecase usecase){return effects[(byte)usecase];}
	#nullable disable

	public void Add(SpecialEffect e){
		int usecase = (int)e.GetUsecase();

		if(this.effects[usecase] == null){
			this.effects[usecase] = new HashSet<SpecialEffect>();
		}

		this.effects[usecase].Add(e);
	}

	public void Remove(SpecialEffect e){
		int usecase = (int)e.GetUsecase();

		if(this.effects[usecase] == null)
			return;

		this.effects[usecase].Remove(e);
	}

	public bool Contains(SpecialEffect e){
		int usecase = (int)e.GetUsecase();

		if(this.effects[usecase] == null)
			return false;

		return this.effects[usecase].Contains(e);
	}

	public List<SpecialEffect> GetAllEffects(){
		List<SpecialEffect> outList = new List<SpecialEffect>();

		foreach(HashSet<SpecialEffect> hs in this.effects){
			if(hs == null)
				continue;

			foreach(SpecialEffect sfx in hs){
				outList.Add(sfx);
			}
		}

		return outList;
	}
}