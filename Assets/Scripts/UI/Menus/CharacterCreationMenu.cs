using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;
using Object = System.Object;

public class CharacterCreationMenu : Menu{
    [Header("Menu")]
    public CharacterCreationStatusMenu statusMenu;
    public CharacterCreationReligionMenu religionMenu;

    [Header("Text Color")]
    public Color notSelectedColor = new Color(.87f, .87f, .87f);
    public Color selectedColor = new Color(.32f, .72f, .91f);

    [Header("UI Elements")]
    public GameObject appearanceDiv;
    public GameObject playerObject;
    public GameObject generalTab;

    [Header("Buttons")]
    public Button generalButton;
    public Button clothesButton;
    public Button legsButton;
    public Button bootsButton;
    public Button hatsButton;
    public Button hairButton;
    public Button faceButton;

    [Header("Input Field")]
    public InputField nameInput;

    [Header("ScrollView")]
    public ScrollRect itemsView;
    public Scrollbar itemsScrollBar;
    public GameObject scrollViewContent;
    public Image viewScrollbar;
    public GameObject slidingArea;

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
    public ColorPickerLerp skinColorPicker;
    public GameObject colorPickerDiv;
    public GameObject skinColorMenu;
    private Color skinColor;
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
    private Color faceColor1;
    private Color faceColor2;
    private Color faceColor3;

    [Header("Materials")]
    public Material prefabPlainMat;
    public Material dragonSkinMat;
    public Material eyeIrisMat;
    public Material dragonlingHornMat;
    private Material skinMat;
    private Material clothesMat1;
    private Material clothesMat2;
    private Material clothesMat3;
    private Material legsMat1;
    private Material legsMat2;
    private Material legsMat3;
    private Material bootsMat1;
    private Material bootsMat2;
    private Material bootsMat3;
    private Material hatsMat1;
    private Material hatsMat2;
    private Material hatsMat3;
    private Material hairMat1;
    private Material hairMat2;
    private Material hairMat3;
    private Material faceMat1;
    private Material faceMat2;
    private Material faceMat3;

    [Header("Default Options")]
    public Button defaultGender;
    public Button defaultRace;
    public Button defaultPreset;

    [Header("General Buttons")]
    public Button humanRace;
    public Button elfRace;
    public Button dwarfRace;
    public Button orcRace;
    public Button halflingRace;
    public Button dragonlingRace;
    public Button undeadRace;

    public Button maleGender;
    public Button femaleGender;

    public Button preset1;
    public Button preset2;
    public Button preset3;

    [Header("Animation")]
    public RuntimeAnimatorController maleAnimations;
    public RuntimeAnimatorController femaleAnimations;

    private CharacterBuilderMenu characterBuilder;
    private bool INIT = false;
    private bool ENABLED = false;

    // Items List
    private List<GameObject> clothesItems = new List<GameObject>();
    private List<GameObject> legsItems = new List<GameObject>();
    private List<GameObject> bootsItems = new List<GameObject>();
    private List<GameObject> hatsItems = new List<GameObject>();
    private List<GameObject> hairItems = new List<GameObject>();
    private List<GameObject> faceItems = new List<GameObject>();

    // Selected Items
    private ModelType selectedDiv;
    private string selectedClothes;
    private string selectedLeg;
    private string selectedBoot;
    private string selectedHat;
    private string selectedHair;
    private string selectedFace;
    private GameObject selectedClothesObj;
    private GameObject selectedLegObj;
    private GameObject selectedBootObj;
    private GameObject selectedHatObj;
    private GameObject selectedHairObj;
    private GameObject selectedFaceObj;
    private GameObject selectedModel;

    // Selected in General
    private Race race;
    private bool selectedGenderIsMale = true;
    private bool genderUsedToLoadingItems;
    private Button selectedRaceItem;
    private Button selectedGenderItem;
    private Button selectedPresetItem;
    private Gradient skinColorGradient;

    // Name to code Dictionary
    private Dictionary<string, int> clothesDict = new Dictionary<string, int>();
    private Dictionary<string, int> legsDict = new Dictionary<string, int>();
    private Dictionary<string, int> bootsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hatsDict = new Dictionary<string, int>();
    private Dictionary<string, int> hairDict = new Dictionary<string, int>();
    private Dictionary<string, int> faceDict = new Dictionary<string, int>();

    // Default Model
    private static readonly string DEFAULT_CLOTHES = "<No Top>";
    private static readonly string DEFAULT_HAT = "<No Hat>";
    private static readonly string DEFAULT_LEGS = "<No Pants>";
    private static readonly string DEFAULT_BOOTS = "<No Boots>";
    private static readonly string DEFAULT_FACE = "Face 1";
    private static readonly string DEFAULT_HAIR = "<No Hair>";

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

