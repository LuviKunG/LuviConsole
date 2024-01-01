using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuviKunG.Console
{
    using Extension;

    [AddComponentMenu("LuviKunG/Luvi Log Preview")]
    [HelpURL("https://github.com/LuviKunG/LuviConsole")]
    public sealed class LuviLogPreview : MonoBehaviour
    {
        private const string DEFAULT_RESOURCE_PATH_PREFAB = "LuviKunG/LuviContext";
        private const string DEFAULT_RESOURCE_PATH_GUISKIN = "LuviConsoleGUI";

        /// <summary>
        /// Default and start font size of this console.
        /// Must be positive number.
        /// Default value is 16.
        /// </summary>
        public int defaultFontSize = 16;

        /// <summary>
        /// This console will show itself when any debug warning is appearing. 
        /// </summary>
        public bool autoShowWarning = false;

        /// <summary>
        /// This console will show itself when any debug error is appearing. 
        /// </summary>
        public bool autoShowError = false;

        /// <summary>
        /// This console will show itself when any debug exception is appearing. 
        /// </summary>
        public bool autoShowException = false;

        /// <summary>
        /// Keys to toggle show/hide of the preview.
        /// This value will available when using in Unity Editor and Unity using Target Build of WebGL.
        /// </summary>
        public KeyCode[] togglePreviewKeys = new KeyCode[] { KeyCode.F2 };

        /// <summary>
        /// Anchor of the preview.
        /// </summary>
        public TextAnchor previewAnchorPosition = TextAnchor.LowerRight;

        /// <summary>
        /// Size of the preview window.
        /// </summary>
        public Vector2 previewSize = new Vector2(128f, 96f);

        /// <summary>
        /// Extend the LuviConsole GUI window.
        /// </summary>
        public Extend2D extendUI = new Extend2D(0);

        private static LuviLogPreview instance;

        /// <summary>
        /// Get the instance of <see cref="LuviLogPreview"/>.
        /// <see cref="LuviLogPreview"/> must be only one instance and being singleton.
        /// </summary>
        public static LuviLogPreview Instance
        {
            get
            {
                if (instance == null)
                {
                    LuviLogPreview prefab = Resources.Load<LuviLogPreview>(DEFAULT_RESOURCE_PATH_PREFAB);
                    if (prefab == null)
                        throw new MissingReferenceException($"Cannot find {nameof(LuviLogPreview)} asset/prefab in resources path {DEFAULT_RESOURCE_PATH_PREFAB}.");
                    instance = Instantiate(prefab);
                    instance.name = prefab.name;
                }
                return instance;
            }
        }

        [SerializeField]
        private GUISkin guiSkin;

        private Rect rectPreviewWindow;
        private bool isShowingPreview;
        private int countLogVerbose;
        private int countLogWarning;
        private int countLogError;
        private int countLogException;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.LogError($"There are 2 or more instance of \'{nameof(LuviLogPreview)}\' on the scene. The latest instance will be destroy.");
                Destroy(gameObject);
                return;
            }
            Initialize();
        }

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += LogReceiveCallback;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= LogReceiveCallback;
        }

        private void Update()
        {
            UpdateInput();
        }

        private void OnGUI()
        {
            GUI.skin = guiSkin;
            Rect safeArea = Screen.safeArea;
            Rect rectScreen = new Rect(safeArea.x, Screen.height - (safeArea.y + safeArea.height), safeArea.width, safeArea.height);
            Rect rectExtendUI = new Rect(rectScreen.x + extendUI.left, rectScreen.y + extendUI.top, rectScreen.width - extendUI.width, rectScreen.height - extendUI.height);
            if (isShowingPreview)
            {
                UpdatePreviewUI(in rectExtendUI);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            LoadGUISkin();
        }
#endif

        /// <summary>
        /// Clear all logs.
        /// </summary>
        public void ClearLogs()
        {
            countLogVerbose = 0;
            countLogWarning = 0;
            countLogError = 0;
            countLogException = 0;
        }

        private void Initialize()
        {
            LoadGUISkin();
            DontDestroyOnLoad(gameObject);
        }

        private void LoadGUISkin()
        {
            guiSkin ??= Resources.Load<GUISkin>(DEFAULT_RESOURCE_PATH_GUISKIN);
            if (guiSkin != null)
            {
                guiSkin.label.fontSize = defaultFontSize;
            }
        }

        private void UpdateInput()
        {
#if UNITY_EDITOR
            if (togglePreviewKeys != null && togglePreviewKeys.Length > 0)
            {
                bool isToggled = true;
                for (int i = 0; i < togglePreviewKeys.Length - 1; ++i)
                {
                    if (!Input.GetKey(togglePreviewKeys[i]))
                    {
                        isToggled = false;
                        return;
                    }
                }
                if (isToggled && Input.GetKeyDown(togglePreviewKeys[^1]))
                {
                    isShowingPreview = !isShowingPreview;
                }
            }
#elif UNITY_ANDROID || UNITY_IOS
            Touch touch0 = Input.GetTouch(0);
            if (touch0.phase == TouchPhase.Began)
            {
                touchSwipePosStart = touch0.position;
            }
            else if (touch0.phase == TouchPhase.Moved)
            {
                touchSwipePosMoving = touch0.position;
                float screenDivision = Screen.width / 2;
                bool isShowPreview = touchSwipePosStart.x > screenDivision;
                Vector3 direction = touchSwipePosMoving - touchSwipePosStart;
                if (Vector3.Distance(touchSwipePosStart, touchSwipePosMoving) > (Screen.height * swipeRatio))
                {
                    if (isShowPreview && Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                    {
                        if (direction.x < 0)
                        {
                            touchSwipePosStart = Vector3.zero;
                            touchSwipePosMoving = Vector3.zero;
                            isShowingPreview = true;
                        }
                        else
                        {
                            touchSwipePosStart = Vector3.zero;
                            touchSwipePosMoving = Vector3.zero;
                            isShowingPreview = false;
                        }
                    }
                }
            }
#endif
        }

        private void UpdatePreviewUI(in Rect rect)
        {
            Rect GetRectByAnchor(in Rect rect, TextAnchor anchor, Vector2 size)
            {
                return anchor switch
                {
                    TextAnchor.UpperLeft => new Rect(rect.x, rect.y, size.x, size.y),
                    TextAnchor.UpperCenter => new Rect(rect.x + (rect.width - size.x) / 2, rect.y, size.x, size.y),
                    TextAnchor.UpperRight => new Rect(rect.x + rect.width - size.x, rect.y, size.x, size.y),
                    TextAnchor.MiddleLeft => new Rect(rect.x, rect.y + (rect.height - size.y) / 2, size.x, size.y),
                    TextAnchor.MiddleCenter => new Rect(rect.x + (rect.width - size.x) / 2, rect.y + (rect.height - size.y) / 2, size.x, size.y),
                    TextAnchor.MiddleRight => new Rect(rect.x + rect.width - size.x, rect.y + (rect.height - size.y) / 2, size.x, size.y),
                    TextAnchor.LowerLeft => new Rect(rect.x, rect.y + rect.height - size.y, size.x, size.y),
                    TextAnchor.LowerCenter => new Rect(rect.x + (rect.width - size.x) / 2, rect.y + rect.height - size.y, size.x, size.y),
                    TextAnchor.LowerRight => new Rect(rect.x + rect.width - size.x, rect.y + rect.height - size.y, size.x, size.y),
                    _ => new Rect(0, 0, size.x, size.y),
                };
            }
            rectPreviewWindow = GetRectByAnchor(in rect, previewAnchorPosition, previewSize);
            using (var areaScope = new GUILayout.AreaScope(rectPreviewWindow, GUIContent.none, guiSkin.customStyles[5]))
            {
                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        string color = $"#{ColorUtility.ToHtmlStringRGB(Color.white)}";
                        GUILayout.Label($"<color={color}>Verbose:</color>", guiSkin.customStyles[6]);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"<color={color}>{countLogVerbose:N0}</color>", guiSkin.customStyles[6]);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        string color = $"#{ColorUtility.ToHtmlStringRGB(Color.yellow)}";
                        GUILayout.Label($"<color={color}>Warning:</color>", guiSkin.customStyles[6]);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"<color={color}>{countLogWarning:N0}</color>", guiSkin.customStyles[6]);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        string color = $"#{ColorUtility.ToHtmlStringRGB(Color.red)}";
                        GUILayout.Label($"<color={color}>Error:</color>", guiSkin.customStyles[6]);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"<color={color}>{countLogError:N0}</color>", guiSkin.customStyles[6]);
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        string color = $"#{ColorUtility.ToHtmlStringRGB(Color.red)}";
                        GUILayout.Label($"<color={color}><b>Exception:</b></color>", guiSkin.customStyles[6]);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"<color={color}><b>{countLogException:N0}</b></color>", guiSkin.customStyles[6]);
                    }
                }
            }
        }

        private void LogReceiveCallback(string message, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    {
                        if (autoShowWarning & !isShowingPreview)
                            isShowingPreview = true;
                        countLogWarning++;
                    }
                    break;
                case LogType.Error:
                    {
                        if (autoShowError & !isShowingPreview)
                            isShowingPreview = true;
                        countLogError++;
                    }
                    break;
                case LogType.Exception:
                    {
                        if (autoShowException & !isShowingPreview)
                            isShowingPreview = true;
                        countLogException++;
                    }
                    break;
                default:
                    {
                        countLogVerbose++;
                    }
                    break;
            }
        }
    }
}