using UnityEngine;
using UnityEngine.UI;

public class HighlightToggleText : MonoBehaviour {
	public Color normalColor = new Color(.9f,.9f,.9f);
	public Color highlightColor = new Color(.47f, .898f, .807f);
	private Text txt;

	void Awake(){this.txt = GetComponentInChildren<Text>();}

	public void ActivateHighlight(){this.txt.color = this.highlightColor;}
	public void DeactivateHighlight(){this.txt.color = this.normalColor;}
}