using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour{
	// Skybox
	[SerializeField]
	private GameObject skybox;

	// Menus
	[SerializeField]
	private Menu initialMenu;
	[SerializeField]
	private Menu selectWorldMenu;
	[SerializeField]
	private Menu createWorldMenu;

	// Pointer
	private Menu currentMenu;

	// Flags
	private static bool firstLoad = true;

	// Directory
	private string worldsDir;

	void Start(){
		EnvironmentVariablesCentral.Start();

		this.currentMenu = this.initialMenu;
		this.currentMenu.Enable();

		if(MenuManager.firstLoad)
			GameObject.DontDestroyOnLoad(this.skybox);
		else
			UnloadMemory();

		Configurations.LoadConfigFile();
		World.SetGameSceneFlag(false);

		MenuManager.firstLoad = false;
		this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";

		GameObject.Find("AudioManager").GetComponent<AudioManager>().RefreshVolume();
	}

	public void ChangeMenu(MenuID id){
		this.currentMenu.Disable();

		switch(id){
			case MenuID.INITIAL_MENU:
				this.currentMenu = this.initialMenu;
				break;
			case MenuID.SELECT_WORLD:
				this.currentMenu = this.selectWorldMenu;
				break;
			case MenuID.CREATE_WORLD:
				this.currentMenu = this.createWorldMenu;
				break;
			default:
				Debug.Log("Failed to fetch menu with MenuID: " + id);
				break;
		}

		this.currentMenu.Enable();
	}

	private void UnloadMemory(){
		System.GC.Collect();
		Resources.UnloadUnusedAssets();		
	}
}