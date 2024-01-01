using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LuviKunG.Console.Editor
{
    [CustomEditor(typeof(LuviConsole))]
    public sealed class LuviConsoleEditor : UnityEditor.Editor
    {
        private const string LABEL_VERSION = "LuviConsole Version 3.0.0";
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
        private readonly GUIContent contentToggleConsoleKeys = new GUIContent("Toggle Console Keys", "Keys to toggle open and close of the console.");
        //private readonly GUIContent contentTogglePreviewKeys = new GUIContent("Toggle Preview Keys", "Keys to toggle open and close of the preview.");
        //private readonly GUIContent contentPreviewAnchorPosition = new GUIContent("Preview Anchor Position", "Define anchor position of preview window.");
        //private readonly GUIContent contentPreviewSize = new GUIContent("Preview Size", "Define size of preview window.");
        private readonly GUIContent contentSettingsConsole = new GUIContent("Console settings", "Settings of console.");
        private readonly GUIContent contentSettingsPreview = new GUIContent("Preview settings", "Settings of preview.");
        private readonly GUIContent contentExtendUI = new GUIContent("Extend UI", "Control extend size of UI of the GUI.");

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
        private SerializedProperty toggleConsoleKeys;
        //private SerializedProperty togglePreviewKeys;
        //private SerializedProperty previewAnchorPosition;
        //private SerializedProperty previewSize;
        private SerializedProperty extendUI;
        private SerializedProperty extendUITop;
        private SerializedProperty extendUIBottom;
        private SerializedProperty extendUILeft;
        private SerializedProperty extendUIRight;

        private ReorderableList reorderListConsoleKeys;
        //private ReorderableList reorderListPreviewKeys;

        private bool isShowSettingConsole;
        // private bool isShowSettingPreview;
        private bool isShowExtendUI;

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
            toggleConsoleKeys = serializedObject.FindProperty(nameof(console.toggleConsoleKeys));
            //togglePreviewKeys = serializedObject.FindProperty(nameof(console.togglePreviewKeys));
            //previewAnchorPosition = serializedObject.FindProperty(nameof(console.previewAnchorPosition));
            //previewSize = serializedObject.FindProperty(nameof(console.previewSize));
            extendUI = serializedObject.FindProperty(nameof(console.extendUI));
            extendUITop = extendUI.FindPropertyRelative(nameof(Extend2D.top));
            extendUIBottom = extendUI.FindPropertyRelative(nameof(Extend2D.bottom));
            extendUILeft = extendUI.FindPropertyRelative(nameof(Extend2D.left));
            extendUIRight = extendUI.FindPropertyRelative(nameof(Extend2D.right));

            if (reorderListConsoleKeys == null)
            {
                reorderListConsoleKeys = new ReorderableList(serializedObject, toggleConsoleKeys, true, true, true, true);
                reorderListConsoleKeys.drawHeaderCallback = (rect) =>
                {
                    GUI.Label(rect, contentToggleConsoleKeys);
                };
                reorderListConsoleKeys.elementHeightCallback = (index) =>
                {
                    return EditorGUIUtility.singleLineHeight;
                };
                reorderListConsoleKeys.drawElementCallback = (rect, index, isActive, isFocus) =>
                {
                    var property = reorderListConsoleKeys.serializedProperty;
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
                reorderListConsoleKeys.drawNoneElementCallback = (rect) =>
                {
                    EditorGUI.HelpBox(rect, WARNING_ASSIGN_KEY, MessageType.Error);
                };
                reorderListConsoleKeys.onReorderCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    property.serializedObject.ApplyModifiedProperties();
                };
                reorderListConsoleKeys.onAddCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    property.InsertArrayElementAtIndex(property.arraySize);
                    var sp = property.GetArrayElementAtIndex(property.arraySize - 1);
                    sp.intValue = (int)KeyCode.None;
                    property.serializedObject.ApplyModifiedProperties();
                };
                reorderListConsoleKeys.onRemoveCallback = (list) =>
                {
                    var property = list.serializedProperty;
                    var index = list.selectedIndices.Count > 0 ? list.selectedIndices[0] : property.arraySize - 1;
                    property.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                };
            }
            //if (reorderListPreviewKeys == null)
            //{
            //    reorderListPreviewKeys = new ReorderableList(serializedObject, togglePreviewKeys, true, true, true, true);
            //    reorderListPreviewKeys.drawHeaderCallback = (rect) =>
            //    {
            //        GUI.Label(rect, contentTogglePreviewKeys);
            //    };
            //    reorderListPreviewKeys.elementHeightCallback = (index) =>
            //    {
            //        return EditorGUIUtility.singleLineHeight;
            //    };
            //    reorderListPreviewKeys.drawElementCallback = (rect, index, isActive, isFocus) =>
            //    {
            //        var property = reorderListPreviewKeys.serializedProperty;
            //        var sp = property.GetArrayElementAtIndex(index);
            //        using (var changeScope = new EditorGUI.ChangeCheckScope())
            //        {
            //            sp.intValue = (int)(KeyCode)EditorGUI.EnumPopup(rect, (KeyCode)sp.intValue);
            //            if (changeScope.changed)
            //            {
            //                property.serializedObject.ApplyModifiedProperties();
            //            }
            //        }
            //    };
            //    reorderListPreviewKeys.drawNoneElementCallback = (rect) =>
            //    {
            //        EditorGUI.HelpBox(rect, WARNING_ASSIGN_KEY, MessageType.Error);
            //    };
            //    reorderListPreviewKeys.onReorderCallback = (list) =>
            //    {
            //        var property = list.serializedProperty;
            //        property.serializedObject.ApplyModifiedProperties();
            //    };
            //    reorderListPreviewKeys.onAddCallback = (list) =>
            //    {
            //        var property = list.serializedProperty;
            //        property.InsertArrayElementAtIndex(property.arraySize);
            //        var sp = property.GetArrayElementAtIndex(property.arraySize - 1);
            //        sp.intValue = (int)KeyCode.None;
            //        property.serializedObject.ApplyModifiedProperties();
            //    };
            //    reorderListPreviewKeys.onRemoveCallback = (list) =>
            //    {
            //        var property = list.serializedProperty;
            //        var index = list.selectedIndices.Count > 0 ? list.selectedIndices[0] : property.arraySize - 1;
            //        property.DeleteArrayElementAtIndex(index);
            //        property.serializedObject.ApplyModifiedProperties();
            //    };
            //}
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
                isShowSettingConsole = EditorGUILayout.Foldout(isShowSettingConsole, contentSettingsConsole);
                if (isShowSettingConsole)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {

                        reorderListConsoleKeys.DoLayoutList();
                        using (new EditorGUILayout.VerticalScope())
                        {
                            bool wasContainNone = false;
                            bool wasSameValue = false;
                            for (int i = 0; i < toggleConsoleKeys.arraySize; ++i)
                            {
                                var sp = toggleConsoleKeys.GetArrayElementAtIndex(i);
                                if (sp.intValue == (int)KeyCode.None)
                                {
                                    wasContainNone = true;
                                    break;
                                }
                                for (int j = 0; j < toggleConsoleKeys.arraySize; ++j)
                                {
                                    if (i == j)
                                        continue;
                                    var spr = toggleConsoleKeys.GetArrayElementAtIndex(j);
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
                    }
                }
                // isShowSettingPreview = EditorGUILayout.Foldout(isShowSettingPreview, contentSettingsPreview);
                // if (isShowSettingPreview)
                // {
                //    using (new EditorGUI.IndentLevelScope())
                //    {
                //        reorderListPreviewKeys.DoLayoutList();
                //        using (new EditorGUILayout.VerticalScope())
                //        {
                //            bool wasContainNone = false;
                //            bool wasSameValue = false;
                //            for (int i = 0; i < togglePreviewKeys.arraySize; ++i)
                //            {
                //                var sp = togglePreviewKeys.GetArrayElementAtIndex(i);
                //                if (sp.intValue == (int)KeyCode.None)
                //                {
                //                    wasContainNone = true;
                //                    break;
                //                }
                //                for (int j = 0; j < togglePreviewKeys.arraySize; ++j)
                //                {
                //                    if (i == j)
                //                        continue;
                //                    var spr = togglePreviewKeys.GetArrayElementAtIndex(j);
                //                    if (sp.intValue == spr.intValue)
                //                    {
                //                        wasSameValue = true;
                //                        break;
                //                    }
                //                }
                //                if (wasSameValue)
                //                    break;
                //            }
                //            if (wasContainNone)
                //            {
                //                EditorGUILayout.HelpBox($"Keys cannot contains key code of none.", MessageType.Error, true);
                //            }
                //            if (wasSameValue)
                //            {
                //                EditorGUILayout.HelpBox($"Keys should not have the same multiple value.", MessageType.Error, true);
                //            }
                //        }
                //        previewAnchorPosition.intValue = (int)(TextAnchor)EditorGUILayout.EnumPopup(contentPreviewAnchorPosition, (TextAnchor)previewAnchorPosition.intValue);
                //        previewSize.vector2Value = EditorGUILayout.Vector2Field(contentPreviewSize, previewSize.vector2Value);
                //    }
                // }
                isShowExtendUI = EditorGUILayout.Foldout(isShowExtendUI, contentExtendUI);
                if (isShowExtendUI)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            extendUITop.floatValue = EditorGUILayout.FloatField("Top", extendUITop.floatValue);
                            extendUIBottom.floatValue = EditorGUILayout.FloatField("Bottom", extendUIBottom.floatValue);
                            extendUILeft.floatValue = EditorGUILayout.FloatField("Left", extendUILeft.floatValue);
                            extendUIRight.floatValue = EditorGUILayout.FloatField("Right", extendUIRight.floatValue);
                        }
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