        if(ENABLED){
            GameObject loadedModel;

            // Load General info
            this.nameInput.text = CharacterCreationData.GetName();
            SelectRace(IdentifyRace((Race)CharacterCreationData.GetRace()));
            SelectGender(IdentifyGender(CharacterCreationData.GetMale()));

            // Load Color Pallete
            this.clothesColor1 = CharacterCreationData.GetClothesColor1();
            this.clothesColor2 = CharacterCreationData.GetClothesColor2();
            this.clothesColor3 = CharacterCreationData.GetClothesColor3();
            this.legsColor1 = CharacterCreationData.GetLegsColor1();
            this.legsColor2 = CharacterCreationData.GetLegsColor2();
            this.legsColor3 = CharacterCreationData.GetLegsColor3();
            this.hatsColor1 = CharacterCreationData.GetHatsColor1();
            this.hatsColor2 = CharacterCreationData.GetHatsColor2();
            this.hatsColor3 = CharacterCreationData.GetHatsColor3();
            this.bootsColor1 = CharacterCreationData.GetBootsColor1();
            this.bootsColor2 = CharacterCreationData.GetBootsColor2();
            this.bootsColor3 = CharacterCreationData.GetBootsColor3();
            this.faceColor1 = CharacterCreationData.GetFaceColor1();
            this.faceColor2 = CharacterCreationData.GetFaceColor2();
            this.faceColor3 = CharacterCreationData.GetFaceColor3();
            this.hairColor1 = CharacterCreationData.GetHairColor1();
            this.hairColor2 = CharacterCreationData.GetHairColor2();
            this.hairColor3 = CharacterCreationData.GetHairColor3();


            // Load Models
            this.selectedDiv = ModelType.HEADGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);

