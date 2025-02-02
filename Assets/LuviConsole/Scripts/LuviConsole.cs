using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuviKunG.Console
{
    using Extension;

    [AddComponentMenu("LuviKunG/LuviConsole")]
    [HelpURL("https://github.com/LuviKunG/LuviConsole")]
    public sealed class LuviConsole : MonoBehaviour
    {
        // Shared (main–thread) StringBuilder instance.
        private static readonly StringBuilder str = new();

        #region Nested Types

        private readonly struct CommandPreset
        {
            public readonly string preset;
            public readonly string name;
            public readonly string description;
            public readonly string group;
            public readonly bool executeImmediately;

            public CommandPreset(string preset, string name, string description, string group, bool executeImmediately)
            {
                this.preset = preset;
                this.name = name;
                this.description = description;
                this.group = group;
                this.executeImmediately = executeImmediately;
            }
        }

        private struct ScreenMessage
        {
            // Note: using a static builder here is OK if you know only one message is formatted at a time.
            private static readonly StringBuilder str = new();

            public readonly string message;
            public float duration;
            public readonly Color? color;
            public readonly bool bold;
            public readonly bool italic;

            public ScreenMessage(string message, float duration)
            {
                this.message = message;
                this.duration = duration;
                this.color = null;
                this.bold = false;
                this.italic = false;
            }

            public ScreenMessage(string message, float duration, bool bold, bool italic)
            {
                this.message = message;
                this.duration = duration;
                this.color = null;
                this.bold = bold;
                this.italic = italic;
            }

            public ScreenMessage(string message, float duration, Color color)
            {
                this.message = message;
                this.duration = duration;
                this.color = color;
                this.bold = false;
                this.italic = false;
            }

            public ScreenMessage(string message, float duration, Color color, bool bold, bool italic)
            {
                this.message = message;
                this.duration = duration;
                this.color = color;
                this.bold = bold;
                this.italic = italic;
            }

            public override string ToString()
            {
                str.Clear();
                str.Append(message);
                if (color.HasValue)
                    str.Color(color.Value);
                if (bold)
                    str.Bold();
                if (italic)
                    str.Italic();
                return str.ToString();
            }
        }

        [Serializable]
        public struct Extend2D
        {
            public float top;
            public float bottom;
            public float left;
            public float right;

            public float width => left + right;
            public float height => top + bottom;

            public Extend2D(float top, float bottom, float left, float right)
            {
                this.top = top;
                this.bottom = bottom;
                this.left = left;
                this.right = right;
            }

            public Extend2D(float vertical, float horizontal)
            {
                this.top = vertical;
                this.bottom = vertical;
                this.left = horizontal;
                this.right = horizontal;
            }

            public Extend2D(float all)
            {
                this.top = all;
                this.bottom = all;
                this.left = all;
                this.right = all;
            }

            public static Extend2D operator +(Extend2D a, Extend2D b) =>
                new Extend2D(a.top + b.top, a.bottom + b.bottom, a.left + b.left, a.right + b.right);

            public static Extend2D operator -(Extend2D a, Extend2D b) =>
                new Extend2D(a.top - b.top, a.bottom - b.bottom, a.left - b.left, a.right - b.right);

            public static Extend2D operator *(Extend2D a, Extend2D b) =>
                new Extend2D(a.top * b.top, a.bottom * b.bottom, a.left * b.left, a.right * b.right);

            public static Extend2D operator /(Extend2D a, Extend2D b) =>
                new Extend2D(a.top / b.top, a.bottom / b.bottom, a.left / b.left, a.right / b.right);

            public static Extend2D operator +(Extend2D a, float b) =>
                new Extend2D(a.top + b, a.bottom + b, a.left + b, a.right + b);

            public static Extend2D operator -(Extend2D a, float b) =>
                new Extend2D(a.top - b, a.bottom - b, a.left - b, a.right - b);

            public static Extend2D operator *(Extend2D a, float b) =>
                new Extend2D(a.top * b, a.bottom * b, a.left * b, a.right * b);

            public static Extend2D operator /(Extend2D a, float b) =>
                new Extend2D(a.top / b, a.bottom / b, a.left / b, a.right / b);

            public static bool operator ==(Extend2D a, Extend2D b) =>
                a.top == b.top && a.bottom == b.bottom && a.left == b.left && a.right == b.right;

            public static bool operator !=(Extend2D a, Extend2D b) => !(a == b);

            public override bool Equals(object obj) =>
                obj is Extend2D other && this == other;

            public override int GetHashCode() => HashCode.Combine(top, bottom, left, right);
        }

        #endregion

        #region Constants and Static Fields

        private const string DEFAULT_RESOURCE_PATH_PREFAB = "LuviKunG/LuviConsole";
        private const string DEFAULT_RESOURCE_PATH_GUISKIN = "LuviConsoleGUI";

        private static readonly Color LOG_COLOR_COMMAND = new Color(0.0f, 1.0f, 1.0f, 1.0f); // Cyan
        private static readonly Color LOG_COLOR_WARNING = new Color(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        private static readonly Color LOG_COLOR_ERROR = new Color(1.0f, 0.0f, 0.0f, 1.0f);   // Red

        private static LuviConsole instance;
        public static LuviConsole Instance
        {
            get
            {
                if (instance == null)
                {
                    LuviConsole prefab = Resources.Load<LuviConsole>(DEFAULT_RESOURCE_PATH_PREFAB);
                    if (prefab == null)
                        throw new MissingReferenceException($"Cannot find {nameof(LuviConsole)} asset/prefab in resources path {DEFAULT_RESOURCE_PATH_PREFAB}.");
                    instance = Instantiate(prefab);
                    instance.name = prefab.name;
                }
                return instance;
            }
        }

        #endregion

        #region Inspector Fields

        [SerializeField]
        private GUISkin guiSkin;

        public int logCapacity = 64;
        public int executeCapacity = 16; // renamed from "excuteCapacity"
#if UNITY_ANDROID || UNITY_IOS
        public float swipeRatio = 0.8f;
#endif
        public int defaultFontSize = 16;
        public bool autoShowWarning = false;
        public bool autoShowError = false;
        public bool autoShowException = false;
        public bool commandLog = false;
        public KeyCode[] toggleConsoleKeys = new KeyCode[] { KeyCode.F1 };
        public KeyCode[] togglePreviewKeys = new KeyCode[] { KeyCode.F2 };
        public KeyCode[] toggleScreenMessageKeys = new KeyCode[] { KeyCode.F3 };

        public TextAnchor previewAnchorPosition = TextAnchor.LowerRight;
        public Vector2 previewSize = new Vector2(128f, 96f);
        public bool showScreenMessageOnAwake = true;
        public Extend2D extendUI = new Extend2D(0);

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.LogError($"Multiple instances of {nameof(LuviConsole)} found. Destroying duplicate.");
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
            float deltaTime = Time.deltaTime;
            UpdateInput();
            UpdateScreenMessages(deltaTime);
            if (isShowingConsole)
                UpdateConsoleScrollDrag();
        }

        private void OnGUI()
        {
            const float SCREEN_MESSAGE_PADDING = 10.0f;
            GUI.skin = guiSkin;
            Rect safeArea = Screen.safeArea;
            // Convert safe area to “top‐left” coordinates.
            Rect rectScreen = new Rect(safeArea.x, Screen.height - (safeArea.y + safeArea.height), safeArea.width, safeArea.height);
            Rect rectExtendUI = new Rect(rectScreen.x + extendUI.left, rectScreen.y + extendUI.top,
                                          rectScreen.width - extendUI.width, rectScreen.height - extendUI.height);
            Rect rectScreenMessage = new Rect(rectExtendUI.x + SCREEN_MESSAGE_PADDING, rectExtendUI.y + SCREEN_MESSAGE_PADDING,
                                              rectExtendUI.width - SCREEN_MESSAGE_PADDING, rectExtendUI.height - SCREEN_MESSAGE_PADDING);

            if (isShowingConsole)
                UpdateConsoleUI(rectExtendUI);
            if (isShowingPreview)
                UpdatePreviewUI(rectExtendUI);
            if (isShowingScreenMessage)
                UpdateScreenMessageUI(rectScreenMessage);
        }

#if UNITY_EDITOR
        private void Reset() => LoadGUISkin();
#endif

        #endregion

        #region Initialization and Input

        private void Initialize()
        {
            isShowingScreenMessage = showScreenMessageOnAwake;
            LoadGUISkin();
            DontDestroyOnLoad(gameObject);
        }

        // Helper method to check if a key combo was pressed.
        private bool CheckKeyCombo(KeyCode[] keys)
        {
            if (keys == null || keys.Length == 0)
                return false;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (!Input.GetKey(keys[i]))
                    return false;
            }
            return Input.GetKeyDown(keys[keys.Length - 1]);
        }

        private void UpdateInput()
        {
#if UNITY_EDITOR
            if (CheckKeyCombo(toggleConsoleKeys))
            {
                isShowingConsole = !isShowingConsole;
                GUI.FocusControl("commandfield");
            }
            if (CheckKeyCombo(togglePreviewKeys))
                isShowingPreview = !isShowingPreview;
            if (CheckKeyCombo(toggleScreenMessageKeys))
                isShowingScreenMessage = !isShowingScreenMessage;
#elif UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
            {
                Touch touch0 = Input.GetTouch(0);
                if (touch0.phase == TouchPhase.Began)
                {
                    touchSwipePosStart = touch0.position;
                }
                else if (touch0.phase == TouchPhase.Moved)
                {
                    touchSwipePosMoving = touch0.position;
                    float screenDivision = Screen.width / 2;
                    bool isShowConsole = touchSwipePosStart.x < screenDivision;
                    bool isShowPreview = touchSwipePosStart.x > screenDivision;
                    Vector3 direction = touchSwipePosMoving - touchSwipePosStart;
                    if (Vector3.Distance(touchSwipePosStart, touchSwipePosMoving) > (Screen.height * swipeRatio))
                    {
                        if (isShowConsole && Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                        {
                            isShowingConsole = (direction.x > 0);
                            touchSwipePosStart = touchSwipePosMoving = Vector3.zero;
                        }
                        if (isShowPreview && Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                        {
                            isShowingPreview = (direction.x < 0);
                            touchSwipePosStart = touchSwipePosMoving = Vector3.zero;
                        }
                    }
                }
            }
#endif
        }

        private void LoadGUISkin()
        {
            guiSkin ??= Resources.Load<GUISkin>(DEFAULT_RESOURCE_PATH_GUISKIN);
            if (guiSkin != null)
                guiSkin.label.fontSize = defaultFontSize;
        }

        #endregion

        #region UI Helpers and Scrolling

        private void UpdateConsoleScrollDrag()
        {
            Vector2 currentPosition = Input.mousePosition;
            currentPosition.y = Screen.height - currentPosition.y; // Convert mouse pos to top–left origin.
            if (Input.GetMouseButtonDown(0) && rectDebugLogScrollDrag.Contains(currentPosition))
            {
                isScrollDebugDragging = true;
                scrollDebugDragPosition = currentPosition;
            }
            if (Input.GetMouseButtonUp(0))
                isScrollDebugDragging = false;
            if (isScrollDebugDragging)
            {
                Vector2 delta = scrollDebugDragPosition - currentPosition;
                scrollDebugPosition += delta;
                scrollDebugDragPosition = currentPosition;
            }
        }

        // These fields are used in UI layout.
        private readonly Vector2 scrollLockPosition = new Vector2(0, Mathf.Infinity);
        private Rect rectDebugLogBackground;
        private Rect rectDebugLogScroll;
        private Rect rectDebugLogScrollDrag;
        private Rect rectDebugButtonClear;
        private Rect rectDebugButtonFontInc;
        private Rect rectDebugButtonFontDesc;
        private Rect rectCommandBackground;
        private Rect rectCommandArea;
        private Rect rectCommandInput;
        private Rect rectCommandHelpBox;
        private Rect rectCommandReturn;
        private Rect rectPreviewWindow;
        private Vector2 scrollDebugPosition = Vector2.zero;
        private Vector2 scrollCommandPosition = Vector2.zero;
        private Vector2 scrollDebugDragPosition = Vector2.zero;
        private Vector2 scrollCommandDragPosition = Vector2.zero;

        #endregion

        #region Internal Data

        private Dictionary<string, LuviCommandExecution> commandData = new Dictionary<string, LuviCommandExecution>();
        private List<CommandPreset> commandPresets = new List<CommandPreset>();
        private List<string> log = new List<string>();
        private List<string> logExecuteCommands = new List<string>();
        private List<ScreenMessage> screenMessages = new List<ScreenMessage>();

        private bool isShowingConsole;
        private bool isShowingPreview;
        private bool isShowingScreenMessage;
        private bool isScrollDebugDragging = false;
        private string commandHelpText = string.Empty;
        private string command = string.Empty;
        private string commandDisplayingGroup = null;
        private int logExecutePosition;
        private int indexConsole;
        private int indexDebug;
        private int countLogVerbose;
        private int countLogWarning;
        private int countLogError;
        private int countLogException;
#if UNITY_ANDROID || UNITY_IOS
        private Vector3 touchSwipePosStart = Vector3.zero;
        private Vector3 touchSwipePosMoving = Vector3.zero;
#endif

        #endregion

        #region Logging Methods

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            log.Add(message);
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        public void LogScreen(string message, float duration)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            ScreenMessage screenMessage = new(message, duration);
            LogScreen(ref screenMessage);
        }

        public void LogScreen(string message, float duration, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            ScreenMessage screenMessage = new(message, duration, bold, italic);
            LogScreen(ref screenMessage);
        }

        public void LogScreen(string message, float duration, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            ScreenMessage screenMessage = new(message, duration, color);
            LogScreen(ref screenMessage);
        }

        public void LogScreen(string message, float duration, Color color, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            ScreenMessage screenMessage = new(message, duration, color, bold, italic);
            LogScreen(ref screenMessage);
        }

        private void LogScreen(ref ScreenMessage message)
        {
            screenMessages.Add(message);
        }

        public void Log(string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            str.Clear();
            str.Append(message);
            str.Color(color);
            log.Add(str.ToString());
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        public void Log(string message, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            str.Clear();
            str.Append(message);
            if (bold)
                str.Bold();
            if (italic)
                str.Italic();
            log.Add(str.ToString());
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        public void Log(string message, Color color, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            str.Clear();
            str.Append(message);
            str.Color(color);
            if (bold)
                str.Bold();
            if (italic)
                str.Italic();
            log.Add(str.ToString());
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        private void LogReceiveCallback(string message, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    str.Clear();
                    str.Append("Warning: ").Append(message);
                    str.Color(LOG_COLOR_WARNING).Italic();
                    Log(str.ToString());
                    if (autoShowWarning && !isShowingConsole)
                        isShowingConsole = true;
                    countLogWarning++;
                    break;
                case LogType.Error:
                    str.Clear();
                    str.Append("Error: ").Append(message);
                    str.Color(LOG_COLOR_ERROR).Bold();
                    Log(str.ToString());
                    if (autoShowError && !isShowingConsole)
                        isShowingConsole = true;
                    countLogError++;
                    break;
                case LogType.Exception:
                    str.Clear();
                    str.Append("Exception: ").Append(message).Bold().AppendLine().Append(stackTrace);
                    str.Color(LOG_COLOR_ERROR);
                    Log(str.ToString());
                    if (autoShowException && !isShowingConsole)
                        isShowingConsole = true;
                    countLogException++;
                    break;
                default:
                    Log(message);
                    countLogVerbose++;
                    break;
            }
        }

        public void ClearLogs()
        {
            log.Clear();
            countLogVerbose = countLogWarning = countLogError = countLogException = 0;
        }

        private void ScrollLogToBottom() => scrollDebugPosition = scrollLockPosition;

        private void UpdateLogCapacity()
        {
            while (log.Count > logCapacity)
                log.RemoveAt(0);
        }

        private void UpdateScreenMessages(float deltaTime)
        {
            // Iterate backward so removals don’t mess up the index.
            for (int i = screenMessages.Count - 1; i >= 0; --i)
            {
                float duration = screenMessages[i].duration - deltaTime;
                if (duration < 0.0f)
                    screenMessages.RemoveAt(i);
                else
                {
                    var msg = screenMessages[i];
                    msg.duration = duration;
                    screenMessages[i] = msg;
                }
            }
        }

        #endregion

        #region Console UI

        private void UpdateConsoleUI(Rect rect)
        {
            // Layout differently for landscape vs. portrait.
            if (rect.width > rect.height)
            {
                // Landscape: split rect vertically.
                Rect rectLeft = new Rect(rect.x, rect.y, rect.width / 2, rect.height);
                Rect rectRight = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height);
                rectDebugLogBackground = new Rect(rectLeft.x, rectLeft.y, rectLeft.width, rectLeft.height - 64);
                rectDebugLogScroll = new Rect(rectLeft.x + 8, rectLeft.y + 24, rectLeft.width - 16, rectLeft.height - 96);
                rectDebugLogScrollDrag = new Rect(rectLeft.x + 8, rectLeft.y + 24, rectLeft.width - 48, rectLeft.height - 96);
                rectDebugButtonClear = new Rect(rectLeft.x, rectLeft.y + rectLeft.height - 64, 128, 64);
                rectDebugButtonFontInc = new Rect(rectLeft.x + rectLeft.width - 128, rectLeft.y + rectLeft.height - 64, 64, 64);
                rectDebugButtonFontDesc = new Rect(rectLeft.x + rectLeft.width - 64, rectLeft.y + rectLeft.height - 64, 64, 64);
                rectCommandBackground = new Rect(rectRight.x, rectRight.y + 64, rectRight.width, rectRight.height - 64);
                rectCommandArea = new Rect(rectRight.x + 8, rectRight.y + 136, rectRight.width - 16, rectRight.height - 144);
                rectCommandInput = new Rect(rectRight.x, rectRight.y, rectRight.width - 64, 64);
                rectCommandHelpBox = new Rect(rectRight.x + 8, rectRight.y + 72, rectRight.width - 64, 60);
                rectCommandReturn = new Rect(rectRight.x + rectRight.width - 64, rectRight.y, 64, 64);
            }
            else
            {
                // Portrait: split rect horizontally.
                Rect rectTop = new Rect(rect.x, rect.y, rect.width, rect.height / 2);
                Rect rectBottom = new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2);
                rectDebugLogBackground = new Rect(rectTop.x, rectTop.y, rectTop.width, rectTop.height - 64);
                rectDebugLogScroll = new Rect(rectTop.x + 8, rectTop.y + 24, rectTop.width - 16, rectTop.height - 96);
                rectDebugLogScrollDrag = new Rect(rectTop.x + 8, rectTop.y + 24, rectTop.width - 48, rectTop.height - 96);
                rectDebugButtonClear = new Rect(rectTop.x, rectTop.y + rectTop.height - 64, 128, 64);
                rectDebugButtonFontInc = new Rect(rectTop.x + rectTop.width - 128, rectTop.y + rectTop.height - 64, 64, 64);
                rectDebugButtonFontDesc = new Rect(rectTop.x + rectTop.width - 64, rectTop.y + rectTop.height - 64, 64, 64);
                rectCommandBackground = new Rect(rectBottom.x, rectBottom.y + 64, rectBottom.width, rectBottom.height - 64);
                rectCommandArea = new Rect(rectBottom.x + 8, rectBottom.y + 136, rectBottom.width - 16, rectBottom.height - 144);
                rectCommandInput = new Rect(rectBottom.x, rectBottom.y, rectBottom.width - 64, 64);
                rectCommandHelpBox = new Rect(rectBottom.x + 8, rectBottom.y + 72, rectBottom.width - 64, 60);
                rectCommandReturn = new Rect(rectBottom.x + rectBottom.width - 64, rectBottom.y, 64, 64);
            }

            // Draw the debug log background.
            GUI.Box(rectDebugLogBackground, GUIContent.none, guiSkin.customStyles[0]);
            GUILayout.Space(24f);
            using (new GUILayout.AreaScope(rectDebugLogScroll))
            {
                using (var scrollScope = new GUILayout.ScrollViewScope(scrollDebugPosition, guiSkin.scrollView))
                {
                    scrollScope.handleScrollWheel = true;
                    scrollDebugPosition = scrollScope.scrollPosition;
                    // Only draw up to logCapacity entries.
                    for (indexDebug = 0; indexDebug < log.Count && indexDebug <= logCapacity; indexDebug++)
                    {
                        GUILayout.Label(log[indexDebug], guiSkin.label);
                    }
                }
            }
            if (GUI.Button(rectDebugButtonClear, "Clear", guiSkin.button))
                ClearLogs();
            if (GUI.Button(rectDebugButtonFontInc, "A+", guiSkin.button))
            {
                if (guiSkin.label.fontSize < 64)
                    guiSkin.label.fontSize += 2;
            }
            if (GUI.Button(rectDebugButtonFontDesc, "A-", guiSkin.button))
            {
                if (guiSkin.label.fontSize > 8)
                    guiSkin.label.fontSize -= 2;
            }
            GUI.SetNextControlName("commandfield");
            GUI.enabled = commandData.Count > 0;
            command = GUI.TextField(rectCommandInput, command, guiSkin.textField);
            if (GUI.Button(rectCommandReturn, "Send", guiSkin.button))
                ExecuteCurrentCommand();
            GUI.enabled = true;
            GUI.Box(rectCommandBackground, GUIContent.none, guiSkin.customStyles[1]);
            GUI.Label(rectCommandHelpBox, commandHelpText, guiSkin.customStyles[4]);

            // Draw the command–preset list.
            using (new GUILayout.AreaScope(rectCommandArea))
            {
                using (var scrollScope = new GUILayout.ScrollViewScope(scrollCommandPosition, guiSkin.scrollView))
                {
                    scrollScope.handleScrollWheel = true;
                    scrollCommandPosition = scrollScope.scrollPosition;
                    string cacheGroupName = null;
                    using (new GUILayout.VerticalScope())
                    {
                        foreach (var commandPreset in commandPresets)
                        {
                            if (cacheGroupName != commandPreset.group)
                            {
                                cacheGroupName = commandPreset.group;
                                if (!string.IsNullOrEmpty(cacheGroupName))
                                {
                                    if (GUILayout.Button(cacheGroupName, guiSkin.customStyles[2]))
                                        commandDisplayingGroup = commandDisplayingGroup == cacheGroupName ? null : cacheGroupName;
                                }
                                else
                                    GUILayout.Label("Etc.", guiSkin.customStyles[2]);
                            }
                            if (!string.IsNullOrEmpty(commandDisplayingGroup) && commandDisplayingGroup == cacheGroupName ||
                                string.IsNullOrEmpty(cacheGroupName))
                            {
                                if (GUILayout.Button(commandPreset.name, guiSkin.customStyles[3]))
                                {
                                    commandHelpText = commandPreset.description ?? string.Empty;
                                    if (commandPreset.executeImmediately)
                                        _ = ExecuteCommand(commandPreset.preset);
                                    else
                                    {
                                        command = commandPreset.preset;
                                        GUI.FocusControl("commandfield");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // Handle key events (up/down arrows, enter) when the text field is focused.
            if (GUI.GetNameOfFocusedControl() == "commandfield" && Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.UpArrow:
                        GetPreviousExecuteLog();
                        break;
                    case KeyCode.DownArrow:
                        GetNextExecuteLog();
                        break;
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        ExecuteCurrentCommand();
                        break;
                }
            }
        }

        private void UpdatePreviewUI(Rect rect)
        {
            Rect GetRectByAnchor(Rect rect, TextAnchor anchor, Vector2 size) => anchor switch
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

            rectPreviewWindow = GetRectByAnchor(rect, previewAnchorPosition, previewSize);
            using (new GUILayout.AreaScope(rectPreviewWindow, GUIContent.none, guiSkin.customStyles[5]))
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

        private void UpdateScreenMessageUI(Rect rect)
        {
            using (new GUILayout.AreaScope(rect))
            {
                using (new GUILayout.VerticalScope())
                {
                    for (int i = 0; i < screenMessages.Count; ++i)
                        GUILayout.Label(screenMessages[i].ToString());
                }
            }
        }

        #endregion

        #region Command Registration and Execution

        public void AddCommand(string prefix, LuviCommandExecution execution)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException("Prefix cannot be null or empty.");
#if !LUVI_CONSOLE_ADD_COMMAND_EXCLUDE_START_WITH_SLASH
            if (!prefix.StartsWith("/"))
                throw new ArgumentException("Prefix must start with slash (/) character.");
#endif
            if (prefix.Contains(" "))
                throw new ArgumentException("Prefix cannot contain any white space.");
            if (commandData.ContainsKey(prefix))
                throw new InvalidOperationException($"Command with prefix '{prefix}' already exists.");
            commandData.Add(prefix, execution);
        }

        public bool RemoveCommand(string prefix) => commandData.Remove(prefix);

        public void AddCommandPreset(string preset, string name, string description = null, string group = null, bool executeImmediately = false)
        {
            if (string.IsNullOrWhiteSpace(preset))
                throw new ArgumentException("Preset cannot be null or whitespace.", nameof(preset));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            commandPresets.Add(new CommandPreset(preset, name.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                string.IsNullOrWhiteSpace(group) ? null : group.Trim(),
                executeImmediately));
            // For a small list, sorting each time is acceptable.
            commandPresets.Sort((lhs, rhs) => string.Compare(lhs.group, rhs.group, StringComparison.Ordinal));
        }

        public bool RemoveCommandPreset(string name)
        {
            int index = commandPresets.FindIndex(cp => cp.name == name);
            if (index < 0)
                return false;
            commandPresets.RemoveAt(index);
            return true;
        }

        public bool RemoveCommandPresetAt(int index)
        {
            if (index < 0 || index >= commandPresets.Count)
                return false;
            commandPresets.RemoveAt(index);
            return true;
        }

        public IReadOnlyList<string> GetAllCommandPresets()
        {
            List<string> list = new List<string>(commandPresets.Count);
            foreach (var cp in commandPresets)
                list.Add(cp.preset);
            return list;
        }

        public IReadOnlyList<string> GetAllCommandPresetsName()
        {
            List<string> list = new List<string>(commandPresets.Count);
            foreach (var cp in commandPresets)
                list.Add(cp.name);
            return list;
        }

        public void ClearAllCommands() => commandData.Clear();

        public void ClearAllCommandPresets() => commandPresets.Clear();

        public bool ExecuteCommand(string command)
        {
            command = command.Trim();
            if (commandLog)
            {
                str.Clear();
                str.Append(">").Append(command).Italic().Color(LOG_COLOR_COMMAND);
                Log(str.ToString());
            }
            if (!string.IsNullOrEmpty(command))
            {
                IReadOnlyList<string> arguments = SplitCommandArguments(command);
                if (arguments.Count > 0)
                {
                    string prefix = arguments[0];
                    LogExecuteCommands(command);
                    // Use TryGetValue to avoid a duplicate lookup.
                    if (commandData.TryGetValue(prefix, out LuviCommandExecution commandExec))
                    {
                        try
                        {
                            commandExec.Invoke(arguments);
                        }
                        catch (Exception e)
                        {
                            LogReceiveCallback(e.Message, e.StackTrace, LogType.Exception);
                        }
                        return true;
                    }
                    else
                    {
                        str.Clear();
                        str.Append($"No command '{prefix}' found.").Color(LOG_COLOR_COMMAND);
                        Log(str.ToString());
                        return false;
                    }
                }
                return false;
            }
            return false;
        }

        private void ExecuteCurrentCommand()
        {
            _ = ExecuteCommand(command);
            command = string.Empty;
        }

        private void LogExecuteCommands(string command)
        {
            logExecuteCommands.Add(command);
            while (logExecuteCommands.Count > executeCapacity)
                logExecuteCommands.RemoveAt(0);
            logExecutePosition = logExecuteCommands.Count;
        }

        private IReadOnlyList<string> SplitCommandArguments(string command)
        {
            List<string> arguments = new List<string>();
            int start = 0;
            bool inQuote = false;
            bool inJSON = false;
            bool lastIsSpace = false;
            for (int i = 0; i < command.Length; i++)
            {
                if (command[i] == '{' && !inJSON && (i == 0 || lastIsSpace))
                {
                    inJSON = true;
                    start = i;
                }
                else if (command[i] == '}' && inJSON)
                {
                    inJSON = false;
                    if (start != i)
                        arguments.Add(command.Substring(start, i + 1 - start));
                    start = i + 1;
                }
                else if (command[i] == '"' && !inJSON)
                {
                    inQuote = !inQuote;
                    if (inQuote)
                        start = i + 1;
                    else
                    {
                        if (start != i)
                            arguments.Add(command.Substring(start, i - start));
                        start = i + 1;
                    }
                }
                else if (command[i] == ' ' && !inQuote && !inJSON)
                {
                    if (start != i)
                        arguments.Add(command.Substring(start, i - start));
                    start = i + 1;
                    lastIsSpace = true;
                }
                else
                    lastIsSpace = false;
            }
            var lastString = command.Substring(start);
            if (!string.IsNullOrEmpty(lastString))
                arguments.Add(lastString);
            return arguments;
        }

        private void GetNextExecuteLog()
        {
            if (logExecuteCommands.Count == 0)
                return;
            logExecutePosition++;
            if (logExecutePosition >= logExecuteCommands.Count)
            {
                logExecutePosition = logExecuteCommands.Count - 1;
                command = string.Empty;
                return;
            }
            command = logExecuteCommands[logExecutePosition];
        }

        private void GetPreviousExecuteLog()
        {
            if (logExecuteCommands.Count == 0)
                return;
            logExecutePosition--;
            if (logExecutePosition < 0)
                logExecutePosition = 0;
            command = logExecuteCommands[logExecutePosition];
        }

        #endregion
    }
}
