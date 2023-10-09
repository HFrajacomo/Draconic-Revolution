using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;
using Object = System.Object;

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

    [Header("Color Picking")]
    public ColorPickerPreview primaryColorPicker;
    public ColorPickerPreview secondaryColorPicker;
    public ColorPickerPreview terciaryColorPicker;
    private Color clothesColor1;
    private Color clothesColor2;
    private Color clothesColor3;
    private Color legsColor1;
    private Color legsColor2;
    private Color legsColor3;
    private Color hatsColor1;
    private Color hatsColor2;
    private Color hatsColor3;
    private Color bootsColor1;
    private Color bootsColor2;
    private Color bootsColor3;
    private Color hairColor1;
    private Color hairColor2;
    private Color hairColor3;

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
    private GameObject selectedModel;

    // Name to code Dictionary
    private Dictionary<string, int> clothesDict = new Dictionary<string, int>();
    private Dictionary<string, int> legsDict = new Dictionary<string, int>();
    private Dictionary<string, int> bootsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hatsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hairDict = new Dictionary<string, int>();

    // Default Model
    private static readonly string DEFAULT_CLOTHES = "<No Top>";
    private static readonly string DEFAULT_HAT = "<No Hat>";
    private static readonly string DEFAULT_LEGS = "<No Pants>";
    private static readonly string DEFAULT_BOOTS = "<No Boots>";

    // Cache
    private GameObject cachedObject;
    private RectTransform cachedRect;
    private Text cachedText;
    private EventTrigger cachedEventTrigger;

    public override void Disable(){
        DeselectClickedButton();
        this.mainObject.SetActive(false);
    }

    public override void Enable(){
        this.mainObject.SetActive(true);
        LoadDefaultModel();
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
                referenceDict.Add(mi.GetHandlerName(), counter);
                referenceList.Add(this.cachedObject);

                this.cachedObject.GetComponent<Button>().onClick.AddListener(() => ClickItem(referenceList[referenceDict[mi.GetHandlerName()]]));
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

    private void LoadDefaultModel(bool isMale=true){
        string suffix;
        GameObject loadedModel;

        if(isMale){
            suffix = "/M";
            this.selectedDiv = ModelType.CLOTHES;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_CLOTHES + suffix);
            SelectItem(DEFAULT_CLOTHES, loadedModel);

            this.selectedDiv = ModelType.HEADGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_HAT + suffix);
            SelectItem(DEFAULT_HAT, loadedModel);

            this.selectedDiv = ModelType.LEGS;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_LEGS + suffix);
            SelectItem(DEFAULT_LEGS, loadedModel);

            this.selectedDiv = ModelType.FOOTGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_BOOTS + suffix);
            SelectItem(DEFAULT_BOOTS, loadedModel);
        }
        else{
            suffix = "/W";
            this.selectedDiv = ModelType.CLOTHES;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_CLOTHES + suffix);
            SelectItem(DEFAULT_CLOTHES, loadedModel);

            this.selectedDiv = ModelType.HEADGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_HAT + suffix);
            SelectItem(DEFAULT_HAT, loadedModel);

            this.selectedDiv = ModelType.LEGS;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_LEGS + suffix);
            SelectItem(DEFAULT_LEGS, loadedModel);

            this.selectedDiv = ModelType.FOOTGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_BOOTS + suffix);
            SelectItem(DEFAULT_BOOTS, loadedModel);
        }

        this.selectedDiv = ModelType.CLOTHES;
        ToggleDiv(GetButton(this.selectedDiv));

    }

    private GameObject LoadModel(string name){
        GameObject go = ModelHandler.GetModelObject(this.selectedDiv, name);
        go.name = GenerateGoName();
        this.characterBuilder.Add(this.selectedDiv, go);

        return go;
    }

    private Button GetButton(ModelType type){
        switch(type){
            case ModelType.CLOTHES:
                return clothesButton;
            case ModelType.LEGS:
                return legsButton;
            case ModelType.HEADGEAR:
                return hatsButton;
            case ModelType.FOOTGEAR:
                return bootsButton;
            case ModelType.HAIR:
                return hairButton;
            default:
                return clothesButton;
        }
    }

    private GameObject FetchItemByName(string name){
        foreach(Transform t in this.scrollViewContent.GetComponentsInChildren<Transform>()){
            if(t.name == name){
                return t.gameObject;
            }
        }

        return null;
    }

    private void SelectItem(string name, GameObject go){
        if(this.selectedDiv == ModelType.CLOTHES){
            if(this.selectedClothes != null){
                this.cachedText = this.selectedClothesObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedClothes = name;
            this.cachedText.color = this.selectedColor;
            this.selectedClothesObj = go;
            this.selectedModel = go;
        }
        else if(this.selectedDiv == ModelType.LEGS){
            if(this.selectedLeg != null){
                this.cachedText = this.selectedLegObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedLeg = name;
            this.cachedText.color = this.selectedColor;
            this.selectedLegObj = go;
            this.selectedModel = go;
        }
        else if(this.selectedDiv == ModelType.FOOTGEAR){
            if(this.selectedBoot != null){
                this.cachedText = this.selectedBootObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedBoot = name;
            this.cachedText.color = this.selectedColor;
            this.selectedBootObj = go;
            this.selectedModel = go;
        }
        else if(this.selectedDiv == ModelType.HEADGEAR){
            if(this.selectedHat != null){
                this.cachedText = this.selectedHatObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedHat = name;
            this.cachedText.color = this.selectedColor;
            this.selectedHatObj = go;
            this.selectedModel = go;
        }
        else if(this.selectedDiv == ModelType.HAIR){
            if(this.selectedHair != null){
                this.cachedText = this.selectedHairObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedHair = name;
            this.cachedText.color = this.selectedColor;
            this.selectedHairObj = go;
            this.selectedModel = go;
        }
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
            this.selectedModel = go;
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
            this.selectedModel = go;
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
            this.selectedModel = go;
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
            this.selectedModel = go;
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
            this.selectedModel = go;
        }
    }

    public void ChangeColor(Object[] arguments){
        Color c = (Color)arguments[0];
        ColorPickerPreview picker = (ColorPickerPreview)arguments[1];

        if(this.selectedDiv == ModelType.CLOTHES){
            if(picker == primaryColorPicker){
                this.clothesColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.clothesColor2 = c;
            }
            else{
                this.clothesColor3 = c;
            }
        }
        else if(this.selectedDiv == ModelType.LEGS){
            if(picker == primaryColorPicker){
                this.legsColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.legsColor2 = c;
            }
            else{
                this.legsColor3 = c;
            }
        }
        else if(this.selectedDiv == ModelType.FOOTGEAR){
            if(picker == primaryColorPicker){
                this.bootsColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.bootsColor2 = c;
            }
            else{
                this.bootsColor3 = c;
            }
        }
        else if(this.selectedDiv == ModelType.HEADGEAR){
            if(picker == primaryColorPicker){
                this.hatsColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.hatsColor2 = c;
            }
            else{
                this.hatsColor3 = c;
            }
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