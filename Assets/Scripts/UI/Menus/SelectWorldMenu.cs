using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SelectWorldMenu : Menu
{
    // Prefab
    [Header("Prefabs")]
    public GameObject worldItem;
    public GameObject scrollContent;

    // World
    private string[] worldNames;
    private string worldsDir;

    // Buttons
    [Header("UI Elements")]
    public Button carouselNextButton;
    public Button carouselPrevButton;
    
    // Dropdown selection
    private string dropDownSelection;

    // Carousel
    [Header("Carousel Controllers")]
    public RectTransform scrollView;
    public RectTransform viewport;
    private CarouselController carousel;
    private bool isEnabled = false;
    private bool setButtons = false;

    // Cache
    private Button cacheButton;
    private Text cacheText;
    private Dropdown cacheDD;


    public void OpenInitialMenu(){
        this.RequestMenuChange(MenuID.INITIAL_MENU);
    }

    public void OpenCreateWorldMenu(){
        this.RequestMenuChange(MenuID.CREATE_WORLD);
    }

    public void OpenDropDownSelection(GameObject worldItem){
        this.cacheDD = worldItem.GetComponentInChildren<Dropdown>();
        this.cacheText = worldItem.GetComponentInChildren<Text>();

        this.dropDownSelection = this.cacheDD .options[this.cacheDD .value].text;

        switch(this.dropDownSelection){
            case null:
                return;
            case "Rename":
                OpenRenameWorld(this.cacheText);
                break;
            case "Defragment":
                OpenDefragmentWorld(this.cacheText);
                break;
            default:
                return;
        }
    }

    public void OpenRenameWorld(Text worldNameText){
        RenameWorldMenu.SetWorldName(worldNameText.text);
        this.RequestMenuChange(MenuID.RENAME_WORLD);
    }

    public void OpenDefragmentWorld(Text worldNameText){
        DefragmentWorldMenu.SetWorldName(worldNameText.text);
        this.RequestMenuChange(MenuID.DEFRAG_WORLD);
    }

    public override void Disable(){
        DeselectClickedButton();
        if(this.carousel != null)
            this.carousel.ClearCarousel();

        this.isEnabled = false;
        this.mainObject.SetActive(false);
    }

    public override void Enable(){
        this.mainObject.SetActive(true);

        ListWorldFolders();
        this.isEnabled = true;

        if(!this.setButtons){
            InitClickEvents();
            this.setButtons = true;
        }
    }

    void Start(){
        this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";
        this.carousel = new CarouselController(this.scrollView, this.scrollContent, (int)this.worldItem.GetComponent<RectTransform>().rect.width, (int)this.viewport.rect.width, 0.5f);
    }

    void Update(){
        if(this.isEnabled){
            this.carousel.HandleMouseMovement();

            if(!this.carousel.initialPositionSet)
                this.carousel.InitialMovementOfView();

            this.carousel.Scroll();
            RefreshCarouselButtons();
        }
    }

    public void InitClickEvents(){
        this.carouselNextButton.onClick.AddListener(this.carousel.MoveOneAhead);
        this.carouselPrevButton.onClick.AddListener(this.carousel.MoveOneBack);
    }

    private bool ListWorldFolders(){
        string worldName;

        if(!Directory.Exists(this.worldsDir))
            return false;

        this.worldNames = Directory.GetDirectories(this.worldsDir);

        foreach(string world in this.worldNames){
            worldName = GetDirectoryName(world);

            this.carousel.AddWorld(this.worldItem, worldName, "Description...");
        }

        SetPlayButtonFunctionality();

        if(this.worldNames.Length > 0)
            return true;
        return false;
    }

    private void SetPlayButtonFunctionality(){
        foreach(GameObject world in this.carousel.elements){
            string worldName = world.GetComponentInChildren<Text>().text;
            this.cacheButton = world.GetComponentInChildren<Button>();

            this.cacheButton.onClick.AddListener(() => StartGameSingleplayer(worldName));
        }
    }
    
    private void StartGameSingleplayer(string world){
        World.SetWorldName(world);
        World.SetWorldSeed(0);
        World.SetToClient();

        SceneManager.LoadScene(1);
    }

    private string GetDirectoryName(string path){
        string[] pathList = path.Split("\\");
        return pathList[pathList.Length-1];
    }

    private void RefreshCarouselButtons(){
        if(this.carousel.refreshControllers){
            this.carouselNextButton.interactable = !this.carousel.isNextDisabled;
            this.carouselPrevButton.interactable = !this.carousel.isPrevDisabled;
            
            this.carousel.ResetRefresh();          
        }
    }
}