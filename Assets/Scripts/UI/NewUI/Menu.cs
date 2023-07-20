using UnityEngine;
using UnityEngine.UIElements;

public abstract class Menu : MonoBehaviour{
	[SerializeField]
	protected UIDocument mainDocument;

	public virtual void Disable(){this.mainDocument.panelSettings = null;}
	public virtual void Enable(PanelSettings ps){this.mainDocument.panelSettings = ps;}
}