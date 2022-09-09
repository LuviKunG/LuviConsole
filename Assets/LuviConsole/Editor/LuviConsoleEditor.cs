using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LuviKunG.Console.Editor
{
    [CustomEditor(typeof(LuviConsole))]
    public sealed class LuviConsoleEditor : UnityEditor.Editor
    {
        private const string LABEL_VERSION = "LuviConsole Version 2.7.0";
        private const string WARNING_ASSIGN_KEY = "Please assign key.";

        private readonly GUIContent contentLogCapacity = new GUIContent("Log Capacity", "The capacity of list that will show debug log on console window.");
        private readonly GUIContent contentExecuteCapacity = new GUIContent("Log Excuted Command Capacity", "The capacity of excute command that have been excuted.");
#if UNITY_ANDROID || UNITY_IOS
        private readonly GUIContent contentSwipeRatio = new GUIContent("Swipe Ratio", "The ratio when you swipe up/down on your screen to toggle LuviDebug. 0 when touched, 1 when swipe entire of");
#endif
        private readonly GUIContent contentDefaultFontSize = new GUIContent("Default Font Size", "Default font size of console when it getting start.");
        private readonly GUIContent contentAutoShowWarning = new GUIContent("Show Log When Warning", "Automatically show logs when player is get warning log.");
        private readonly GUIContent contentAutoShowError = new GUIContent("Show Log When Error", "Automatically show logs when player is get error log.");
        private readonly GUIContent contentAutoShowException = new GUIContent("Show Log When Exception", "Automatically show logs when player is get exception log.");
        private readonly GUIContent contentCommandLog = new GUIContent("Log Command", "Log the command after you executed.");
        private readonly GUIContent contentKeys = new GUIContent("Keys", "Keys to toggle open and close of the console.");

        private LuviConsole console;
        private SerializedProperty logCapacity;
        private SerializedProperty excuteCapacity;
#if UNITY_ANDROID || UNITY_IOS
        private SerializedProperty swipeRatio;
#endif
        private SerializedProperty defaultFontSize;
        private SerializedProperty autoShowWarning;
        private SerializedProperty autoShowError;
        private SerializedProperty autoShowException;
        private SerializedProperty commandLog;
        private SerializedProperty keys;

        private ReorderableList reorderListKey;

#if UNITY_ANDROID || UNITY_IOS
        private GUIStyle miniLabelStyle;
#endif

        public void OnEnable()
        {
            console = (LuviConsole)target;
            logCapacity = serializedObject.FindProperty(nameof(console.logCapacity));
            excuteCapacity = serializedObject.FindProperty(nameof(console.excuteCapacity));
#if UNITY_ANDROID || UNITY_IOS
            swipeRatio = serializedObject.FindProperty(nameof(console.swipeRatio));
#endif
            defaultFontSize = serializedObject.FindProperty(nameof(console.defaultFontSize));
            autoShowWarning = serializedObject.FindProperty(nameof(console.autoShowWarning));
            autoShowError = serializedObject.FindProperty(nameof(console.autoShowError));
            autoShowException = serializedObject.FindProperty(nameof(console.autoShowException));
            commandLog = serializedObject.FindProperty(nameof(console.commandLog));
            keys = serializedObject.FindProperty(nameof(console.keys));

            if (reorderListKey == null)
            {
                reorderListKey = new ReorderableList(serializedObject, keys, true, true, true, true);
                reorderListKey.drawHeaderCallback = (rect) =>
                {
                    GUI.Label(rect, contentKeys);
                };
                reorderListKey.elementHeightCallback = (index) =>
                {
                    return EditorGUIUtility.singleLineHeight;
                };
                reorderListKey.drawElementCallback = (rect, index, isActive, isFocus) =>
                {
                    var property = reorderListKey.serializedProperty;
                    var sp = property.GetArrayElementAtIndex(index);
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        sp.intValue = (int)(KeyCode)EditorGUI.EnumPopup(rect, (KeyCode)sp.intValue);
                        if (changeScope.changed)
                        {
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                };
                reorderListKey.drawNoneElementCallback = (rect) =>
                {
                    EditorGUI.HelpBox(rect, WARNING_ASSIGN_KEY, MessageType.Error);
                };
                reorderListKey.onReorderCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    property.serializedObject.ApplyModifiedProperties();
                };
                reorderListKey.onAddCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    property.InsertArrayElementAtIndex(property.arraySize);
                    var sp = property.GetArrayElementAtIndex(property.arraySize - 1);
                    sp.intValue = (int)KeyCode.None;
                    property.serializedObject.ApplyModifiedProperties();
                };
                reorderListKey.onRemoveCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    var index = list.selectedIndices.Count > 0 ? list.selectedIndices[0] : property.arraySize - 1;
                    property.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                };
            }
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LABEL_VERSION, EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                }
#if UNITY_ANDROID || UNITY_IOS
                if (miniLabelStyle == null)
                {
                    miniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                    miniLabelStyle.wordWrap = true;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
#if UNITY_ANDROID
                    GUILayout.Label("In your Android device, Swipe up direction in your screen for open LuviConsole. And swipe down for close LuviConsole.", miniLabelStyle);
#elif UNITY_IOS
                    GUILayout.Label("In your iPhone/iPad/iPod device, Swipe up direction in your screen for open LuviConsole. And swipe down for close LuviConsole.", miniLabelStyle);
#endif
                    GUILayout.FlexibleSpace();
                }
#endif
            }
            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                logCapacity.intValue = EditorGUILayout.IntField(contentLogCapacity, logCapacity.intValue);
                excuteCapacity.intValue = EditorGUILayout.IntField(contentExecuteCapacity, excuteCapacity.intValue);
#if UNITY_ANDROID || UNITY_IOS
                swipeRatio.floatValue = EditorGUILayout.Slider(contentSwipeRatio, swipeRatio.floatValue, 0.01f, 0.99f);
#endif
                defaultFontSize.intValue = EditorGUILayout.IntSlider(contentDefaultFontSize, defaultFontSize.intValue, 8, 64);
                autoShowWarning.boolValue = EditorGUILayout.Toggle(contentAutoShowWarning, autoShowWarning.boolValue);
                autoShowError.boolValue = EditorGUILayout.Toggle(contentAutoShowError, autoShowError.boolValue);
                autoShowException.boolValue = EditorGUILayout.Toggle(contentAutoShowException, autoShowException.boolValue);
                commandLog.boolValue = EditorGUILayout.Toggle(contentCommandLog, commandLog.boolValue);
                reorderListKey.DoLayoutList();
                using (new EditorGUILayout.VerticalScope())
                {
                    bool wasContainNone = false;
                    bool wasSameValue = false;
                    for (int i = 0; i < keys.arraySize; ++i)
                    {
                        var sp = keys.GetArrayElementAtIndex(i);
                        if (sp.intValue == (int)KeyCode.None)
                        {
                            wasContainNone = true;
                            break;
                        }
                        for (int j = 0; j < keys.arraySize; ++j)
                        {
                            if (i == j)
                                continue;
                            var spr = keys.GetArrayElementAtIndex(j);
                            if (sp.intValue == spr.intValue)
                            {
                                wasSameValue = true;
                                break;
                            }
                        }
                        if (wasSameValue)
                            break;
                    }
                    if (wasContainNone)
                    {
                        EditorGUILayout.HelpBox($"Keys cannot contains key code of none.", MessageType.Error, true);
                    }
                    if (wasSameValue)
                    {
                        EditorGUILayout.HelpBox($"Keys should not have the same multiple value.", MessageType.Error, true);
                    }
                }
                if (checkScope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}