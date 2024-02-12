using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour{
	// Menus
	[Header("Menu Objects")]
	[SerializeField]
	private Menu initialMenu;
	[SerializeField]
	private Menu selectWorldMenu;
	[SerializeField]
	private Menu createWorldMenu;
	[SerializeField]
	private Menu multiplayerMenu;
	[SerializeField]
	private Menu optionsMenu;
	[SerializeField]
	private Menu renameWorldMenu;
	[SerializeField]
	private Menu defragmentWorldMenu;
	[SerializeField]
	private Menu resetWorldMenu;
	[SerializeField]
	private Menu deleteWorldMenu;
	[SerializeField]
	private Menu characterCreationMenu;
	[SerializeField]
	private Menu characterCreationDataMenu;
	[SerializeField]
	private Menu characterCreationReligionMenu;

	// Pointer
	private Menu currentMenu;

	// Flags
	private static bool firstLoad = true;
	private static bool initCharacterCreation = false;

	// Directory
	private string worldsDir;

	void Start(){
		EnvironmentVariablesCentral.Start();

		DisableMenus();

		this.currentMenu = this.initialMenu;
		this.currentMenu.Enable();

		if(!MenuManager.firstLoad)
			UnloadMemory();

		Configurations.LoadConfigFile();
		World.SetGameSceneFlag(false);

		MenuManager.firstLoad = false;
		this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";

		GameObject.Find("AudioManager").GetComponent<AudioManager>().RefreshVolume();

		// Menu inits
		if(initCharacterCreation){
			SetInitCharacterCreationFlag(false);
			((CharacterCreationMenu)this.characterCreationMenu).RESET_ON_ENABLE = true;
			ChangeMenu(MenuID.CHARACTER_CREATION);
		}
	}

	private void DisableMenus(){
		this.initialMenu.Disable();
		this.selectWorldMenu.Disable();
	}

	public static void SetInitCharacterCreationFlag(bool b){initCharacterCreation = b;}

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
			case MenuID.MULTIPLAYER:
				this.currentMenu = this.multiplayerMenu;
				break;
			case MenuID.OPTIONS:
				this.currentMenu = this.optionsMenu;
				break;
			case MenuID.RENAME_WORLD:
				this.currentMenu = this.renameWorldMenu;
				break;
			case MenuID.DEFRAG_WORLD:
				this.currentMenu = this.defragmentWorldMenu;
				break;
			case MenuID.RESET_WORLD:
				this.currentMenu = this.resetWorldMenu;
				break;
			case MenuID.DELETE_WORLD:
				this.currentMenu = this.deleteWorldMenu;
				break;
			case MenuID.CHARACTER_CREATION:
				this.currentMenu = this.characterCreationMenu;
				break;
			case MenuID.CHARACTER_CREATION_DATA:
				this.currentMenu = this.characterCreationDataMenu;
				break;
			case MenuID.CHARACTER_CREATION_RELIGION:
				this.currentMenu = this.characterCreationReligionMenu;
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