            this.selectedDiv = ModelType.FOOTGEAR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);

            this.selectedDiv = ModelType.LEGS;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);

            this.selectedDiv = ModelType.CLOTHES;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);

            this.selectedDiv = ModelType.FACE;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);

            this.selectedDiv = ModelType.HAIR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModelByCode(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv));
            SelectItem(ModelHandler.GetModelName(this.selectedDiv, CharacterCreationData.GetBodyPart(this.selectedDiv)), loadedModel);            


            ToggleDiv(this.generalButton);
        }
        else{
            ResetColors();
            LoadDefaultModel(isMale:this.selectedGenderIsMale);
        }

        ENABLED = true;
    }

    void Awake(){
        Material mat = Instantiate(this.borderPulseMaterial);
        Material matField = Instantiate(this.borderPulseMaterial);

        mat.SetFloat("_BorderSize", this.BORDER_SIZE);
        mat.SetFloat("_HorizontalAdjustment", this.HORIZONTAL_ADJUSTMENT);

        appearanceDiv.GetComponentInChildren<Image>().material = mat;
        this.nameInput.GetComponent<Image>().material = matField;

        if(!INIT){
            this.characterBuilder = new CharacterBuilderMenu(this.playerObject, this.maleAnimations, Race.HUMAN, new Material[]{Instantiate(this.prefabPlainMat), Instantiate(this.dragonlingHornMat)}, isMale:true);

            this.selectedGenderItem = this.defaultGender;
            this.selectedGenderItem.GetComponentInChildren<Text>().color = this.selectedColor;

            this.selectedRaceItem = this.defaultRace;
            this.selectedRaceItem.GetComponentInChildren<Text>().color = this.selectedColor;

            this.selectedPresetItem = this.defaultPreset;
            this.selectedPresetItem.GetComponentInChildren<Text>().color = this.selectedColor;

            this.skinColorGradient = RaceManager.GetSettings(Race.HUMAN).gradient1;
            this.skinColor = this.skinColorGradient.color2;
        }
        else{
            this.nameInput.text = CharacterCreationData.GetName();

            this.selectedGenderItem = IdentifyGender(CharacterCreationData.GetMale());
            this.selectedGenderItem.GetComponentInChildren<Text>().color = this.selectedColor;

            this.selectedRaceItem = IdentifyRace((Race)CharacterCreationData.GetRace());
            this.selectedRaceItem.GetComponentInChildren<Text>().color = this.selectedColor;

            this.selectedPresetItem = IdentifySkinPreset(CharacterCreationData.GetSkinPreset());
            this.selectedPresetItem.GetComponentInChildren<Text>().color = this.selectedColor;
        }

        ToggleDiv(this.generalButton);
    }

    public void Reset(){
        this.ENABLED = false;
        this.INIT = false;

        this.nameInput.text = "";
        this.selectedGenderIsMale = true;
        SetRace("Human");

        this.selectedGenderItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.selectedGenderItem = this.defaultGender;
        this.selectedGenderItem.GetComponentInChildren<Text>().color = this.selectedColor;

        this.selectedRaceItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.selectedRaceItem = this.defaultRace;
        this.selectedRaceItem.GetComponentInChildren<Text>().color = this.selectedColor;

        this.selectedPresetItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.selectedPresetItem = this.defaultPreset;
        this.selectedPresetItem.GetComponentInChildren<Text>().color = this.selectedColor;

        this.skinColorGradient = RaceManager.GetSettings(Race.HUMAN).gradient1;
        this.skinColor = this.skinColorGradient.color2;
        this.skinColorPicker.SetValue(1f);
        SelectSkinPreset(this.defaultPreset);

        LoadDefaultModel(isReload:true);
        this.characterBuilder.ChangeRace(Race.HUMAN, true);
        this.characterBuilder.ChangeAnimationGender(this.maleAnimations);
        UpdateColorInAllModel();
    }

    public void ToggleDiv(Button bt){
        if(bt.GetComponentInChildren<Text>().color == this.selectedColor)
            return;

        // Closes Color Picking menu if open
        this.primaryColorPicker.ResetPreview();

        ModelType selectedType = IdentifyType(bt);
        this.selectedDiv = selectedType;

        this.generalButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.clothesButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.legsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.bootsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.hatsButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.hairButton.GetComponentInChildren<Text>().color = this.notSelectedColor;
        this.faceButton.GetComponentInChildren<Text>().color = this.notSelectedColor;

        bt.GetComponentInChildren<Text>().color = this.selectedColor;

        // Deletes previous items
        foreach(Transform children in scrollViewContent.transform){
            children.gameObject.SetActive(false);
        }

        // Adds new items
        if(bt != this.generalButton){
            this.generalTab.SetActive(false);
            this.scrollViewContent.SetActive(true);
            this.slidingArea.SetActive(true);
            this.viewScrollbar.enabled = true;
            this.itemsScrollBar.value = 1;
            this.skinColorMenu.SetActive(false);

            // If it's a first load
            if(IdentifyListOfItems(bt).Count == 0){
                PopulateItemList(bt);
            }
            // If Gender has been changed -> Reload items list
            else if(this.genderUsedToLoadingItems != this.selectedGenderItem){
                for(int i = this.clothesItems.Count-1; i >= 0; i--){
                    GameObject.DestroyImmediate(this.clothesItems[i]);
                }
            }
            // If it's a div change on the same gender -> reactivate relevant items
            else{
                foreach(GameObject go in IdentifyListOfItems(bt)){
                    go.SetActive(true);
                }
            }

            this.colorPickerDiv.SetActive(false);

            // If the models are still null - a.k.a was run on Start()
            if(this.selectedFace != null){ // This should be the last Div loaded in LoadDefaultModel
                ShowColorPickers(this.characterBuilder.GetMaterialLength(this.selectedDiv));
            }
        }
        else{
            this.scrollViewContent.SetActive(false);
            this.viewScrollbar.enabled = false;
            this.generalTab.SetActive(true);
            this.slidingArea.SetActive(false);
            this.skinColorMenu.SetActive(true);

            this.primaryColorPicker.gameObject.SetActive(false);
            this.secondaryColorPicker.gameObject.SetActive(false);
            this.terciaryColorPicker.gameObject.SetActive(false);
        }
    }

    public void ClickItem(GameObject go, string handlerReferenceName){
        SelectItem(go);
        LoadModel(handlerReferenceName);
    }

    private void PopulateItemList(Button bt){
        List<GameObject> referenceList = IdentifyListOfItems(bt);
        Dictionary<string, int> referenceDict = IdentifyDict(bt);
        ModelType selectedType = IdentifyType(bt);
        int counter = 0;

        this.genderUsedToLoadingItems = this.selectedGenderItem;

        foreach(ModelInfo mi in ModelHandler.GetAllModelInfoList(selectedType, filterByGender:true, gender:IdentifyGender())){
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

            this.cachedObject.GetComponent<Button>().onClick.AddListener(() => ClickItem(referenceList[referenceDict[mi.GetHandlerName()]], mi.GetHandlerName()));
            counter++;
        }
    }

    private void LoadDefaultModel(bool isMale=true, bool isReload=false){
        string suffix;
        GameObject loadedModel;

        if(isReload){
            this.characterBuilder.ChangeArmature(this.selectedGenderIsMale);
        }

        if(isMale){
            suffix = "/M";

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

            this.selectedDiv = ModelType.CLOTHES;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_CLOTHES + suffix);
            SelectItem(DEFAULT_CLOTHES, loadedModel);

            this.selectedDiv = ModelType.HAIR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_HAIR + suffix);
            SelectItem(DEFAULT_HAIR, loadedModel);

            this.selectedDiv = ModelType.FACE;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_FACE + suffix);
            SelectItem(DEFAULT_FACE, loadedModel);
        }
        else{
            suffix = "/F";

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

            this.selectedDiv = ModelType.CLOTHES;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_CLOTHES + suffix);
            SelectItem(DEFAULT_CLOTHES, loadedModel);

            this.selectedDiv = ModelType.HAIR;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_HAIR + suffix);
            SelectItem(DEFAULT_HAIR, loadedModel);

            this.selectedDiv = ModelType.FACE;
            ToggleDiv(GetButton(this.selectedDiv));
            loadedModel = LoadModel(DEFAULT_FACE + suffix);
            SelectItem(DEFAULT_FACE, loadedModel);
        }

        this.selectedDiv = ModelType.GENERAL;
        ToggleDiv(GetButton(this.selectedDiv));
    }

    private GameObject LoadModel(string name){
        GameObject go = ModelHandler.GetModelObject(this.selectedDiv, name);
        go.name = GenerateGoName();

        this.characterBuilder.Add(this.selectedDiv, go, name);

        ShowColorPickers(this.characterBuilder.GetMaterialLength(this.selectedDiv));
        ApplyColorToModel(go);

        return go;
    }

    private GameObject LoadModelByCode(ModelType type, ushort code){
        GameObject go = ModelHandler.GetModelByCode(type, code);
        go.name = GenerateGoName();

        this.characterBuilder.Add(type, go, AddGenderString(ModelHandler.GetModelName(type, code)));

        ShowColorPickers(this.characterBuilder.GetMaterialLength(type));
        ApplyColorToModel(go);

        return go;
    }

    private void DestroyAllItems(){
        this.selectedClothesObj = null;
        this.selectedLegObj = null;
        this.selectedHatObj = null;
        this.selectedBootObj = null;
        this.selectedFaceObj = null;
        this.selectedHairObj = null;

        this.selectedClothes = null;
        this.selectedLeg = null;
        this.selectedBoot = null;
        this.selectedHat = null;
        this.selectedFace = null;
        this.selectedHair = null;


        for(int i = this.clothesItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.clothesItems[i]);
            this.clothesItems.RemoveAt(i);
        }
        for(int i = this.legsItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.legsItems[i]);
            this.legsItems.RemoveAt(i);
        }
        for(int i = this.hatsItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.hatsItems[i]);
            this.hatsItems.RemoveAt(i);
        }
        for(int i = this.bootsItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.bootsItems[i]);
            this.bootsItems.RemoveAt(i);
        }
        for(int i = this.faceItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.faceItems[i]);
            this.faceItems.RemoveAt(i);
        }
        for(int i = this.hairItems.Count-1; i >= 0; i--){
            GameObject.DestroyImmediate(this.hairItems[i]);
            this.hairItems.RemoveAt(i);
        }

        this.clothesDict.Clear();
        this.legsDict.Clear();
        this.hatsDict.Clear();
        this.bootsDict.Clear();
        this.faceDict.Clear();
        this.hairDict.Clear();
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
            case ModelType.GENERAL:
                return generalButton;
            case ModelType.FACE:
                return faceButton;
            default:
                return generalButton;
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
            this.selectedClothesObj = IdentifyItemGO(name);
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
            this.selectedLegObj = IdentifyItemGO(name);
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
            this.selectedBootObj = IdentifyItemGO(name);
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
            this.selectedHatObj = IdentifyItemGO(name);
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
            this.selectedHairObj = IdentifyItemGO(name);
            this.selectedModel = go;
        }
        else if(this.selectedDiv == ModelType.FACE){
            if(this.selectedFace != null){
                this.cachedText = this.selectedFaceObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = FetchItemByName(name).GetComponentInChildren<Text>();
            this.selectedFace = name;
            this.cachedText.color = this.selectedColor;
            this.selectedFaceObj = IdentifyItemGO(name);
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
        else if(this.selectedDiv == ModelType.FACE){
            if(this.selectedFace != null){
                this.cachedText = this.selectedFaceObj.GetComponentInChildren<Text>();
                this.cachedText.color = this.notSelectedColor;
            }

            this.cachedText = go.GetComponentInChildren<Text>();
            this.selectedFace = this.cachedText.text;
            this.cachedText.color = this.selectedColor;
            this.selectedFaceObj = go;
            this.selectedModel = go;
        }
    }

    public void SelectGender(Button bt){
        if(this.selectedGenderItem == bt){
            return;
        }

        if(this.selectedGenderItem != null && bt != this.selectedGenderItem){
            this.selectedGenderItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        }

        this.selectedGenderItem = bt;

        this.cachedText = bt.GetComponentInChildren<Text>();

        this.cachedText.color = this.selectedColor;

        SetGender(this.cachedText.text);
        DestroyAllItems();
        
        PopulateItemList(this.clothesButton);
        PopulateItemList(this.legsButton);
        PopulateItemList(this.hatsButton);
        PopulateItemList(this.bootsButton);
        PopulateItemList(this.faceButton);
        PopulateItemList(this.hairButton);

        LoadDefaultModel(isMale:this.selectedGenderIsMale, isReload:true);
        this.characterBuilder.ChangeGender(this.race, this.selectedGenderIsMale);

        if(this.selectedGenderIsMale)
            this.characterBuilder.ChangeAnimationGender(this.maleAnimations);
        else
            this.characterBuilder.ChangeAnimationGender(this.femaleAnimations);

        this.selectedDiv = ModelType.GENERAL;
        ToggleDiv(this.generalButton);
    }

    public void SelectRace(Button bt){
        if(this.selectedRaceItem == bt){
            return;
        }

        if(this.selectedRaceItem != null){
            this.selectedRaceItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        }
        
        this.selectedRaceItem = bt;
        this.cachedText = bt.GetComponentInChildren<Text>();

        this.cachedText.color = this.selectedColor;

        SetRace(this.cachedText.text);
        this.characterBuilder.ChangeRace(this.race, this.selectedGenderIsMale);

        // Setting Skin Material
        if(this.race != Race.DRAGONLING)
            this.skinMat = Instantiate(this.prefabPlainMat);
        else
            this.skinMat = Instantiate(this.dragonSkinMat);
        
        SelectSkinPreset(this.defaultPreset);


        UpdateColorInAllModel();
    }

    public void SelectSkinPreset(Button bt){
        if(this.selectedPresetItem == bt && this.selectedPresetItem != this.defaultPreset)
            return;

        if(this.selectedPresetItem != null){
            this.selectedPresetItem.GetComponentInChildren<Text>().color = this.notSelectedColor;
        }

        this.selectedPresetItem = bt;
        this.cachedText = bt.GetComponentInChildren<Text>();

        this.cachedText.color = this.selectedColor;

        if(this.cachedText.text == "P1"){
            this.skinColorGradient = RaceManager.GetSettings(this.race).gradient1;
        }
        else if(this.cachedText.text == "P2"){
            this.skinColorGradient = RaceManager.GetSettings(this.race).gradient2;
        }
        else if(this.cachedText.text == "P3"){
            this.skinColorGradient = RaceManager.GetSettings(this.race).gradient3;
        }
        else{
            this.skinColorGradient = RaceManager.GetSettings(this.race).gradient1;            
        }

        this.skinColorPicker.ChangeGradient(this.skinColorGradient);
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
        else if(this.selectedDiv == ModelType.FACE){
            if(picker == primaryColorPicker){
                this.faceColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.faceColor2 = c;
            }
            else{
                this.faceColor3 = c;
            }
        }
        else if(this.selectedDiv == ModelType.HAIR){
            if(picker == primaryColorPicker){
                this.hairColor1 = c;
            }
            else if(picker == secondaryColorPicker){
                this.hairColor2 = c;
            }
            else{
                this.hairColor3 = c;
            }
        }
        else{
            return;
        }

        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
    }

    public void ChangeMainColor(object obj){
        Color color = (Color)obj;
        this.skinColor = color;

        UpdateColorInAllModel();
    }

    public Gradient GetSkinColorGradient(){return this.skinColorGradient;}

    public void OpenCharacterCreationDataMenu(){
        if(this.nameInput.text == "")
            return;

        CharacterCreationData.SetName(this.nameInput.text);
        CharacterCreationData.SetRace(this.race);
        CharacterCreationData.SetMale(this.selectedGenderIsMale);

        CharacterCreationData.SetBodyPart(ModelType.CLOTHES, ModelHandler.GetCode(ModelType.CLOTHES, IdentifySelectedName(ModelType.CLOTHES)));
        CharacterCreationData.SetBodyPart(ModelType.LEGS, ModelHandler.GetCode(ModelType.LEGS, IdentifySelectedName(ModelType.LEGS)));
        CharacterCreationData.SetBodyPart(ModelType.FOOTGEAR, ModelHandler.GetCode(ModelType.FOOTGEAR, IdentifySelectedName(ModelType.FOOTGEAR)));
        CharacterCreationData.SetBodyPart(ModelType.HEADGEAR, ModelHandler.GetCode(ModelType.HEADGEAR, IdentifySelectedName(ModelType.HEADGEAR)));
        CharacterCreationData.SetBodyPart(ModelType.FACE, ModelHandler.GetCode(ModelType.FACE, IdentifySelectedName(ModelType.FACE)));
        CharacterCreationData.SetBodyPart(ModelType.HAIR, ModelHandler.GetCode(ModelType.HAIR, IdentifySelectedName(ModelType.HAIR)));

        CharacterCreationData.SetAttribute(AttributeName.STRENGTH, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.STRENGTH));
        CharacterCreationData.SetAttribute(AttributeName.PRECISION, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.PRECISION));
        CharacterCreationData.SetAttribute(AttributeName.VITALITY, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.VITALITY));
        CharacterCreationData.SetAttribute(AttributeName.EVASION, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.EVASION));
        CharacterCreationData.SetAttribute(AttributeName.MAGIC, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.MAGIC));
        CharacterCreationData.SetAttribute(AttributeName.CHARISMA, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.CHARISMA));
        CharacterCreationData.SetAttribute(AttributeName.FIRE_RESISTANCE, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.FIRE_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.ICE_RESISTANCE, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.ICE_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.LIGHTNING_RESISTANCE, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.LIGHTNING_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.POISON_RESISTANCE, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.POISON_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.CURSE_RESISTANCE, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.CURSE_RESISTANCE));
        CharacterCreationData.SetAttribute(AttributeName.SPEED, 0, AttributeIncreaseTableRace.GetAttributeIncrease(this.race, AttributeName.SPEED));

        CharacterCreationData.SetClothesColor1(this.clothesColor1);
        CharacterCreationData.SetClothesColor2(this.clothesColor2);
        CharacterCreationData.SetClothesColor3(this.clothesColor3);
        CharacterCreationData.SetLegsColor1(this.legsColor1);
        CharacterCreationData.SetLegsColor2(this.legsColor2);
        CharacterCreationData.SetLegsColor3(this.legsColor3);
        CharacterCreationData.SetHatsColor1(this.hatsColor1);
        CharacterCreationData.SetHatsColor2(this.hatsColor2);
        CharacterCreationData.SetHatsColor3(this.hatsColor3);
        CharacterCreationData.SetBootsColor1(this.bootsColor1);
        CharacterCreationData.SetBootsColor2(this.bootsColor2);
        CharacterCreationData.SetBootsColor3(this.bootsColor3);
        CharacterCreationData.SetFaceColor1(this.faceColor1);
        CharacterCreationData.SetFaceColor2(this.faceColor2);
        CharacterCreationData.SetFaceColor3(this.faceColor3);
        CharacterCreationData.SetHairColor1(this.hairColor1);
        CharacterCreationData.SetHairColor2(this.hairColor2);
        CharacterCreationData.SetHairColor3(this.hairColor3);

        CharacterCreationData.SetSkinPreset(IdentifySkinPreset(this.selectedPresetItem));
        CharacterCreationData.SetSkin(this.skinColor);

        this.INIT = true;

        this.RequestMenuChange(MenuID.CHARACTER_CREATION_DATA);
    }

    public void LeaveCharacterCreation(){
        CharacterCreationData.Reset();
        Reset();
        this.statusMenu.Reset();
        this.religionMenu.Reset();

        this.RequestMenuChange(MenuID.SELECT_WORLD);
    }

    private void UpdateColorInAllModel(){
        ModelType currentDiv = this.selectedDiv;

        this.selectedDiv = ModelType.CLOTHES;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
        this.selectedDiv = ModelType.LEGS;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
        this.selectedDiv = ModelType.FOOTGEAR;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
        this.selectedDiv = ModelType.HEADGEAR;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
        this.selectedDiv = ModelType.FACE;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));
        this.selectedDiv = ModelType.HAIR;
        ApplyColorToModel(this.characterBuilder.Get(this.selectedDiv));

        this.selectedDiv = currentDiv;
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
        if(button == this.faceButton)
            return ModelType.FACE;
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
        if(button == this.faceButton)
            return this.faceItems;
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
        if(button == this.faceButton)
            return this.faceDict;
        return this.clothesDict;          
    }

    private string IdentifySelectedName(ModelType type){
        string fullname = "";

        if(type == ModelType.CLOTHES)
            fullname = this.selectedClothes;
        else if(type == ModelType.LEGS)
            fullname = this.selectedLeg;
        else if(type == ModelType.FOOTGEAR)
            fullname = this.selectedBoot;
        else if(type == ModelType.HEADGEAR)
            fullname = this.selectedHat;
        else if(type == ModelType.FACE)
            fullname = this.selectedFace;
        else if(type == ModelType.HAIR)
            fullname = this.selectedHair;

        if(this.selectedGenderIsMale)
            return fullname + "/M";
        else
            return fullname + "/F";
    }

    private string AddGenderString(string name){
        if(this.selectedGenderIsMale)
            return name + "/M";
        return name + "/F";
    }

    private GameObject IdentifyItemGO(string name){
        foreach(Text text in this.scrollViewContent.GetComponentsInChildren<Text>()){
            if(text.text == name){
                return text.transform.parent.gameObject;
            }
        }

        return null;
    }

    private char IdentifyGender(){
        if(this.selectedGenderIsMale)
            return 'M';
        return 'F';
    }

    private Button IdentifyGender(bool isMale){
        if(isMale)
            return this.maleGender;
        return this.femaleGender;
    }

    private Button IdentifyRace(Race r){
        switch(r){
            case Race.HUMAN:
                return this.humanRace;
            case Race.ELF:
                return this.elfRace;
            case Race.DWARF:
                return this.dwarfRace;
            case Race.ORC:
                return this.orcRace;
            case Race.HALFLING:
                return this.halflingRace;
            case Race.DRAGONLING:
                return this.dragonlingRace;
            case Race.UNDEAD:
                return this.undeadRace;
            default:
                return this.humanRace;
        }
    }

    private Button IdentifySkinPreset(byte b){
        if(b == 1)
            return this.preset1;
        if(b == 2)
            return this.preset2;
        return this.preset3;
    }

    private byte IdentifySkinPreset(Button b){
        if(b == this.preset1)
            return 1;
        if(b == this.preset2)
            return 2;
        return 3;
    }

    private void SetGender(string text){
        if(text == "Male")
            this.selectedGenderIsMale = true;
        else
            this.selectedGenderIsMale = false;
    }

    private void SetRace(string text){
        switch(text){
            case "Human":
                this.race = Race.HUMAN;
                return;
            case "Elf":
                this.race = Race.ELF;
                return;
            case "Dwarf":
                this.race = Race.DWARF;
                return;
            case "Orc":
                this.race = Race.ORC;
                return;
            case "Halfling":
                this.race = Race.HALFLING;
                return;
            case "Dragonling":
                this.race = Race.DRAGONLING;
                return;
            case "Undead":
                this.race = Race.UNDEAD;
                return;
            default:
                this.race = Race.HUMAN;
                return;
        }
    }

    private Race IdentifyRace(string text){
        switch(text){
            case "Human":
                return Race.HUMAN;
            case "Elf":
                return Race.ELF;
            case "Dwarf":
                return Race.DWARF;
            case "Orc":
                return Race.ORC;
            case "Halfling":
                return Race.HALFLING;
            case "Dragonling":
                return Race.DRAGONLING;
            case "Undead":
                return Race.UNDEAD;
            default:
                return Race.HUMAN;
        }
    }

    private string GenerateGoName(){
        return Enum.GetName(typeof(ModelType), (byte)this.selectedDiv);
    }

    private void ShowColorPickers(int numberOfMaterials){
        Color p, s, t;

        // Gets color from ModelType
        if(this.selectedDiv == ModelType.CLOTHES){
            p = this.clothesColor1;
            s = this.clothesColor2;
            t = this.clothesColor3;
        }
        else if(this.selectedDiv == ModelType.LEGS){
            p = this.legsColor1;
            s = this.legsColor2;
            t = this.legsColor3;
        }
        else if(this.selectedDiv == ModelType.FOOTGEAR){
            p = this.bootsColor1;
            s = this.bootsColor2;
            t = this.bootsColor3;
        }
        else if(this.selectedDiv == ModelType.HEADGEAR){
            p = this.hatsColor1;
            s = this.hatsColor2;
            t = this.hatsColor3;
        }
        else if(this.selectedDiv == ModelType.FACE){
            p = this.faceColor1;
            s = this.faceColor2;
            t = this.faceColor3;
        }
        else if(this.selectedDiv == ModelType.HAIR){
            p = this.hairColor1;
            s = this.hairColor2;
            t = this.hairColor3;
        }
        else{
            p = Color.black;
            s = Color.black;
            t = Color.black;
        }

        // Activates Color Pickers and add the color
        if(numberOfMaterials == 1){
            this.primaryColorPicker.gameObject.SetActive(false);
            this.secondaryColorPicker.gameObject.SetActive(false);
            this.terciaryColorPicker.gameObject.SetActive(false);
        }
        else if(numberOfMaterials == 2){
            this.primaryColorPicker.gameObject.SetActive(true);
            this.primaryColorPicker.SetDefiniteColor(p);
            this.secondaryColorPicker.gameObject.SetActive(false);
            this.terciaryColorPicker.gameObject.SetActive(false);            
        }
        else if(numberOfMaterials == 3){
            this.primaryColorPicker.gameObject.SetActive(true);
            this.primaryColorPicker.SetDefiniteColor(p);
            this.secondaryColorPicker.gameObject.SetActive(true);
            this.secondaryColorPicker.SetDefiniteColor(s);
            this.terciaryColorPicker.gameObject.SetActive(false);            
        }
        else if(numberOfMaterials == 4){
            this.primaryColorPicker.gameObject.SetActive(true);
            this.primaryColorPicker.SetDefiniteColor(p);
            this.secondaryColorPicker.gameObject.SetActive(true);
            this.secondaryColorPicker.SetDefiniteColor(s);
            this.terciaryColorPicker.gameObject.SetActive(true);
            this.terciaryColorPicker.SetDefiniteColor(t);            
        }
        else if(numberOfMaterials <= 0){
            this.primaryColorPicker.gameObject.SetActive(false);
            this.secondaryColorPicker.gameObject.SetActive(false);
            this.terciaryColorPicker.gameObject.SetActive(false);
        }
        else{
            this.primaryColorPicker.gameObject.SetActive(true);
            this.primaryColorPicker.SetDefiniteColor(p);
            this.secondaryColorPicker.gameObject.SetActive(true);
            this.secondaryColorPicker.SetDefiniteColor(s);
            this.terciaryColorPicker.gameObject.SetActive(true);
            this.terciaryColorPicker.SetDefiniteColor(t);    
        }
    }



    private void ApplyColorToModel(GameObject go){
        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();

        if(smr == null)
            return;

        Material[] materials = smr.materials;

        if(this.selectedDiv == ModelType.CLOTHES){
            if(materials.Length > 1){
                materials[1] = this.clothesMat1;
                materials[1].SetColor("_Color", this.clothesColor1);
            }
            if(materials.Length > 2){
                materials[2] = this.clothesMat2;
                materials[2].SetColor("_Color", this.clothesColor2);
            }
            if(materials.Length > 3){
                materials[3] = this.clothesMat3;
                materials[3].SetColor("_Color", this.clothesColor3);
            }
        }
        else if(this.selectedDiv == ModelType.LEGS){
            if(materials.Length > 1){
                materials[1] = this.legsMat1;
                materials[1].SetColor("_Color", this.legsColor1);
            }
            if(materials.Length > 2){
                materials[2] = this.legsMat2;
                materials[2].SetColor("_Color", this.legsColor2);
            }
            if(materials.Length > 3){
                materials[3] = this.legsMat3;
                materials[3].SetColor("_Color", this.legsColor3);
            }
        }
        else if(this.selectedDiv == ModelType.FOOTGEAR){
            if(materials.Length > 1){
                materials[1] = this.bootsMat1;
                materials[1].SetColor("_Color", this.bootsColor1);
            }
            if(materials.Length > 2){
                materials[2] = this.bootsMat2;
                materials[2].SetColor("_Color", this.bootsColor2);
            }
            if(materials.Length > 3){
                materials[3] = this.bootsMat3;
                materials[3].SetColor("_Color", this.bootsColor3);
            }
        }
        else if(this.selectedDiv == ModelType.HEADGEAR){
            if(materials.Length > 1){
                materials[1] = this.hatsMat1;
                materials[1].SetColor("_Color", this.hatsColor1);
            }
            if(materials.Length > 2){
                materials[2] = this.hatsMat2;
                materials[2].SetColor("_Color", this.hatsColor2);
            }
            if(materials.Length > 3){
                materials[3] = this.hatsMat3;
                materials[3].SetColor("_Color", this.hatsColor3);
            }
        }
        else if(this.selectedDiv == ModelType.FACE){
            if(materials.Length > 1){
                materials[1] = this.faceMat1;
                materials[1].SetColor("_Color", this.faceColor1);
                materials[1].SetColor("_IrisColor", this.faceColor2);
            }
            if(materials.Length > 2){
                materials[2] = this.faceMat2;
                materials[2].SetColor("_Color", this.faceColor3);
            }
            if(materials.Length > 3){
                materials[3] = this.faceMat3;
                materials[3].SetColor("_Color", Color.white);
            }
        }
        else if(this.selectedDiv == ModelType.HAIR){
            if(materials.Length > 1){
                materials[1] = this.hairMat1;
                materials[1].SetColor("_Color", this.hairColor1);
            }
            if(materials.Length > 2){
                materials[2] = this.hairMat2;
                materials[2].SetColor("_Color", this.hairColor2);
            }
            if(materials.Length > 3){
                materials[3] = this.hairMat3;
                materials[3].SetColor("_Color", this.hairColor3);
            }
        }

        materials[0] = this.skinMat;
        materials[0].SetColor("_Color", this.skinColor);

        this.characterBuilder.ChangeAddonColor(this.skinColor, this.race);

        go.GetComponent<SkinnedMeshRenderer>().materials = materials;
    }

    private void ResetColors(){
        if(this.race != Race.DRAGONLING)
            this.skinMat = Instantiate(this.prefabPlainMat);
        else
            this.skinMat = Instantiate(this.dragonSkinMat);

        this.clothesMat1 = Instantiate(this.prefabPlainMat);
        this.clothesMat2 = Instantiate(this.prefabPlainMat);
        this.clothesMat3 = Instantiate(this.prefabPlainMat);
        this.legsMat1 = Instantiate(this.prefabPlainMat);
        this.legsMat2 = Instantiate(this.prefabPlainMat);
        this.legsMat3 = Instantiate(this.prefabPlainMat);
        this.bootsMat1 = Instantiate(this.prefabPlainMat);
        this.bootsMat2 = Instantiate(this.prefabPlainMat);
        this.bootsMat3 = Instantiate(this.prefabPlainMat);
        this.hatsMat1 = Instantiate(this.prefabPlainMat);
        this.hatsMat2 = Instantiate(this.prefabPlainMat);
        this.hatsMat3 = Instantiate(this.prefabPlainMat);
        this.hairMat1 = Instantiate(this.prefabPlainMat);
        this.hairMat2 = Instantiate(this.prefabPlainMat);
        this.hairMat3 = Instantiate(this.prefabPlainMat);
        this.faceMat1 = Instantiate(this.eyeIrisMat);
        this.faceMat2 = Instantiate(this.prefabPlainMat);
        this.faceMat3 = Instantiate(this.prefabPlainMat);

        this.skinColor = this.skinColorGradient.color2;
        this.clothesColor1 = new Color(.57f, .55f, .275f);
        this.clothesColor2 = new Color(.761f, .54f, .286f);
        this.clothesColor3 = new Color(.87f, .81f, .39f);
        this.legsColor1 = new Color(.447f, .49f, .37f);
        this.legsColor2 = new Color(.588f, .43f, .2f);
        this.legsColor3 = new Color(.624f, .77f, .337f);
        this.bootsColor1 = new Color(.478f, .26f, .14f);
        this.bootsColor2 = new Color(.388f, .353f, .333f);
        this.bootsColor3 = new Color(.74f, .74f, .74f);
        this.hatsColor1 = new Color(.51f, .396f, .27f);
        this.hatsColor2 = new Color(.64f, .8f, .44f);
        this.hatsColor3 = new Color(.67f, .67f, .67f);
        this.hairColor1 = new Color(.54f, .37f, .2f);
        this.hairColor2 = new Color(.7f, .6f, .5f);
        this.hairColor3 = new Color(.78f, .66f, .25f);
        this.faceColor1 = new Color(.7f, .42f, .114f);
        this.faceColor2 = new Color(.31f, .25f, .184f);
        this.faceColor3 = new Color(.812f, .47f, .39f);

        this.skinMat.SetColor("_Color", this.skinColor);
        this.clothesMat1.SetColor("_Color", this.clothesColor1);
        this.clothesMat2.SetColor("_Color", this.clothesColor2);
        this.clothesMat3.SetColor("_Color", this.clothesColor3);
        this.legsMat1.SetColor("_Color", this.legsColor1);
        this.legsMat2.SetColor("_Color", this.legsColor2);
        this.legsMat3.SetColor("_Color", this.legsColor3);
        this.bootsMat1.SetColor("_Color", this.bootsColor1);
        this.bootsMat2.SetColor("_Color", this.bootsColor2);
        this.bootsMat3.SetColor("_Color", this.bootsColor3);
        this.hatsMat1.SetColor("_Color", this.hatsColor1);
        this.hatsMat2.SetColor("_Color", this.hatsColor2);
        this.hatsMat3.SetColor("_Color", this.hatsColor3);
        this.hairMat1.SetColor("_Color", this.hairColor1);
        this.hairMat2.SetColor("_Color", this.hairColor2);
        this.hairMat3.SetColor("_Color", this.hairColor3);
        this.faceMat1.SetColor("_Color", this.faceColor1);
        this.faceMat1.SetColor("_IrisColor", this.faceColor2);
        this.faceMat2.SetColor("_Color", this.faceColor3);
        this.faceMat3.SetColor("_Color", Color.white);
    }
}