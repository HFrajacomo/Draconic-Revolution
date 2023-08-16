using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefragmentWorldMenu : Menu
{
	[Header("UI Elements")]
	public Button defragmentButton;
	public Button cancelButton;
	public Button backButton;
	public Slider progressBar;

	[Header("Text Elements")]
	public Text currentRegionText;
	public Text progressText;
	public Text totalWorldSize;
	public Text defragedWorldSize;

	[Header("Material")]
	public Material progressBarMaterial;
	public Color primaryColor = new Color(0.156f, 0.679f, 0.392f);
	public Color secondaryColor = new Color(0.225f, 0.318f, 0.330f);

	public int frequency = 72;

	// Defragmenter
	private RegionDefragmenter defrag;

	// Defragment operations
	private static string WORLD_NAME;
	private bool isDefraging = false;
	private List<string> regionFilepath = new List<string>();
	private int amountOfRegions;

	// Directories
	private readonly int sizeLevelIncrease = 1024;

	// World Sizes
	private double totalSize = 0f;
	private double newWorldSize = 0f;
	private string totalSizeLevel = "B";
	private string newWorldSizeLevel = "B";

	// Cache
	private string cachedString;

	void Start(){
		Material mat = Instantiate(this.progressBarMaterial);

		mat.SetColor("_Primary_Color", this.primaryColor);
		mat.SetColor("_Secondary_Color", this.secondaryColor);
		mat.SetFloat("_Sine_Frequency", this.frequency);
		this.progressBar.GetComponentsInChildren<Image>()[1].material = mat;
	}

	void Update(){
		if(this.isDefraging){
			if(this.regionFilepath.Count == 0){
				FinishedDefragment();
				return;
			}

			this.cachedString = this.regionFilepath[0];
			this.regionFilepath.RemoveAt(0);

			currentRegionText.text = "Defragmenting: " + this.cachedString;
			progressText.text = ((this.amountOfRegions - (this.regionFilepath.Count+1))/this.amountOfRegions).ToString("0.00") + "%";
			progressBar.value = ((this.amountOfRegions - (this.regionFilepath.Count+1))/this.amountOfRegions);

			this.defrag = new RegionDefragmenter(this.cachedString, WORLD_NAME, EnvironmentVariablesCentral.saveDir + WORLD_NAME + "/");
			this.defrag.Defragment();

			ConvertTotalSize(this.defrag.GetPreviousSize());
			ConvertNewSize(this.defrag.GetDefragSize());

			this.defragedWorldSize.text = "New World Size: " + this.newWorldSize.ToString("0.00") + " " + this.newWorldSizeLevel;
			this.totalWorldSize.text = "World Size: " + this.totalSize.ToString("0.00") + " " + this.totalSizeLevel;
		}
	}

	public override void Enable(){
		this.mainObject.SetActive(true);
		this.defragmentButton.interactable = true;
		this.cancelButton.interactable = false;
		this.backButton.interactable = true;
		this.progressBar.value = 0f;
		this.progressText.text = "";
		this.currentRegionText.text = "";
		this.defragedWorldSize.text = "";
		this.totalWorldSize.text = "";
		this.totalSize = 0f;
		this.newWorldSize = 0f;
		this.totalSizeLevel = "B";
		this.newWorldSizeLevel = "B";
		this.isDefraging = false;
	}

	private void FinishedDefragment(){
		this.currentRegionText.text = "";
		this.progressText.text = "100%";
		this.progressBar.value = 1f;
		this.isDefraging = false;
		this.backButton.interactable = true;
		this.cancelButton.interactable = false;	
	}

	public static void SetWorldName(string name){WORLD_NAME = name;}

	public void StartDefragment(){
		this.regionFilepath = EnvironmentVariablesCentral.ListFilesInWorldFolder(WORLD_NAME, extensionFilter:"rdf", firstLetterFilter:'r', onlyName:true);
		
		this.amountOfRegions = this.regionFilepath.Count;
		this.isDefraging = true;

		this.defragmentButton.interactable = false;
		this.cancelButton.interactable = true;
		this.backButton.interactable = false;
		this.progressBar.value = 0f;
	}

	public void CancelDefragment(){
		if(this.isDefraging){
			this.isDefraging = false;
			this.defragmentButton.interactable = true;
			this.cancelButton.interactable = false;
			this.backButton.interactable = true;
			this.progressBar.value = 0f;
			this.progressText.text = "";
			this.currentRegionText.text = "";
		}
	}

	private void ConvertTotalSize(long newBytes){
		if(totalSizeLevel == "B")
			totalSize += newBytes;
		else if(totalSizeLevel == "KB")
			totalSize += (newBytes / this.sizeLevelIncrease);
		else if(totalSizeLevel == "MB")
			totalSize += (newBytes / (this.sizeLevelIncrease*this.sizeLevelIncrease));
		else if(totalSizeLevel == "GB")
			totalSize += (newBytes / (this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease));
		else if(totalSizeLevel == "TB")
			totalSize += (newBytes / (this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease));

		if(totalSize >= this.sizeLevelIncrease && totalSizeLevel != "TB"){
			if(totalSizeLevel == "B"){
				totalSize /= this.sizeLevelIncrease;
				totalSizeLevel = "KB";
			}
			else if(totalSizeLevel == "KB"){
				totalSize /= this.sizeLevelIncrease;
				totalSizeLevel = "MB";
			}
			else if(totalSizeLevel == "MB"){
				totalSize /= this.sizeLevelIncrease;
				totalSizeLevel = "GB";
			}
			else if(totalSizeLevel == "GB"){
				totalSize /= this.sizeLevelIncrease;
				totalSizeLevel = "TB";
			}
		}
	}

	private void ConvertNewSize(long newBytes){
		if(newWorldSizeLevel == "B")
			newWorldSize += newBytes;
		else if(newWorldSizeLevel == "KB")
			newWorldSize += (newBytes / this.sizeLevelIncrease);
		else if(newWorldSizeLevel == "MB")
			newWorldSize += (newBytes / this.sizeLevelIncrease*this.sizeLevelIncrease);
		else if(newWorldSizeLevel == "GB")
			newWorldSize += (newBytes / (this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease));
		else if(newWorldSizeLevel == "TB")
			newWorldSize += (newBytes / (this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease*this.sizeLevelIncrease));

		if(newWorldSize >= this.sizeLevelIncrease && newWorldSizeLevel != "TB"){
			if(newWorldSizeLevel == "B"){
				newWorldSize /= this.sizeLevelIncrease;
				newWorldSizeLevel = "KB";
			}
			else if(newWorldSizeLevel == "KB"){
				newWorldSize /= this.sizeLevelIncrease;
				newWorldSizeLevel = "MB";
			}
			else if(newWorldSizeLevel == "MB"){
				newWorldSize /= this.sizeLevelIncrease;
				newWorldSizeLevel = "GB";
			}
			else if(newWorldSizeLevel == "GB"){
				newWorldSize /= this.sizeLevelIncrease;
				newWorldSizeLevel = "TB";
			}
		}
	}

	public void OpenSelectWorldMenu(){
		if(!this.isDefraging){
			this.RequestMenuChange(MenuID.SELECT_WORLD);
		}
	}
}