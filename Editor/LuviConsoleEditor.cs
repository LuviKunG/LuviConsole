using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace LuviKunG.Console.Editor
{
    [CustomEditor(typeof(LuviConsole))]
    public sealed class LuviConsoleEditor : UnityEditor.Editor
    {
        private const string LABEL_VERSION = "LuviConsole Version 4.0.0";
        private const string HELP_SWIPE = "On touch screens, swipe right from the left half of the screen to open the console and swipe left to close it. Swipe left from the right half of the screen to open the preview and swipe right to close it.";
        private const string WARNING_NO_BINDING = "{0} has no bindings.";
        private const string VIEW_DATA_KEY_PREFIX = "LuviConsoleEditor.";

        public override VisualElement CreateInspectorGUI()
        {
            LuviConsole console = (LuviConsole)target;
            VisualElement root = new VisualElement();

            Label version = new Label(LABEL_VERSION);
            version.style.unityFontStyleAndWeight = FontStyle.Bold;
            version.style.unityTextAlign = TextAnchor.MiddleCenter;
            version.style.marginTop = 4;
            version.style.marginBottom = 4;
            root.Add(version);

            root.Add(CreatePropertyField("styleSheet"));
            root.Add(CreatePropertyField(nameof(console.logCapacity)));
            root.Add(CreatePropertyField(nameof(console.executeCapacity), "Log Executed Command Capacity"));
            root.Add(CreatePropertyField(nameof(console.defaultFontSize)));
            root.Add(CreatePropertyField(nameof(console.autoShowWarning), "Show Log When Warning"));
            root.Add(CreatePropertyField(nameof(console.autoShowError), "Show Log When Error"));
            root.Add(CreatePropertyField(nameof(console.autoShowException), "Show Log When Exception"));
            root.Add(CreatePropertyField(nameof(console.commandLog), "Log Command"));

            Foldout consoleSettings = CreateFoldout(root, "Console settings");
            consoleSettings.Add(CreateInputActionField(nameof(console.toggleConsoleAction), () => console.toggleConsoleAction));
            consoleSettings.Add(CreatePropertyField(nameof(console.swipeToToggle)));
            consoleSettings.Add(CreatePropertyField(nameof(console.swipeRatio)));
            consoleSettings.Add(new HelpBox(HELP_SWIPE, HelpBoxMessageType.Info));

            Foldout previewSettings = CreateFoldout(root, "Preview settings");
            previewSettings.Add(CreateInputActionField(nameof(console.togglePreviewAction), () => console.togglePreviewAction));
            previewSettings.Add(CreatePropertyField(nameof(console.previewAnchorPosition)));
            previewSettings.Add(CreatePropertyField(nameof(console.previewSize)));

            Foldout screenMessageSettings = CreateFoldout(root, "Screen Message settings");
            screenMessageSettings.Add(CreateInputActionField(nameof(console.toggleScreenMessageAction), () => console.toggleScreenMessageAction));
            screenMessageSettings.Add(CreatePropertyField(nameof(console.showScreenMessageOnAwake)));

            Foldout extendUISettings = CreateFoldout(root, "Extend UI");
            string extendUI = nameof(console.extendUI);
            extendUISettings.Add(CreatePropertyField($"{extendUI}.{nameof(LuviConsole.Extend2D.top)}"));
            extendUISettings.Add(CreatePropertyField($"{extendUI}.{nameof(LuviConsole.Extend2D.bottom)}"));
            extendUISettings.Add(CreatePropertyField($"{extendUI}.{nameof(LuviConsole.Extend2D.left)}"));
            extendUISettings.Add(CreatePropertyField($"{extendUI}.{nameof(LuviConsole.Extend2D.right)}"));

            return root;
        }

        private PropertyField CreatePropertyField(string propertyPath, string label = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            return label == null ? new PropertyField(property) : new PropertyField(property, label);
        }

        private static Foldout CreateFoldout(VisualElement parent, string text)
        {
            Foldout foldout = new Foldout
            {
                text = text,
                value = false,
                viewDataKey = VIEW_DATA_KEY_PREFIX + text,
            };
            parent.Add(foldout);
            return foldout;
        }

        private VisualElement CreateInputActionField(string propertyPath, Func<InputAction> getAction)
        {
            VisualElement container = new VisualElement();
            container.Add(CreatePropertyField(propertyPath));
            HelpBox warning = new HelpBox(string.Format(WARNING_NO_BINDING, ObjectNames.NicifyVariableName(propertyPath)), HelpBoxMessageType.Warning);
            container.Add(warning);
            void Refresh()
            {
                InputAction action = getAction();
                warning.style.display = action != null && action.bindings.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
            Refresh();
            container.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            return container;
        }
    }
}
