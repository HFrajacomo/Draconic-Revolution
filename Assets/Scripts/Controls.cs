// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/Controls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @Controls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @Controls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Controls"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""378eecaa-d866-45ba-8af2-2a32322b927f"",
            ""actions"": [
                {
                    ""name"": ""Movement"",
                    ""type"": ""Value"",
                    ""id"": ""a863e12e-ecb2-4281-a057-d30f54a44935"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": ""NormalizeVector2"",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""2555b908-176c-4cab-aea7-e79359628764"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""Primary Action"",
                    ""type"": ""Button"",
                    ""id"": ""eaeb59d7-ca2b-458d-9c46-67fd0f7f3f11"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Secondary Action"",
                    ""type"": ""Button"",
                    ""id"": ""710f1945-d1a2-451f-a52a-bfea220cd792"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""2130e78c-cdaa-4174-a62a-8d8958facd7e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Mouse Look"",
                    ""type"": ""Value"",
                    ""id"": ""6f61dff5-6a91-4041-b888-22ef7d85d4b0"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Toggle Gravity"",
                    ""type"": ""Button"",
                    ""id"": ""19097fbd-caec-423a-96f3-e008033cbb0f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""PrefabRead"",
                    ""type"": ""Button"",
                    ""id"": ""b2bf4501-9b98-4164-b0f4-7702b11615db"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""PrefabReadAir"",
                    ""type"": ""Button"",
                    ""id"": ""4f6b10d2-cae0-48e6-8a31-cfcf437ba9fc"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleFreeCam"",
                    ""type"": ""Button"",
                    ""id"": ""f0052053-ee62-4f4d-9825-efafbc08c16a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Shifting"",
                    ""type"": ""Button"",
                    ""id"": ""40c019f8-c03f-4d59-a503-493579898165"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""DebugKey"",
                    ""type"": ""Button"",
                    ""id"": ""d5e78c4f-e6e3-41f2-917b-39985b5642d5"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""ToggleHUD"",
                    ""type"": ""Button"",
                    ""id"": ""e65e0f1c-4dc0-4fc2-92f3-ae6f3b5148e2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll1"",
                    ""type"": ""Button"",
                    ""id"": ""e5a12e99-f7e6-497c-af79-f41b25cbceda"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll2"",
                    ""type"": ""Button"",
                    ""id"": ""81e7f2d2-5958-45a9-9e56-e35f0629b8c8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll3"",
                    ""type"": ""Button"",
                    ""id"": ""08576c05-01ac-4c94-b319-3b3e47a753e8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll4"",
                    ""type"": ""Button"",
                    ""id"": ""bdca2870-ddd3-4079-af4c-3055ba9d80c8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll5"",
                    ""type"": ""Button"",
                    ""id"": ""53d832f3-3fe0-41c7-bfbd-48323ceff3d7"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll6"",
                    ""type"": ""Button"",
                    ""id"": ""1eec7ff8-febc-4bbb-b03b-f39924d4ff8f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll7"",
                    ""type"": ""Button"",
                    ""id"": ""ac118276-3d83-48cc-ba46-d1164849a329"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll8"",
                    ""type"": ""Button"",
                    ""id"": ""cdfdb68f-21e9-483a-90b5-0573e541b500"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Scroll9"",
                    ""type"": ""Button"",
                    ""id"": ""3b7188cf-173d-46a0-b468-ea006cea0f6c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""MouseScroll"",
                    ""type"": ""Value"",
                    ""id"": ""290c46cd-5ca4-40ce-9f59-7704ea6951ce"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""7de5f9db-e348-4b53-b7cd-5d7343980857"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": ""Normalize(min=-1,max=1)"",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""1f5abed6-1c61-4ef4-af4c-ba0d5c9ffe1d"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""98c98eba-798f-4e44-a25d-0f1bb27ecf06"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""59d42dd0-3bf9-447a-a25a-f7997ecd12ab"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""e6c7eddc-aae2-4f95-858b-7b6f8862b041"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""55a1e69a-d5ef-4e76-83bf-d45bffd7b209"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""09c5df05-d889-49d2-8c32-3469364f6570"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Primary Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3b06e507-6506-4fb2-9326-66a8caa5c1fa"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Secondary Action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e7e4a5c8-08c5-4ffa-a69e-9689848ec291"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""95bae44c-e232-4b82-928d-8a2d1c6a842b"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Mouse Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8985a6c9-cc7a-469a-a108-28c55685ecb8"",
                    ""path"": ""<Keyboard>/h"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Toggle Gravity"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""72dce022-52b3-4940-8198-58778524b85b"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""PrefabRead"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1c1ad14-407f-4393-b2d2-67e0f048273f"",
                    ""path"": ""<Keyboard>/o"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""PrefabReadAir"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""07d1f2b4-2c49-41d8-abe1-ccefcc01869f"",
                    ""path"": ""<Keyboard>/g"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""ToggleFreeCam"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""02d43d1e-16d0-41bc-abd2-bd2510bcdcac"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Shifting"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""572f82ba-0257-4d27-8a3e-1e46495bd67a"",
                    ""path"": ""<Keyboard>/numpad0"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""DebugKey"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""65d27db9-ac78-424f-8490-4356089b6d41"",
                    ""path"": ""<Keyboard>/f1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""ToggleHUD"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""dcd5e309-8471-4a49-afcd-6331b35805e4"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1594b20d-6b80-4978-8408-2102d15b614b"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""aac77255-df29-4cf0-baf1-0da125d7ddbf"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll3"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8a8888f0-4071-4c75-b1c8-ba0a14e65552"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll4"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ef907ebb-6ee7-4929-9d53-6baf42517d65"",
                    ""path"": ""<Keyboard>/5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll5"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2d6c91d3-6f95-4a6a-90b7-083a47850da8"",
                    ""path"": ""<Keyboard>/6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll6"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b8794cad-8df8-4107-a1b7-238c830bbb0e"",
                    ""path"": ""<Keyboard>/7"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll7"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b0a9fca5-3d12-4acf-b686-c4d6682dd814"",
                    ""path"": ""<Keyboard>/8"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll8"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""406d11c5-c798-4f4a-a06b-88ab4389cabc"",
                    ""path"": ""<Keyboard>/9"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""Scroll9"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8dbeba68-82ef-43dc-b6f0-794a5bab0a68"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard and Mouse"",
                    ""action"": ""MouseScroll"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard and Mouse"",
            ""bindingGroup"": ""Keyboard and Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_Movement = m_Player.FindAction("Movement", throwIfNotFound: true);
        m_Player_Jump = m_Player.FindAction("Jump", throwIfNotFound: true);
        m_Player_PrimaryAction = m_Player.FindAction("Primary Action", throwIfNotFound: true);
        m_Player_SecondaryAction = m_Player.FindAction("Secondary Action", throwIfNotFound: true);
        m_Player_Interact = m_Player.FindAction("Interact", throwIfNotFound: true);
        m_Player_MouseLook = m_Player.FindAction("Mouse Look", throwIfNotFound: true);
        m_Player_ToggleGravity = m_Player.FindAction("Toggle Gravity", throwIfNotFound: true);
        m_Player_PrefabRead = m_Player.FindAction("PrefabRead", throwIfNotFound: true);
        m_Player_PrefabReadAir = m_Player.FindAction("PrefabReadAir", throwIfNotFound: true);
        m_Player_ToggleFreeCam = m_Player.FindAction("ToggleFreeCam", throwIfNotFound: true);
        m_Player_Shifting = m_Player.FindAction("Shifting", throwIfNotFound: true);
        m_Player_DebugKey = m_Player.FindAction("DebugKey", throwIfNotFound: true);
        m_Player_ToggleHUD = m_Player.FindAction("ToggleHUD", throwIfNotFound: true);
        m_Player_Scroll1 = m_Player.FindAction("Scroll1", throwIfNotFound: true);
        m_Player_Scroll2 = m_Player.FindAction("Scroll2", throwIfNotFound: true);
        m_Player_Scroll3 = m_Player.FindAction("Scroll3", throwIfNotFound: true);
        m_Player_Scroll4 = m_Player.FindAction("Scroll4", throwIfNotFound: true);
        m_Player_Scroll5 = m_Player.FindAction("Scroll5", throwIfNotFound: true);
        m_Player_Scroll6 = m_Player.FindAction("Scroll6", throwIfNotFound: true);
        m_Player_Scroll7 = m_Player.FindAction("Scroll7", throwIfNotFound: true);
        m_Player_Scroll8 = m_Player.FindAction("Scroll8", throwIfNotFound: true);
        m_Player_Scroll9 = m_Player.FindAction("Scroll9", throwIfNotFound: true);
        m_Player_MouseScroll = m_Player.FindAction("MouseScroll", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Player
    private readonly InputActionMap m_Player;
    private IPlayerActions m_PlayerActionsCallbackInterface;
    private readonly InputAction m_Player_Movement;
    private readonly InputAction m_Player_Jump;
    private readonly InputAction m_Player_PrimaryAction;
    private readonly InputAction m_Player_SecondaryAction;
    private readonly InputAction m_Player_Interact;
    private readonly InputAction m_Player_MouseLook;
    private readonly InputAction m_Player_ToggleGravity;
    private readonly InputAction m_Player_PrefabRead;
    private readonly InputAction m_Player_PrefabReadAir;
    private readonly InputAction m_Player_ToggleFreeCam;
    private readonly InputAction m_Player_Shifting;
    private readonly InputAction m_Player_DebugKey;
    private readonly InputAction m_Player_ToggleHUD;
    private readonly InputAction m_Player_Scroll1;
    private readonly InputAction m_Player_Scroll2;
    private readonly InputAction m_Player_Scroll3;
    private readonly InputAction m_Player_Scroll4;
    private readonly InputAction m_Player_Scroll5;
    private readonly InputAction m_Player_Scroll6;
    private readonly InputAction m_Player_Scroll7;
    private readonly InputAction m_Player_Scroll8;
    private readonly InputAction m_Player_Scroll9;
    private readonly InputAction m_Player_MouseScroll;
    public struct PlayerActions
    {
        private @Controls m_Wrapper;
        public PlayerActions(@Controls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Movement => m_Wrapper.m_Player_Movement;
        public InputAction @Jump => m_Wrapper.m_Player_Jump;
        public InputAction @PrimaryAction => m_Wrapper.m_Player_PrimaryAction;
        public InputAction @SecondaryAction => m_Wrapper.m_Player_SecondaryAction;
        public InputAction @Interact => m_Wrapper.m_Player_Interact;
        public InputAction @MouseLook => m_Wrapper.m_Player_MouseLook;
        public InputAction @ToggleGravity => m_Wrapper.m_Player_ToggleGravity;
        public InputAction @PrefabRead => m_Wrapper.m_Player_PrefabRead;
        public InputAction @PrefabReadAir => m_Wrapper.m_Player_PrefabReadAir;
        public InputAction @ToggleFreeCam => m_Wrapper.m_Player_ToggleFreeCam;
        public InputAction @Shifting => m_Wrapper.m_Player_Shifting;
        public InputAction @DebugKey => m_Wrapper.m_Player_DebugKey;
        public InputAction @ToggleHUD => m_Wrapper.m_Player_ToggleHUD;
        public InputAction @Scroll1 => m_Wrapper.m_Player_Scroll1;
        public InputAction @Scroll2 => m_Wrapper.m_Player_Scroll2;
        public InputAction @Scroll3 => m_Wrapper.m_Player_Scroll3;
        public InputAction @Scroll4 => m_Wrapper.m_Player_Scroll4;
        public InputAction @Scroll5 => m_Wrapper.m_Player_Scroll5;
        public InputAction @Scroll6 => m_Wrapper.m_Player_Scroll6;
        public InputAction @Scroll7 => m_Wrapper.m_Player_Scroll7;
        public InputAction @Scroll8 => m_Wrapper.m_Player_Scroll8;
        public InputAction @Scroll9 => m_Wrapper.m_Player_Scroll9;
        public InputAction @MouseScroll => m_Wrapper.m_Player_MouseScroll;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
            {
                @Movement.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMovement;
                @Movement.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMovement;
                @Movement.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMovement;
                @Jump.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnJump;
                @Jump.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnJump;
                @Jump.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnJump;
                @PrimaryAction.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrimaryAction;
                @PrimaryAction.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrimaryAction;
                @PrimaryAction.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrimaryAction;
                @SecondaryAction.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSecondaryAction;
                @SecondaryAction.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSecondaryAction;
                @SecondaryAction.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSecondaryAction;
                @Interact.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnInteract;
                @Interact.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnInteract;
                @Interact.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnInteract;
                @MouseLook.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseLook;
                @MouseLook.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseLook;
                @MouseLook.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseLook;
                @ToggleGravity.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleGravity;
                @ToggleGravity.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleGravity;
                @ToggleGravity.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleGravity;
                @PrefabRead.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabRead;
                @PrefabRead.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabRead;
                @PrefabRead.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabRead;
                @PrefabReadAir.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabReadAir;
                @PrefabReadAir.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabReadAir;
                @PrefabReadAir.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPrefabReadAir;
                @ToggleFreeCam.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleFreeCam;
                @ToggleFreeCam.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleFreeCam;
                @ToggleFreeCam.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleFreeCam;
                @Shifting.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnShifting;
                @Shifting.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnShifting;
                @Shifting.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnShifting;
                @DebugKey.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDebugKey;
                @DebugKey.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDebugKey;
                @DebugKey.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDebugKey;
                @ToggleHUD.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleHUD;
                @ToggleHUD.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleHUD;
                @ToggleHUD.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnToggleHUD;
                @Scroll1.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll1;
                @Scroll1.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll1;
                @Scroll1.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll1;
                @Scroll2.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll2;
                @Scroll2.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll2;
                @Scroll2.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll2;
                @Scroll3.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll3;
                @Scroll3.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll3;
                @Scroll3.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll3;
                @Scroll4.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll4;
                @Scroll4.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll4;
                @Scroll4.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll4;
                @Scroll5.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll5;
                @Scroll5.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll5;
                @Scroll5.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll5;
                @Scroll6.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll6;
                @Scroll6.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll6;
                @Scroll6.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll6;
                @Scroll7.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll7;
                @Scroll7.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll7;
                @Scroll7.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll7;
                @Scroll8.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll8;
                @Scroll8.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll8;
                @Scroll8.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll8;
                @Scroll9.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll9;
                @Scroll9.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll9;
                @Scroll9.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScroll9;
                @MouseScroll.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseScroll;
                @MouseScroll.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseScroll;
                @MouseScroll.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMouseScroll;
            }
            m_Wrapper.m_PlayerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Movement.started += instance.OnMovement;
                @Movement.performed += instance.OnMovement;
                @Movement.canceled += instance.OnMovement;
                @Jump.started += instance.OnJump;
                @Jump.performed += instance.OnJump;
                @Jump.canceled += instance.OnJump;
                @PrimaryAction.started += instance.OnPrimaryAction;
                @PrimaryAction.performed += instance.OnPrimaryAction;
                @PrimaryAction.canceled += instance.OnPrimaryAction;
                @SecondaryAction.started += instance.OnSecondaryAction;
                @SecondaryAction.performed += instance.OnSecondaryAction;
                @SecondaryAction.canceled += instance.OnSecondaryAction;
                @Interact.started += instance.OnInteract;
                @Interact.performed += instance.OnInteract;
                @Interact.canceled += instance.OnInteract;
                @MouseLook.started += instance.OnMouseLook;
                @MouseLook.performed += instance.OnMouseLook;
                @MouseLook.canceled += instance.OnMouseLook;
                @ToggleGravity.started += instance.OnToggleGravity;
                @ToggleGravity.performed += instance.OnToggleGravity;
                @ToggleGravity.canceled += instance.OnToggleGravity;
                @PrefabRead.started += instance.OnPrefabRead;
                @PrefabRead.performed += instance.OnPrefabRead;
                @PrefabRead.canceled += instance.OnPrefabRead;
                @PrefabReadAir.started += instance.OnPrefabReadAir;
                @PrefabReadAir.performed += instance.OnPrefabReadAir;
                @PrefabReadAir.canceled += instance.OnPrefabReadAir;
                @ToggleFreeCam.started += instance.OnToggleFreeCam;
                @ToggleFreeCam.performed += instance.OnToggleFreeCam;
                @ToggleFreeCam.canceled += instance.OnToggleFreeCam;
                @Shifting.started += instance.OnShifting;
                @Shifting.performed += instance.OnShifting;
                @Shifting.canceled += instance.OnShifting;
                @DebugKey.started += instance.OnDebugKey;
                @DebugKey.performed += instance.OnDebugKey;
                @DebugKey.canceled += instance.OnDebugKey;
                @ToggleHUD.started += instance.OnToggleHUD;
                @ToggleHUD.performed += instance.OnToggleHUD;
                @ToggleHUD.canceled += instance.OnToggleHUD;
                @Scroll1.started += instance.OnScroll1;
                @Scroll1.performed += instance.OnScroll1;
                @Scroll1.canceled += instance.OnScroll1;
                @Scroll2.started += instance.OnScroll2;
                @Scroll2.performed += instance.OnScroll2;
                @Scroll2.canceled += instance.OnScroll2;
                @Scroll3.started += instance.OnScroll3;
                @Scroll3.performed += instance.OnScroll3;
                @Scroll3.canceled += instance.OnScroll3;
                @Scroll4.started += instance.OnScroll4;
                @Scroll4.performed += instance.OnScroll4;
                @Scroll4.canceled += instance.OnScroll4;
                @Scroll5.started += instance.OnScroll5;
                @Scroll5.performed += instance.OnScroll5;
                @Scroll5.canceled += instance.OnScroll5;
                @Scroll6.started += instance.OnScroll6;
                @Scroll6.performed += instance.OnScroll6;
                @Scroll6.canceled += instance.OnScroll6;
                @Scroll7.started += instance.OnScroll7;
                @Scroll7.performed += instance.OnScroll7;
                @Scroll7.canceled += instance.OnScroll7;
                @Scroll8.started += instance.OnScroll8;
                @Scroll8.performed += instance.OnScroll8;
                @Scroll8.canceled += instance.OnScroll8;
                @Scroll9.started += instance.OnScroll9;
                @Scroll9.performed += instance.OnScroll9;
                @Scroll9.canceled += instance.OnScroll9;
                @MouseScroll.started += instance.OnMouseScroll;
                @MouseScroll.performed += instance.OnMouseScroll;
                @MouseScroll.canceled += instance.OnMouseScroll;
            }
        }
    }
    public PlayerActions @Player => new PlayerActions(this);
    private int m_KeyboardandMouseSchemeIndex = -1;
    public InputControlScheme KeyboardandMouseScheme
    {
        get
        {
            if (m_KeyboardandMouseSchemeIndex == -1) m_KeyboardandMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard and Mouse");
            return asset.controlSchemes[m_KeyboardandMouseSchemeIndex];
        }
    }
    public interface IPlayerActions
    {
        void OnMovement(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnPrimaryAction(InputAction.CallbackContext context);
        void OnSecondaryAction(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
        void OnMouseLook(InputAction.CallbackContext context);
        void OnToggleGravity(InputAction.CallbackContext context);
        void OnPrefabRead(InputAction.CallbackContext context);
        void OnPrefabReadAir(InputAction.CallbackContext context);
        void OnToggleFreeCam(InputAction.CallbackContext context);
        void OnShifting(InputAction.CallbackContext context);
        void OnDebugKey(InputAction.CallbackContext context);
        void OnToggleHUD(InputAction.CallbackContext context);
        void OnScroll1(InputAction.CallbackContext context);
        void OnScroll2(InputAction.CallbackContext context);
        void OnScroll3(InputAction.CallbackContext context);
        void OnScroll4(InputAction.CallbackContext context);
        void OnScroll5(InputAction.CallbackContext context);
        void OnScroll6(InputAction.CallbackContext context);
        void OnScroll7(InputAction.CallbackContext context);
        void OnScroll8(InputAction.CallbackContext context);
        void OnScroll9(InputAction.CallbackContext context);
        void OnMouseScroll(InputAction.CallbackContext context);
    }
}
