using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsMenu : Menu
{
	// Tabs
	public GameObject gameTab;
	public GameObject videoTab;
	public GameObject audioTab;
	public GameObject adminTab;

	// Tab Buttons
	public Button gameButton;
	public Button videoButton;
	public Button audioButton;
	public Button adminButton;

	// Backgrounds
	public Image backgroundDiv;
	public Image backgroundConfigs;

	// Input Field
	public InputField accountID_field;

	// Toggles
	public Toggle subtitlesToggle;

	// Materials
	public Material pulseToggle;

	// Shader Background Settings
	private static readonly Color MENU_COLOR_BG = new Color(0.4f, 0.607f, 0.623f);
	private static readonly Color ANIMATION_COLOR_BG = new Color(0.3411f, 0.619f, 0.866f);
	private static readonly float BORDER_SIZE_BG = 0.005f;
	private static readonly float HORIZONTAL_ADJUSTMENT_BG = 0.996f;

	// Shader Input Field Settings
	private static readonly float BORDER_SIZE_IF = 0.07f;
	private static readonly float HORIZONTAL_ADJUSTMENT_IF = 0.95f;

	// Shader Toggle Settings
	private static readonly float HORIZONTAL_ADJUSTMENT_TG = 1f;

	void Awake(){
		// Create materials
		Material bgMat = Instantiate(this.backgroundDiv.material);
		Material ifMat = Instantiate(this.backgroundDiv.material);
		Material tgMat = Instantiate(this.pulseToggle);

		// Set Initial material info
		bgMat.SetColor("_BorderColor", MENU_COLOR_BG);
		bgMat.SetColor("_AnimationColor", ANIMATION_COLOR_BG);
		bgMat.SetFloat("_BorderSize", BORDER_SIZE_BG);
		bgMat.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT_BG);
		
		ifMat.SetFloat("_BorderSize", BORDER_SIZE_IF);
		ifMat.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT_IF);

		tgMat.SetFloat("_HorizontalAdjustment", HORIZONTAL_ADJUSTMENT_TG);

		// Add Materials
		this.backgroundDiv.material = bgMat;
		this.backgroundConfigs.material = bgMat;
		this.accountID_field.GetComponent<Image>().material = ifMat;
		this.subtitlesToggle.GetComponent<Image>().material = tgMat;

		// Set default values
		this.accountID_field.text = Configurations.accountID.ToString();
		this.subtitlesToggle.isOn = Configurations.subtitlesOn;
	}

	void Start(){
		this.gameButton.Select();
		ToggleDiv(this.gameTab);
	}

	public void ToggleDiv(GameObject go){
		this.gameTab.SetActive(false);
		//this.videoTab.SetActive(false);
		//this.audioTab.SetActive(false);
		//this.adminTab.SetActive(false);

		go.SetActive(true);
	}

	public void ToggleSubtitles(){Configurations.subtitlesOn = !Configurations.subtitlesOn;}
}