using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectWorldMenu : Menu
{
    // Prefab
    public GameObject worldItem;
    public GameObject scrollContent;

    // World
    private string[] worldNames;
    private string worldsDir;

    // Buttons
    public Button carouselNextButton;
    public Button carouselPrevButton;

    // Carousel
    public ScrollRect scrollView;
    private CarouselController carousel;
    private bool isEnabled = false;
    private bool setButtons = false;


    public void OpenInitialMenu(){
        this.RequestMenuChange(MenuID.INITIAL_MENU);
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
        this.carousel = new CarouselController(this.scrollView, this.scrollContent, (int)this.worldItem.GetComponent<RectTransform>().rect.width, 0.5f);

    }

    void Update(){
        if(this.isEnabled){
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
            this.carouselNextButton.enabled = !this.carousel.isNextDisabled;
            this.carouselPrevButton.enabled = !this.carousel.isPrevDisabled;
            
            this.carousel.ResetRefresh();          
        }
    }

    /*
    // World Prefab
    public GameObject worldPrefab;

    // Buttons
    private Button carouselNextButton;
    private Button carouselPrevButton;
    private Button createWorldButton;
    private Button backButton;

    // Directories
    private string[] worldNames;
    private string worldsDir;
    private List<string> worldsList = new List<string>();

    // List of Worlds
    public List<GameObject> worldItemList = new List<GameObject>();

    // Carousel
    private CarouselController carousel;


    public override void Disable(){
        this.carousel.ClearCarousel();
    }

    public override void Enable(){
        ListWorldFolders();
    }

    void Start()
    {
        this.worldsDir = EnvironmentVariablesCentral.clientExeDir + "Worlds\\";

        this.worldItemList = GetWorldItems();
        SetPlayButtonFunctionality(this.worldItemList);

        this.carousel = new CarouselController(this.root.Query<ScrollView>("worlds-scrollview"), this.worldListElement, 520, 0.5f);

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

    private List<VisualElement> GetWorldItems(){
        return this.root.Query<VisualElement>("world-item").ToList();
    }

    private void SetPlayButtonFunctionality(List<VisualElement> worldItems){
        Button cachedButton;
        string cachedName;

        foreach(VisualElement item in worldItems){
            cachedButton = item.Query<Button>().First();
            Debug.Log(cachedButton);
            cachedName = item.Query<Label>("world-name").First().text;
            Debug.Log(cachedName);
            cachedButton.clicked += () => StartGameSingleplayer(cachedName);
        }
    }

    private void StartGameSingleplayer(string world){
        World.SetWorldName(world);
        World.SetWorldSeed(0);
        World.SetToClient();

        Debug.Log("LOGIN: " + world);
        //SceneManager.LoadScene(1);
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
    */
}