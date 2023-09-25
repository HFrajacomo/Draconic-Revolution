using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;

public class CharacterCreationMenu : Menu{
    [Header("Text Color")]
    public Color notSelectedColor = new Color(.87f, .87f, .87f);
    public Color selectedColor = new Color(.32f, .72f, .91f);

    [Header("UI Elements")]
    public GameObject menuDiv;
    public GameObject playerObject;

    [Header("Buttons")]
    public Button clothesButton;
    public Button legsButton;
    public Button bootsButton;
    public Button hatsButton;
    public Button hairButton;

    [Header("ScrollView")]
    public ScrollRect itemsView;
    public GameObject scrollViewContent;

    [Header("Material")]
    public Material borderPulseMaterial;
    public float BORDER_SIZE = .01f;
    public float HORIZONTAL_ADJUSTMENT = 1f;

    [Header("Prefab")]
    public GameObject itemButtonPrefab;

    private CharacterBuilder characterBuilder;

    // Items List
    private List<GameObject> clothesItems = new List<GameObject>();
    private List<GameObject> legsItems = new List<GameObject>();
    private List<GameObject> bootsItems = new List<GameObject>();
    private List<GameObject> hatsItems = new List<GameObject>();
    private List<GameObject> hairItems = new List<GameObject>();

    // Selected Items
    private ModelType selectedDiv;
    private string selectedClothes;
    private string selectedLeg;
    private string selectedBoot;
    private string selectedHat;
    private string selectedHair;
    private GameObject selectedClothesObj;
    private GameObject selectedLegObj;
    private GameObject selectedBootObj;
    private GameObject selectedHatObj;
    private GameObject selectedHairObj;

    // Name to code Dictionary
    private Dictionary<string, int> clothesDict = new Dictionary<string, int>();
    private Dictionary<string, int> legsDict = new Dictionary<string, int>();
    private Dictionary<string, int> bootsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hatsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hairDict = new Dictionary<string, int>();

    // Cache
    private GameObject cachedObject;
    private RectTransform cachedRect;
    private Text cachedText;
    private EventTrigger cachedEventTrigger;

    public override void Disable(){
        DeselectClickedButton();
        this.mainObject.SetActive(false);
    }

    void Start(){
        Material mat = Instantiate(this.borderPulseMaterial);

        mat.SetFloat("_BorderSize", this.BORDER_SIZE);
        mat.SetFloat("_HorizontalAdjustment", this.HORIZONTAL_ADJUSTMENT);

        menuDiv.GetComponentInChildren<Image>().material = mat;

        this.characterBuilder = new CharacterBuilder(this.playerObject);

        ToggleDiv(this.clothesButton);
    }

    public void ToggleDiv(Button bt){
        if(bt.GetComponentInChildren<Text>().color == this.selectedColor)
            return;

        ModelType selectedType = IdentifyType(bt);
        this.selectedDiv = selectedType;

        this.clothesButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.legsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.bootsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.hatsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.hairButton.GetComponentInChildren<Text>().color = this.notSelectedColor;

        bt.GetComponentInChildren<Text>().color = this.selectedColor;

        // Deletes previous items
        foreach(Transform children in scrollViewContent.transform){
            children.gameObject.SetActive(false);
        }

        // Adds new items
        if(IdentifyListOfItems(bt).Count == 0){
            List<GameObject> referenceList = IdentifyListOfItems(bt);
            Dictionary<string, int> referenceDict = IdentifyDict(bt);
            int counter = 0;

            foreach(ModelInfo mi in ModelHandler.GetModelInfoList(selectedType)){
                this.cachedObject = GameObject.Instantiate(this.itemButtonPrefab);
                this.cachedObject.transform.SetParent(this.scrollViewContent.transform);
                this.cachedObject.transform.localScale = Vector3.one;
                this.cachedObject.transform.position = Vector3.zero;
                this.cachedObject.name = mi.name;

                this.cachedRect = this.cachedObject.transform as RectTransform;
                this.cachedRect.anchoredPosition3D = Vector3.zero;

                this.cachedObject.GetComponentInChildren<Text>().text = mi.name;
                referenceDict.Add(mi.name, counter);
                referenceList.Add(this.cachedObject);

                this.cachedObject.GetComponent<Button>().onClick.AddListener(() => ClickItem(referenceList[referenceDict[mi.name]]));
                counter++;
            }
        }
        else{
            foreach(GameObject go in IdentifyListOfItems(bt)){
                go.SetActive(true);
            }
        }
    }

    public void ClickItem(GameObject go){
        SelectItem(go);
        LoadModel(go.GetComponentInChildren<Text>().text + "/M");
    }

    private void LoadModel(string name){
        GameObject go = ModelHandler.GetModelObject(this.selectedDiv, name);
        go.name = GenerateGoName();
        this.characterBuilder.Add(this.selectedDiv, go);
    }

    private void SelectItem(GameObject go){
        if(this.selectedDiv == ModelType.CLOTHES){
            if(this.selectedClothes != null){
                this.cachedText = this.selectedClothesObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedClothes = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedClothesObj = go;
        }
        else if(this.selectedDiv == ModelType.LEGS){
            if(this.selectedLeg != null){
                this.cachedText = this.selectedLegObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedLeg = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedLegObj = go;
        }
        else if(this.selectedDiv == ModelType.FOOTGEAR){
            if(this.selectedBoot != null){
                this.cachedText = this.selectedBootObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedBoot = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedBootObj = go;
        }
        else if(this.selectedDiv == ModelType.HEADGEAR){
            if(this.selectedHat != null){
                this.cachedText = this.selectedHatObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedHat = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedHatObj = go;
        }
        else if(this.selectedDiv == ModelType.HAIR){
            if(this.selectedHair != null){
                this.cachedText = this.selectedHairObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedHair = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedHairObj = go;
        }
    }

    private ModelType IdentifyType(Button button){
        if(button == this.clothesButton)
            return ModelType.CLOTHES;
        if(button == this.legsButton)
            return ModelType.LEGS;
        if(button == this.bootsButton)
            return ModelType.FOOTGEAR;
        if(button == this.hatsButton)
            return ModelType.HEADGEAR;
        if(button == this.hairButton)
            return ModelType.HAIR;
        return ModelType.CLOTHES;
    }

    private List<GameObject> IdentifyListOfItems(Button button){
        if(button == this.clothesButton)
            return this.clothesItems;
        if(button == this.legsButton)
            return this.legsItems;
        if(button == this.bootsButton)
            return this.bootsItems;
        if(button == this.hatsButton)
            return this.hatsItems;
        if(button == this.hairButton)
            return this.hairItems;
        return this.clothesItems;        
    }

    private Dictionary<string, int> IdentifyDict(Button button){
        if(button == this.clothesButton)
            return this.clothesDict;
        if(button == this.legsButton)
            return this.legsDict;
        if(button == this.bootsButton)
            return this.bootsDict;
        if(button == this.hatsButton)
            return this.hatsDict;
        if(button == this.hairButton)
            return this.hairDict;
        return this.clothesDict;          
    }

    private string GenerateGoName(){
        return Enum.GetName(typeof(ModelType), (byte)this.selectedDiv);
    }
}