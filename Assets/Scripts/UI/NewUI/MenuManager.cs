using UnityEngine;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour{
	// Panel Settings
	[SerializeField]
	private PanelSettings panelSettings;

	// Menus
	[SerializeField]
	private Menu selectWorldMenu;
	[SerializeField]
	private Menu createWorldMenu;

	// Pointer
	private Menu currentMenu;

	void Start(){
		this.currentMenu = this.selectWorldMenu;
		this.currentMenu.Enable(this.panelSettings);
	}

	private void ChangeMenu(MenuID id){
		this.currentMenu.Disable();

		switch(id){
			case MenuID.SELECT_WORLD:
				this.currentMenu = selectWorldMenu;
				break;
			case MenuID.CREATE_WORLD:
				this.currentMenu = createWorldMenu;
				break;
			default:
				Debug.Log("Failed to fetch menu with MenuID: " + id);
				break;
		}

		this.currentMenu.Enable(this.panelSettings);
	}
}