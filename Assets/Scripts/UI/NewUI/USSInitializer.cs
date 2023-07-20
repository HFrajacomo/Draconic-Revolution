using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

public static class USSPreparer{
	private static readonly Regex numberRegex = new Regex("^[0-9]+$");

	public static void SetTextFieldLimitation(TextField field, InputFieldLimitation limitation){
		if(limitation != InputFieldLimitation.NO_LIMIT)
        	field.RegisterCallback<ChangeEvent<string>>(evt => OnInputValueChanged(evt, field, limitation));
    }

    public static void SetInputFieldColors(List<VisualElement> inputList, Color backgroundColor, Color textColor, bool alignCenter=true){
    	foreach(VisualElement element in inputList){
	        element.style.backgroundColor = backgroundColor;
	        element.style.borderLeftColor = backgroundColor;
	        element.style.borderRightColor = backgroundColor;
	        element.style.borderBottomColor = backgroundColor;
	        element.style.borderTopColor = backgroundColor;
	        element.style.color = textColor;

	        if(alignCenter)
				element.style.unityTextAlign = TextAnchor.MiddleCenter;
    	}
    } 

    private static void OnInputValueChanged(ChangeEvent<string> evt, TextField field, InputFieldLimitation limitation){
        string newValue = evt.newValue;

        if(limitation == InputFieldLimitation.CHARACTERS_ONLY){
	        if(HasNonLetterCharacters(newValue)){
	        	field.value = evt.previousValue;
	            evt.StopImmediatePropagation();
	        }
	    }
	    else if(limitation == InputFieldLimitation.NUMBERS_ONLY){
	    	if(!IsNumeric(newValue)){
	    		field.value = evt.previousValue;
	    		evt.StopImmediatePropagation();
	    	}
	    }
    }

    private static bool HasNonLetterCharacters(string value){
        // Check if the input contains any character that is not a letter (A-Z, a-z)
        foreach(char c in value){
            if(!char.IsLetter(c))
                return true;
        }
        return false;
    }

    private static bool IsNumeric(string value){
    	if(value == "")
    		return true;

        return numberRegex.IsMatch(value);
    }
}

public enum InputFieldLimitation{
	NO_LIMIT,
	NUMBERS_ONLY,
	CHARACTERS_ONLY
}