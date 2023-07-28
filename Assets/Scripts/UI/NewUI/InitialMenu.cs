using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitialMenu : Menu
{
	public void OnEnable(){

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