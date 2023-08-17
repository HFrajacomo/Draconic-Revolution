using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitialMenu : Menu
{
	public Selectable initialSelectable;

	public void OnEnable(){
		if(this.initialSelectable != null)
			initialSelectable.Select();
	}

	public void OpenWorldSelectMenu(){
		this.RequestMenuChange(MenuID.SELECT_WORLD);
	}

	public void OpenMultiplayerMenu(){
		this.RequestMenuChange(MenuID.MULTIPLAYER);
	}

	public void OpenOptionsMenu(){
		this.RequestMenuChange(MenuID.OPTIONS);
	}

	public void ExitGame(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
	}
}