using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AutoUIInputMapper : MonoBehaviour
{
    public enum VirtualGamepadTarget
    {
        None,
        LeftStick,
        RightStick,
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,
        ButtonSouth, // A / Cross
        ButtonNorth, // Y / Triangle
        ButtonEast,  // B / Circle
        ButtonWest,  // X / Square
        LeftTrigger,
        RightTrigger,
        LeftShoulder,
        RightShoulder
    }

    public enum ButtonBehavior
    {
        Momentary, // Held down = pressed, release = unpressed
        Toggle     // Tap to toggle on/off
    }

    public enum JoystickAxisMode
    {
        FullVector2,
        VerticalOnly,
        HorizontalOnly
    }

    [System.Serializable]
    public class ActionUIMapping
    {
        [HideInInspector] public string actionName;
        [Tooltip("The Input Action you are mapping to.")]
        public InputActionReference actionReference;

        [Tooltip("The Virtual Gamepad control this UI element will drive.")]
        public VirtualGamepadTarget virtualControl = VirtualGamepadTarget.None;

        [Header("UI Controls")]
        public ButtonBehavior buttonBehavior = ButtonBehavior.Momentary;
        public List<Button> buttons = new List<Button>();

        public JoystickAxisMode axisMode = JoystickAxisMode.FullVector2;
        public bool invertJoystick = false;
        public List<Joystick> joysticks = new List<Joystick>();

        [NonSerialized] internal bool isToggledOn = false;
    }

    [Header("Input Asset Sources")]
    [Tooltip("Drop your .inputactions asset files here to auto-populate.")]
    public List<InputActionAsset> inputActionAssets = new List<InputActionAsset>();

    [Header("Mappings")]
    public List<ActionUIMapping> mappings = new List<ActionUIMapping>();

    // The runtime virtual device
    private Gamepad virtualGamepad;

    private void Awake()
    {
        // Create an in-memory virtual gamepad isolated to this session/scene
        virtualGamepad = InputSystem.AddDevice<Gamepad>("VirtualOnScreenGamepad");
    }

    private void OnDestroy()
    {
        // Clean up immediately when leaving this scene so PC controls remain completely untouched
        if (virtualGamepad != null && virtualGamepad.added)
        {
            InputSystem.RemoveDevice(virtualGamepad);
            virtualGamepad = null;
        }
    }

    private void Start()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        for (int i = 0; i < mappings.Count; i++)
        {
            ActionUIMapping map = mappings[i];
            if (map.buttons == null || map.buttons.Count == 0) continue;

            foreach (Button btn in map.buttons)
            {
                if (btn == null) continue;

                if (map.buttonBehavior == ButtonBehavior.Toggle)
                {
                    btn.onClick.AddListener(() =>
                    {
                        map.isToggledOn = !map.isToggledOn;
                        SetButtonState(map.virtualControl, map.isToggledOn);
                    });
                }
                else
                {
                    EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

                    EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                    down.callback.AddListener((_) => SetButtonState(map.virtualControl, true));
                    trigger.triggers.Add(down);

                    EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                    up.callback.AddListener((_) => SetButtonState(map.virtualControl, false));
                    trigger.triggers.Add(up);
                }
            }
        }
    }

    private void Update()
    {
        if (virtualGamepad == null) return;

        // Process joysticks every frame
        for (int i = 0; i < mappings.Count; i++)
        {
            ActionUIMapping map = mappings[i];
            if (map.joysticks == null || map.joysticks.Count == 0) continue;

            for (int j = 0; j < map.joysticks.Count; j++)
            {
                Joystick joy = map.joysticks[j];
                if (joy == null) continue;

                Vector2 dir = joy.Direction;
                if (map.invertJoystick) dir = -dir;

                if (map.axisMode == JoystickAxisMode.VerticalOnly) dir.x = 0f;
                else if (map.axisMode == JoystickAxisMode.HorizontalOnly) dir.y = 0f;

                if (map.virtualControl == VirtualGamepadTarget.LeftStick)
                {
                    InputSystem.QueueStateEvent(virtualGamepad, new GamepadState { leftStick = dir });
                }
                else if (map.virtualControl == VirtualGamepadTarget.RightStick)
                {
                    InputSystem.QueueStateEvent(virtualGamepad, new GamepadState { rightStick = dir });
                }
            }
        }
    }

    private void SetButtonState(VirtualGamepadTarget target, bool pressed)
    {
        if (virtualGamepad == null) return;

        float val = pressed ? 1f : 0f;

        switch (target)
        {
            case VirtualGamepadTarget.ButtonSouth:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.buttonSouth, val);
                break;
            case VirtualGamepadTarget.ButtonNorth:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.buttonNorth, val);
                break;
            case VirtualGamepadTarget.ButtonEast:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.buttonEast, val);
                break;
            case VirtualGamepadTarget.ButtonWest:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.buttonWest, val);
                break;
            case VirtualGamepadTarget.LeftTrigger:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.leftTrigger, val);
                break;
            case VirtualGamepadTarget.RightTrigger:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.rightTrigger, val);
                break;
            case VirtualGamepadTarget.LeftShoulder:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.leftShoulder, val);
                break;
            case VirtualGamepadTarget.RightShoulder:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.rightShoulder, val);
                break;
            case VirtualGamepadTarget.DpadUp:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.dpad.up, val);
                break;
            case VirtualGamepadTarget.DpadDown:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.dpad.down, val);
                break;
            case VirtualGamepadTarget.DpadLeft:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.dpad.left, val);
                break;
            case VirtualGamepadTarget.DpadRight:
                InputSystem.QueueDeltaStateEvent(virtualGamepad.dpad.right, val);
                break;
        }

        InputSystem.Update();
    }
}

