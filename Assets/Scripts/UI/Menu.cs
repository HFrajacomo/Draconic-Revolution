using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public abstract class Menu : MonoBehaviour{
	[Header("Management References")]
	[SerializeField]
	protected MenuManager manager;
	[SerializeField]
	protected GameObject mainObject;
	
	public virtual void Disable(){
		DeselectClickedButton();
		this.mainObject.SetActive(false);
	}

	public virtual void Enable(){
		this.mainObject.SetActive(true);
	}

	public void RequestMenuChange(MenuID id){this.manager.ChangeMenu(id);}

	protected void DeselectClickedButton(){
		EventSystem.current.SetSelectedGameObject(null);
	}
}