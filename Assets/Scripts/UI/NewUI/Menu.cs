using UnityEngine;
using UnityEngine.UIElements;

public abstract class Menu : MonoBehaviour{
	[SerializeField]
	protected MenuManager manager;
	[SerializeField]
	protected GameObject mainObject;
	
	public virtual void Disable(){this.mainObject.SetActive(false);}
	public virtual void Enable(){this.mainObject.SetActive(true);}
	public void RequestMenuChange(MenuID id){this.manager.ChangeMenu(id);}
}