// -------------------------------------------------------------
// INSPECTOR: Clean & Direct Slot Mapping
// -------------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(AutoUIInputMapper))]
public class AutoUIInputMapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AutoUIInputMapper mapper = (AutoUIInputMapper)target;

        serializedObject.Update();

        SerializedProperty assetsProp = serializedObject.FindProperty("inputActionAssets");
        EditorGUILayout.PropertyField(assetsProp, new GUIContent("Input Action Assets"), true);

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Populate Actions", GUILayout.Height(30)))
        {
            PopulateActions(mapper);
        }

        EditorGUILayout.Space(10);

        SerializedProperty mappingsProp = serializedObject.FindProperty("mappings");

        if (mappingsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No mappings yet. Assign your InputActionAssets above and click 'Populate Actions'.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < mappingsProp.arraySize; i++)
            {
                SerializedProperty elem = mappingsProp.GetArrayElementAtIndex(i);
                SerializedProperty actionRef = elem.FindPropertyRelative("actionReference");
                SerializedProperty vControl = elem.FindPropertyRelative("virtualControl");

                string label = actionRef.objectReferenceValue != null ? actionRef.objectReferenceValue.name : $"Mapping [{i}]";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    mappingsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(actionRef, new GUIContent("Action Reference"));
                EditorGUILayout.PropertyField(vControl, new GUIContent("Virtual Gamepad Slot"));

                AutoUIInputMapper.VirtualGamepadTarget target = (AutoUIInputMapper.VirtualGamepadTarget)vControl.enumValueIndex;

                if (target == AutoUIInputMapper.VirtualGamepadTarget.LeftStick || target == AutoUIInputMapper.VirtualGamepadTarget.RightStick)
                {
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("axisMode"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("invertJoystick"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("joysticks"), new GUIContent("UI Joysticks"), true);
                }
                else if (target != AutoUIInputMapper.VirtualGamepadTarget.None)
                {
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("buttonBehavior"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("buttons"), new GUIContent("UI Buttons"), true);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void PopulateActions(AutoUIInputMapper mapper)
    {
        if (mapper.inputActionAssets == null || mapper.inputActionAssets.Count == 0)
        {
            EditorUtility.DisplayDialog("Notice", "Assign your InputActionAsset files first.", "OK");
            return;
        }

        Undo.RecordObject(mapper, "Populate Actions");

        var cache = new Dictionary<string, AutoUIInputMapper.ActionUIMapping>();
        if (mapper.mappings != null)
        {
            foreach (var m in mapper.mappings)
            {
                if (!string.IsNullOrEmpty(m.actionName)) cache[m.actionName] = m;
            }
        }

        mapper.mappings = new List<AutoUIInputMapper.ActionUIMapping>();

        foreach (InputActionAsset asset in mapper.inputActionAssets)
        {
            if (asset == null) continue;

            foreach (InputActionMap map in asset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    string key = $"{asset.name}/{map.name}/{action.name}";

                    if (cache.TryGetValue(key, out var existing))
                    {
                        mapper.mappings.Add(existing);
                    }
                    else
                    {
                        var newMapping = new AutoUIInputMapper.ActionUIMapping
                        {
                            actionName = key,
                            actionReference = InputActionReference.Create(action)
                        };

                        // Smart default slot detection
                        string lower = action.name.ToLower();
                        if (lower.Contains("left") && (lower.Contains("stick") || lower.Contains("track")))
                            newMapping.virtualControl = AutoUIInputMapper.VirtualGamepadTarget.LeftStick;
                        else if (lower.Contains("right") && (lower.Contains("stick") || lower.Contains("track")))
                            newMapping.virtualControl = AutoUIInputMapper.VirtualGamepadTarget.RightStick;
                        else if (lower.Contains("climb"))
                            newMapping.virtualControl = AutoUIInputMapper.VirtualGamepadTarget.LeftStick;
                        else
                            newMapping.virtualControl = AutoUIInputMapper.VirtualGamepadTarget.ButtonSouth;

                        mapper.mappings.Add(newMapping);
                    }
                }
            }
        }

        EditorUtility.SetDirty(mapper);
    }
}
#endif