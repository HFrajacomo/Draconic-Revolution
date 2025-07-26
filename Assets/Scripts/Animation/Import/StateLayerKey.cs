using System;
using UnityEngine;

public struct StateLayerKey {
	public string controller;
	public string layer;
	public string stateName;

	public StateLayerKey(string c, string l, string name){
		this.controller = c;
		this.layer = l;
		this.stateName = name;
	}

	public bool Equals(StateLayerKey other){return this.controller == other.controller && this.layer == other.layer && this.stateName == other.stateName;}
	public override bool Equals(object obj){return obj is StateLayerKey other && Equals(other);}
	public override int GetHashCode(){return HashCode.Combine(this.controller, this.stateName, this.layer);}
}