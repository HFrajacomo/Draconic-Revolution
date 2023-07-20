using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SelectWorldMenu : Menu
{
    // UI Files
    public UIDocument uxmlWorldItem;
    public StyleSheet style;

    // Visual Elements
    private VisualElement root;
    private VisualTreeAsset worldItemAsset;
    private VisualElement worldListElement;

    // Buttons
    private Button carouselNextButton;
    private Button carouselPrevButton;
    private Button createWorldButton;
    private Button backButton;

    // Directories
    private string[] worldNames;
    private string worldsDir;
    private List<string> worldsList = new List<string>();

    // Cache
    private VisualElement cacheElement;
    private VisualElement cacheRoot;

    // Carousel
    private CarouselController carousel;

    void Start()
    {
        this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";

        // Load the UXML file and add it to the root VisualElement
        this.root = this.mainDocument.rootVisualElement;
        this.root.styleSheets.Add(this.style);

        this.createWorldButton = this.root.Query<Button>("world-play-button");
        this.carouselNextButton = this.root.Query<Button>("next-carousel-button");
        this.carouselPrevButton = this.root.Query<Button>("prev-carousel-button");
        this.backButton = this.root.Query<Button>("back-button");

        this.worldItemAsset = this.uxmlWorldItem.visualTreeAsset;
        this.worldListElement = this.root.Query<VisualElement>("unity-content-container");

        this.carousel = new CarouselController(this.root.Query<ScrollView>("worlds-scrollview"), this.worldListElement, 520, 0.5f);

        ListWorldFolders();
        InitClickEvents();
    }

    void Update(){
        this.carousel.Scroll();
        RefreshCarouselButtons();
    }


    public void InitClickEvents(){
        this.carouselNextButton.clicked += () => this.carousel.MoveOneAhead();
        this.carouselPrevButton.clicked += () => this.carousel.MoveOneBack();
        this.createWorldButton.clicked += () => SendMessage("ChangeMenu", MenuID.CREATE_WORLD);
    }

    // REDO
    /*
    public void CreateNewWorld(){
        int rn;

        if(single_nameField.text == ""){
            return;
        }

        if(single_seedField.text == ""){
            Random.InitState((int)DateTime.Now.Ticks);
            rn = (int)Random.Range(0, int.MaxValue);
            World.SetWorldSeed(rn.ToString());
        }
        else{
            World.SetWorldSeed(single_seedField.text);
        }

        World.SetWorldName(single_nameField.text);
        
        if(RegionFileHandler.CreateWorldFile(World.worldName, World.worldSeed))
            OpenSingleplayerMenu();
    }
    */
   
    private bool ListWorldFolders(){
        string worldName;

        this.worldsList.Clear();

        if(!Directory.Exists(this.worldsDir))
            return false;

        this.worldNames = Directory.GetDirectories(this.worldsDir);

        foreach(string world in this.worldNames){
            worldName = GetDirectoryName(world);

            this.carousel.AddWorld(this.worldItemAsset, worldName, "Description...");
        }

        if(this.worldNames.Length > 0)
            return true;
        return false;
    }

    private string GetDirectoryName(string path){
        string[] pathList = path.Split("\\");
        return pathList[pathList.Length-1];
    }

    private void RefreshCarouselButtons(){
        if(this.carousel.refreshControllers){
            this.carouselNextButton.SetEnabled(!this.carousel.isNextDisabled);
            this.carouselPrevButton.SetEnabled(!this.carousel.isPrevDisabled);
            
            this.carousel.ResetRefresh();          
        }
    }
}