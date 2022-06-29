using System.Text;
using UnityEditor;
using UnityEngine;

namespace LuviKunG.Console.Editor
{
    [CustomEditor(typeof(LuviConsole))]
    public class LuviConsoleEditor : UnityEditor.Editor
    {
        private const string LABEL_VERSION = "LuviConsole Version 2.5.2";
        private const string LABEL_LINK = "https://github.com/LuviKunG/LuviConsole";
        private const string LABEL_ASSIGN_KEY = "Please assign key to toggle LuviConsole.";
        private const string LABEL_INFO_SWIPE = "Swipe ratio is disabled in non-target build mobile device.";

        private readonly GUIContent contentLogCapacity = new GUIContent("Log Capacity", "The capacity of list that will show debug log on console window.");
        private readonly GUIContent contentExecuteCapacity = new GUIContent("Log Excuted Command Capacity", "The capacity of excute command that have been excuted.");
        private readonly GUIContent contentSwipeRatio = new GUIContent("Swipe Ratio", "The ratio when you swipe up/down on your screen to toggle LuviDebug. 0 when touched, 1 when swipe entire of");
        private readonly GUIContent contentDefaultFontSize = new GUIContent("Default Font Size", "Default font size of console when it getting start.");
        private readonly GUIContent contentAutoShowWarning = new GUIContent("Show Log When Warning", "Automatically show logs when player is get warning log.");
        private readonly GUIContent contentAutoShowError = new GUIContent("Show Log When Error", "Automatically show logs when player is get error log.");
        private readonly GUIContent contentAutoShowException = new GUIContent("Show Log When Exception", "Automatically show logs when player is get exception log.");
        private readonly GUIContent contentCommandLog = new GUIContent("Log Command", "Log the command after you executed.");
        private readonly GUIContent contentKeys = new GUIContent("Keys", "Keyboard keys to open the console.");

        private LuviConsole console;
        private StringBuilder sb;
        private SerializedProperty logCapacity;
        private SerializedProperty excuteCapacity;
        private SerializedProperty swipeRatio;
        private SerializedProperty defaultFontSize;
        private SerializedProperty autoShowWarning;
        private SerializedProperty autoShowError;
        private SerializedProperty autoShowException;
        private SerializedProperty commandLog;
        private SerializedProperty keys;

        public void OnEnable()
        {
            console = (LuviConsole)target;
            logCapacity = serializedObject.FindProperty(nameof(console.logCapacity));
            excuteCapacity = serializedObject.FindProperty(nameof(console.excuteCapacity));
            swipeRatio = serializedObject.FindProperty(nameof(console.swipeRatio));
            defaultFontSize = serializedObject.FindProperty(nameof(console.defaultFontSize));
            autoShowWarning = serializedObject.FindProperty(nameof(console.autoShowWarning));
            autoShowError = serializedObject.FindProperty(nameof(console.autoShowError));
            autoShowException = serializedObject.FindProperty(nameof(console.autoShowException));
            commandLog = serializedObject.FindProperty(nameof(console.commandLog));
            keys = serializedObject.FindProperty(nameof(console.keys));

            sb = sb ?? new StringBuilder();
            sb.Clear();
            sb.Append(LABEL_VERSION);
            sb.AppendLine();
            sb.Append(LABEL_LINK);
#if UNITY_ANDROID
            sb.AppendLine();
            sb.Append("In your Android device, Swipe up direction in your screen for open LuviConsole. And swipe down for close LuviConsole.");
#elif UNITY_IOS
            sb.AppendLine();
            sb.Append("In your iPhone/iPad/iPod device, Swipe up direction in your screen for open LuviConsole. And swipe down for close LuviConsole.");
#elif UNITY_WEBGL
            sb.AppendLine();
            sb.Append("In your WebGL builds, Use the same assigned button in editor.");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.None, true);
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                logCapacity.intValue = EditorGUILayout.IntField(contentLogCapacity, logCapacity.intValue);
                excuteCapacity.intValue = EditorGUILayout.IntField(contentExecuteCapacity, excuteCapacity.intValue);
#if UNITY_ANDROID || UNITY_IOS
                swipeRatio.floatValue = EditorGUILayout.Slider(contentSwipeRatio, swipeRatio.floatValue, 0.01f, 0.99f);
#else
                EditorGUILayout.HelpBox(LABEL_INFO_SWIPE, MessageType.None, true);
#endif
                defaultFontSize.intValue = EditorGUILayout.IntSlider(contentDefaultFontSize, defaultFontSize.intValue, 8, 64);
                autoShowWarning.boolValue = EditorGUILayout.Toggle(contentAutoShowWarning, autoShowWarning.boolValue);
                autoShowError.boolValue = EditorGUILayout.Toggle(contentAutoShowError, autoShowError.boolValue);
                autoShowException.boolValue = EditorGUILayout.Toggle(contentAutoShowException, autoShowException.boolValue);
                commandLog.boolValue = EditorGUILayout.Toggle(contentCommandLog, commandLog.boolValue);
                EditorGUILayout.PropertyField(keys);
                if (!keys.isArray || keys.arraySize == 0)
                {
                    EditorGUILayout.HelpBox(LABEL_ASSIGN_KEY, MessageType.Error, true);
                }
                if (checkScope.changed)
                {
                    EditorUtility.SetDirty(console);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}