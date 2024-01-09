using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsMenu : Menu
{
	// Tabs
	[Header("Options Tabs")]
	public GameObject gameTab;
	public GameObject videoTab;
	public GameObject audioTab;
	public GameObject adminTab;

	// Tab Buttons
	[Header("Div Buttons")]
	public Button gameButton;
	public Button videoButton;
	public Button audioButton;
	public Button adminButton;

	// Backgrounds
	[Header("Background")]
	public Image backgroundDiv;
	public Image backgroundConfigs;

	// Input Field
	[Header("Game Tab")]
	public InputField accountID_field;
	public Toggle subtitles_toggle;

	[Header("Video Tab")]
	public Slider renderDistance_slider;
	public Slider fov_slider;

	[Header("Audio Tab")]
	public Slider music2D_slider;
	public Slider music3D_slider;
	public Slider sfx2D_slider;
	public Slider sfx3D_slider;
	public Slider voice2D_slider;
	public Slider voice3D_slider;

	[Header("Admin Tab")]
	public Toggle fullbright_toggle;

	[Header("Other")]
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

	// Flags
	private static bool INIT = false;

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
		this.subtitles_toggle.GetComponent<Image>().material = tgMat;
		this.fullbright_toggle.GetComponent<Image>().material = Instantiate(tgMat);

		// Set validations
        this.accountID_field.onValidateInput += ValidateAccountID;
	}

	public override void Enable(){
		this.mainObject.SetActive(true);

		// Set default values
		this.accountID_field.text = Configurations.accountID.ToString();
		this.renderDistance_slider.value = World.renderDistance;
		this.music2D_slider.value = Configurations.music2DVolume;
		this.music3D_slider.value = Configurations.music3DVolume;
		this.sfx2D_slider.value = Configurations.sfx2DVolume;
		this.sfx3D_slider.value = Configurations.sfx3DVolume;
		this.voice2D_slider.value = Configurations.voice2DVolume;
		this.voice3D_slider.value = Configurations.voice3DVolume;
		this.fov_slider.value = Configurations.fieldOfView;

		// Refresh Shader
		this.subtitles_toggle.GetComponent<ShaderBorderFillToggle>().RefreshToggle(Configurations.subtitlesOn);
		this.fullbright_toggle.GetComponent<ShaderBorderFillToggle>().RefreshToggle(Configurations.FULLBRIGHT);

		this.fullbright_toggle.isOn = Configurations.FULLBRIGHT;
		this.subtitles_toggle.isOn = Configurations.subtitlesOn;

		INIT = true;


		ToggleDiv(this.gameTab);
	}

	public void ToggleDiv(GameObject go){
		this.gameTab.SetActive(false);
		this.videoTab.SetActive(false);
		this.audioTab.SetActive(false);
		this.adminTab.SetActive(false);

		this.gameButton.Select();
		go.SetActive(true);
	}

	public void ToggleSubtitles(){
		if(INIT){
			Configurations.subtitlesOn = !Configurations.subtitlesOn;
			this.subtitles_toggle.GetComponent<ShaderBorderFillToggle>().RefreshToggle(Configurations.subtitlesOn);
		}
	}
	public void ToggleFullbright(){
		if(INIT){
			Configurations.FULLBRIGHT = !Configurations.FULLBRIGHT;
			this.fullbright_toggle.GetComponent<ShaderBorderFillToggle>().RefreshToggle(Configurations.FULLBRIGHT);
		}
	}

	public void SaveAndLeaveMenu(){
		Configurations.SaveConfigFile();
		GameObject.Find("AudioManager").GetComponent<AudioManager>().RefreshVolume();

		this.RequestMenuChange(MenuID.INITIAL_MENU);
	}

    private char ValidateAccountID(string text, int charIndex, char addedChar){
        if(char.IsDigit(addedChar)){
        	if(text.Length > 18){
        		string newStr = text + addedChar;

        		if(ulong.TryParse(newStr, out _)){
        			return addedChar;
        		}
        		else{
        			return '\0';
        		}
        	}

        	return addedChar;
        }
        return '\0';
    }
}