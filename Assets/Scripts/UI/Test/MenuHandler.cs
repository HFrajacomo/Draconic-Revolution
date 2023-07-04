using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuHandler : MonoBehaviour
{
    public UIDocument htmlMainMenu;
    public UIDocument htmlSingleMenu;
    public UIDocument htmlMultiMenu;
    public UIDocument htmlOptionMenu;
    public UIDocument htmlCreateWorld;
    public StyleSheet mainStyle;
    public StyleSheet defaultStyle;
    public VisualElement root;

    public Button singleplayerButton;
    public Button multiplayerButton;
    public Button optionsButton;
    public Button exitButton;

    private List<Button> buttons;
    private int i = 0;

    void Start()
    {
        this.htmlMainMenu = GetComponent<UIDocument>();

        // Load the UXML file and add it to the root VisualElement
        this.root = this.htmlMainMenu.rootVisualElement;
        this.root.styleSheets.Add(this.mainStyle);

        this.buttons = this.root.Query<Button>().ToList();

        this.singleplayerButton = this.buttons[0];
        this.multiplayerButton = this.buttons[1];
        this.optionsButton = this.buttons[2];
        this.exitButton = this.buttons[3];

        InitClickEvents();
    }

    void Update(){
        this.i++;
    }

    public void InitClickEvents(){
        this.singleplayerButton.clicked += () => PrintTest(this.i);
    }

    private void PrintTest(int a){
        Debug.Log(a);
    }
}