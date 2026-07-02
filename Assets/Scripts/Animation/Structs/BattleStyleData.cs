using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public struct BattleStyleData{
	public AttachmentData[] attachments;
	public int combo_hits;
	public Wrapper<StateClipPair> overrides;
	private StateClipPair[] clipPairs;
	private Dictionary<string, StateClipPair> map;
	private string name;
	private int code;

	public void PostDeserializationSetup(string styleName, int styleCode){
		this.name = styleName;
		this.code = styleCode;

		this.map = new Dictionary<string, StateClipPair>();
		this.clipPairs = overrides.data;

		for(int i=0; i < this.clipPairs.Length; i++){
			this.map.Add(this.clipPairs[i].state, this.clipPairs[i]);
		}

		if(this.attachments != null){
			for(int i=0; i < this.attachments.Length; i++){
				this.attachments[i].PostDeserializationSetup();
			}
		}
	}

	public string GetName(){return this.name;}
	public int GetCode(){return this.code;}
	public int GetComboHits(){return this.combo_hits;}
	public StateClipPair[] GetOverrides(){return this.clipPairs;}
	public StateClipPair GetStateStyleData(string state){return this.map[state];}
	public override string ToString(){return this.name;}
}