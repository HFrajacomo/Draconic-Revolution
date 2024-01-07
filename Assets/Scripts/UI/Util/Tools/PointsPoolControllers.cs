using System;
using UnityEngine;
using UnityEngine.UI;

public class PointsPoolControllers : MonoBehaviour {
	public InputField attachedField;
	public InputField pointsPool;

	public void Increase(){
		int poolVal = ToNumber(pointsPool.text.Split("/")[0]);
		int poolMax = ToNumber(pointsPool.text.Split("/")[1]);

		if(poolVal > 0){
			if(attachedField.text == "")
				return;

			int newVal = ToNumber(attachedField.text) + 1;
			int newPoolVal = poolVal - 1;

			this.attachedField.text = newVal.ToString();
			this.pointsPool.text = newPoolVal.ToString() + "/" + poolMax.ToString();
		}
	}

	public void Decrease(AttributeInputField aif){
		if(aif.IsAtBase())
			return;

		int poolCurrent = ToNumber(pointsPool.text.Split("/")[0]);
		int poolMax = ToNumber(pointsPool.text.Split("/")[1]);

		if(poolCurrent == poolMax)
			return;

		int newVal = ToNumber(attachedField.text) - 1;
		int newPoolVal = poolCurrent + 1;

		this.attachedField.text = newVal.ToString();
		this.pointsPool.text = newPoolVal.ToString() + "/" + poolMax.ToString();;
	}


	private int ToNumber(string text){
		return Convert.ToInt32(text);
	}
}