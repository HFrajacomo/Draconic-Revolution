using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TESTONLY : MonoBehaviour
{
	public RuntimeAnimatorController animations;
	public Material clothing;
	public Material dragonskin;
	private CharacterBuilder builder;

	void Start(){
		this.builder = new CharacterBuilder(this.gameObject, this.animations, GenerateTest(), this.clothing, this.dragonskin);
		this.builder.Build();

		this.gameObject.transform.localPosition = new Vector3(-1f, 852.57f, 0f);
		this.gameObject.transform.localScale = new Vector3(.56f, .56f, .56f);
	}

	public CharacterAppearance GenerateTest(){
		return new CharacterAppearance(Race.HALFLING, Color.white, new ClothingInfo(0, Color.white, Color.blue, Color.green, true), new ClothingInfo(2, Color.blue, Color.green, Color.green, true)
			, new ClothingInfo(0, Color.white, Color.blue, Color.green, true), new ClothingInfo(0, Color.white, Color.blue, Color.green, true));
	}
}
