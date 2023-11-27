using System;
using UnityEngine;
using UnityEngine.UI;

public class AttributeInputField : MonoBehaviour {
	public bool GOES_BELOW_BASE = false;

	private Color baseColor = Color.white;
	private Color increasedColor = new Color(.4f, 1f, .8f);
	private Color decreasedColor = new Color(.65f, .1f, .1f);

	private InputField field;
	public int baseValue;


	void Awake(){
		this.field = GetComponent<InputField>();
		SetBaseValue(10);
		ColorField();
	}

	public void ColorField(){
		if(this.field.text == "")
			return;

		int val = Convert.ToInt32(this.field.text);

		if(val == this.baseValue)
			this.field.textComponent.color = this.baseColor;
		else if(val > this.baseValue)
			this.field.textComponent.color = this.increasedColor;
		else if(GOES_BELOW_BASE)
			this.field.textComponent.color = this.decreasedColor;
	}

	public short GetExtra(){return (short)(Convert.ToInt32(this.field.text) - this.baseValue);}

	public void SetBaseValue(int val){this.baseValue = val;}

	public bool IsAtBase(){return Convert.ToInt32(this.field.text) == this.baseValue;}

	public void SetFieldValue(short val){this.field.text = val.ToString(); SetBaseValue((int)val); ColorField();}
}