using LuviKunG.Console.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuviKunG.Console
{
    [AddComponentMenu("LuviKunG/LuviConsole")]
    [HelpURL("https://github.com/LuviKunG/LuviConsole")]
    public sealed class LuviConsole : MonoBehaviour
    {
        private struct CommandPreset
        {
            public string preset;
            public string name;
            public string description;
            public string group;
            public bool executeImmediately;

            public CommandPreset(string preset, string name, string description, string group, bool executeImmediately)
            {
                this.preset = preset;
                this.name = name;
                this.description = description;
                this.group = group;
                this.executeImmediately = executeImmediately;
            }
        }

        private const string DEFAULT_PREFAB_RESOURCE_PATH = "LuviKunG/LuviConsole";
        private const string DEFAULT_GUISKIN_RESOURCE_PATH = "LuviConsoleGUI";
        private static readonly Color LOG_COLOR_COMMAND = new Color(0.0f, 1.0f, 1.0f, 1.0f); // Cyan
        private static readonly Color LOG_COLOR_WARNING = new Color(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        private static readonly Color LOG_COLOR_ERROR = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red

        private static LuviConsole instance;

        /// <summary>
        /// Get the instance of <see cref="LuviConsole"/>.
        /// <see cref="LuviConsole"/> must be only one instance and being singleton.
        /// </summary>
        public static LuviConsole Instance
        {
            get
            {
                if (instance == null)
                {
                    LuviConsole prefab = Resources.Load<LuviConsole>(DEFAULT_PREFAB_RESOURCE_PATH);
                    if (prefab == null) throw new MissingReferenceException($"Cannot find {nameof(LuviConsole)} asset/prefab in resources path {DEFAULT_PREFAB_RESOURCE_PATH}.");
                    instance = Instantiate(prefab);
                    instance.name = prefab.name;
                }
                return instance;
            }
        }

        [SerializeField]
        private GUISkin guiSkin;

        /// <summary>
        /// Log capacity of this console.
        /// Must be positive number and recommend not to high (over 100+) because it will cause IMGUI laggy.
        /// Default value is 64.
        /// </summary>
        public int logCapacity = 64;

        /// <summary>
        /// Log capacity of execute commands in console.
        /// Must be positive number.
        /// Default value is 16.
        /// </summary>
        public int excuteCapacity = 16;

#if UNITY_ANDROID || UNITY_IOS
        /// <summary>
        /// Swipe ratio of touch screen to toggle show/hide the console.
        /// 1.0 means need to drag from the top to bottom of touch screen and 0.0 means just tap and drag a little bit in touch screen to toggle.
        /// This value will available when Unity using Target Build of Android or iOS.
        /// </summary>
        public float swipeRatio = 0.8f;
#endif

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
        /// Show log command when executing the command in this console.
        /// </summary>
        public bool commandLog = false;

        /// <summary>
        /// Keys to toggle show/hide of this console.
        /// This value will available when using in Unity Editor and Unity using Target Build of WebGL.
        /// </summary>
        public KeyCode[] keys = new KeyCode[] { KeyCode.F1 };

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.LogError($"There are 2 or more instance of \'{nameof(LuviConsole)}\' on the scene. The latest instance will be destroy.");
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
            if (isShowing)
                UpdateScrollDrag();
        }

        private void OnGUI()
        {
            GUI.skin = guiSkin;
            if (isShowing)
            {
                UpdateWindow();
                ShowDebugWindow();
                ShowCommandWindow();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            LoadGUISkin();
        }
#endif

        private void Initialize()
        {
            LoadGUISkin();
            DontDestroyOnLoad(gameObject);
            UpdateWindow();
        }

        private void UpdateInput()
        {
#if UNITY_EDITOR
            if (keys == null || keys.Length == 0)
                return;
            bool isKey = true;
            for (int i = 0; i < keys.Length - 1; ++i)
            {
                if (!Input.GetKey(keys[i]))
                {
                    isKey = false;
                    return;
                }
            }
            if (isKey && Input.GetKeyDown(keys[keys.Length - 1]))
            {
                ToggleConsole();
                GUI.FocusControl("commandfield");
            }
#elif UNITY_ANDROID || UNITY_IOS
            if (Input.GetMouseButtonDown(0))
            {
                _swipePosStart = Input.mousePosition;
            }
            if (Input.GetMouseButton(0) && _swipePosStart != Vector3.zero)
            {
                _swipePosMoving = Input.mousePosition;
                Vector3 direction = _swipePosMoving - _swipePosStart;
                if (Vector3.Distance(_swipePosStart, _swipePosMoving) > (Screen.height * swipeRatio))
                {
                    if (Mathf.Abs(direction.x) <= Mathf.Abs(direction.y))
                    {
                        if (direction.y > 0)
                        {
                            _swipePosStart = Vector3.zero;
                            _swipePosMoving = Vector3.zero;
                            isShowing = true;
                        }
                        else
                        {
                            _swipePosStart = Vector3.zero;
                            _swipePosMoving = Vector3.zero;
						    isShowing = false;
                        }
                    }
                }
            }
#elif UNITY_WEBGL
#endif
        }

        private void LoadGUISkin()
        {
            if (guiSkin == null)
                guiSkin = Resources.Load<GUISkin>(DEFAULT_GUISKIN_RESOURCE_PATH);
            if (guiSkin != null)
                guiSkin.label.fontSize = defaultFontSize;
        }

        private void UpdateScrollDrag()
        {
            Vector2 currentPosition = Input.mousePosition;
            // Bacause Input.mousePosition is start at bottom-left, so we need to convert into the same as Rect that start at top-left.
            currentPosition.y = Screen.height - currentPosition.y;
            if (Input.GetMouseButtonDown(0) && rectDebugLogScrollDrag.Contains(currentPosition))
            {
                isScrollDebugDragging = true;
                scrollDebugDragPosition = currentPosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                isScrollDebugDragging = false;
            }
            if (isScrollDebugDragging)
            {
                Vector2 delta = scrollDebugDragPosition - currentPosition;
                scrollDebugPosition += delta;
                scrollDebugDragPosition = currentPosition;
            }
        }

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
        private Vector2 scrollDebugPosition = Vector2.zero;
        private Vector2 scrollCommandPosition = Vector2.zero;
        private Vector2 scrollDebugDragPosition = Vector2.zero;
        private Vector2 scrollCommandDragPosition = Vector2.zero;
        private Dictionary<string, LuviCommandExecution> commandData = new Dictionary<string, LuviCommandExecution>();
        private List<CommandPreset> commandPresets = new List<CommandPreset>();
        private List<string> log = new List<string>();
        private List<string> logExecuteCommands = new List<string>();
        private StringBuilder str = new StringBuilder();

        private bool isShowing;
        private bool isScrollDebugDragging = false;
        private string commandHelpText = string.Empty;
        private string command = string.Empty;
        private string commandDisplayingGroup = null;
        private int logExecutePosition;
        private int indexConsole;
        private int indexDebug;
#if !(UNITY_EDITOR || UNITY_EDITOR_OSX || UNITY_WEBGL) && (UNITY_ANDROID || UNITY_IOS)
        private Vector3 _swipePosStart = Vector3.zero;
        private Vector3 _swipePosMoving = Vector3.zero;
#endif

        /// <summary>
        /// Log the message into console.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            log.Add(message);
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        /// <summary>
        /// Log the message into console.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="color">Color of this message.</param>
        public void Log(string message, Color color)
        {
            if (string.IsNullOrEmpty(message))
                return;
            str.Clear();
            str.Append(message);
            str.Color(color);
            log.Add(str.ToString());
            UpdateLogCapacity();
            ScrollLogToBottom();
        }

        /// <summary>
        /// Log the message into console.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="bold">Bold this message.</param>
        /// <param name="italic">Italic this message.</param>
        public void Log(string message, bool bold, bool italic)
        {
            if (string.IsNullOrEmpty(message))
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

        /// <summary>
        /// Log the message into console.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="color">Color of this message.</param>
        /// <param name="bold">Bold this message.</param>
        /// <param name="italic">Italic this message.</param>
        public void Log(string message, Color color, bool bold, bool italic)
        {
            if (string.IsNullOrEmpty(message))
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
                    {
                        str.Clear();
                        str.Append("Warning: ");
                        str.Append(message);
                        str.Color(LOG_COLOR_WARNING);
                        str.Italic();
                        Log(str.ToString());
                        if (autoShowWarning & !isShowing)
                            isShowing = true;
                    }
                    break;
                case LogType.Error:
                    {
                        str.Clear();
                        str.Append("Error: ");
                        str.Append(message);
                        str.Color(LOG_COLOR_ERROR);
                        str.Bold();
                        Log(str.ToString());
                        if (autoShowError & !isShowing)
                            isShowing = true;
                    }
                    break;
                case LogType.Exception:
                    {
                        str.Clear();
                        str.Append("Exception: ");
                        str.Append(message);
                        str.Bold();
                        str.AppendLine();
                        str.Append(stackTrace);
                        str.Color(LOG_COLOR_ERROR);
                        Log(str.ToString());
                        if (autoShowException & !isShowing)
                            isShowing = true;
                    }
                    break;
                default:
                    Log(message);
                    break;
            }
        }

        /// <summary>
        /// Clear all logs.
        /// </summary>
        public void ClearLogs()
        {
            log.Clear();
        }

        private void ToggleConsole()
        {
            isShowing = !isShowing;
        }

        private void ScrollLogToBottom()
        {
            instance.scrollDebugPosition = instance.scrollLockPosition;
        }

        private void UpdateLogCapacity()
        {
            while (log.Count > logCapacity)
                log.RemoveAt(0);
        }

        private void UpdateWindow()
        {
            if (Screen.width > Screen.height)
            {
                //Landscape
                rectDebugLogBackground = new Rect(0, 0, Screen.width / 2, Screen.height - 64);
                rectDebugLogScroll = new Rect(8, 24, (Screen.width / 2) - 16, Screen.height - 96);
                rectDebugLogScrollDrag = new Rect(8, 24, (Screen.width / 2) - 48, Screen.height - 96);
                rectDebugButtonClear = new Rect(0, Screen.height - 64, 128, 64);
                rectDebugButtonFontInc = new Rect((Screen.width / 2) - 128, Screen.height - 64, 64, 64);
                rectDebugButtonFontDesc = new Rect((Screen.width / 2) - 64, Screen.height - 64, 64, 64);
                rectCommandBackground = new Rect(Screen.width / 2, 64, Screen.width / 2, Screen.height - 64);
                rectCommandArea = new Rect((Screen.width / 2) + 8, 136, (Screen.width / 2) - 16, Screen.height - 144);
                rectCommandInput = new Rect(Screen.width / 2, 0, (Screen.width / 2) - 64, 64);
                rectCommandHelpBox = new Rect((Screen.width / 2) + 8, 72, (Screen.width / 2) - 64, 60);
                rectCommandReturn = new Rect(Screen.width - 64, 0, 64, 64);
            }
            else
            {
                //Portrait
                rectDebugLogBackground = new Rect(0, 0, Screen.width, (Screen.height / 2) - 64);
                rectDebugLogScroll = new Rect(8, 24, Screen.width - 16, (Screen.height / 2) - 96);
                rectDebugLogScrollDrag = new Rect(8, 24, Screen.width - 48, (Screen.height / 2) - 96);
                rectDebugButtonClear = new Rect(0, (Screen.height / 2) - 64, 128, 64);
                rectDebugButtonFontInc = new Rect(Screen.width - 128, (Screen.height / 2) - 64, 64, 64);
                rectDebugButtonFontDesc = new Rect(Screen.width - 64, (Screen.height / 2) - 64, 64, 64);
                rectCommandBackground = new Rect(0, (Screen.height / 2) + 64, Screen.width, (Screen.height / 2) - 64);
                rectCommandArea = new Rect(8, (Screen.height / 2) + 136, Screen.width - 16, (Screen.height / 2) - 144);
                rectCommandInput = new Rect(0, Screen.height / 2, Screen.width - 64, 64);
                rectCommandHelpBox = new Rect(8, (Screen.height / 2) + 72, Screen.width - 64, 60);
                rectCommandReturn = new Rect(Screen.width - 64, Screen.height / 2, 64, 64);
            }
        }

        private void ShowDebugWindow()
        {
            GUI.Box(rectDebugLogBackground, GUIContent.none, guiSkin.customStyles[0]);
            GUILayout.Space(24f);
            using (var areaScope = new GUILayout.AreaScope(rectDebugLogScroll))
            {
                using (var scrollScope = new GUILayout.ScrollViewScope(scrollDebugPosition, guiSkin.scrollView))
                {
                    scrollScope.handleScrollWheel = true;
                    scrollDebugPosition = scrollScope.scrollPosition;
                    for (indexDebug = 0; indexDebug < log.Count; indexDebug++)
                    {
                        if (indexDebug > logCapacity)
                            break;
                        GUILayout.Label(log[indexDebug], guiSkin.label);
                    }
                }
            }
            if (GUI.Button(rectDebugButtonClear, "Clear", guiSkin.button))
            {
                ClearLogs();
            }
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
        }

        private void ShowCommandWindow()
        {
            GUI.SetNextControlName("commandfield");
            GUI.enabled = commandData.Count > 0;
            command = GUI.TextField(rectCommandInput, command, guiSkin.textField);
            if (GUI.Button(rectCommandReturn, "Send", guiSkin.button))
                ExecuteCurrentCommand();
            GUI.enabled = true;
            GUI.Box(rectCommandBackground, GUIContent.none, guiSkin.customStyles[1]);
            GUI.Label(rectCommandHelpBox, commandHelpText);
            using (var areaScope = new GUILayout.AreaScope(rectCommandArea))
            {
                using (var scrollScope = new GUILayout.ScrollViewScope(scrollCommandPosition, guiSkin.scrollView))
                {
                    scrollScope.handleScrollWheel = true;
                    scrollCommandPosition = scrollScope.scrollPosition;
                    string cacheGroupName = null;
                    using (var verticalScope = new GUILayout.VerticalScope())
                    {
                        foreach (var commandPreset in commandPresets)
                        {
                            if (cacheGroupName != commandPreset.group)
                            {
                                cacheGroupName = commandPreset.group;
                                if (!string.IsNullOrEmpty(cacheGroupName))
                                {
                                    if (GUILayout.Button(cacheGroupName, guiSkin.customStyles[2]))
                                    {
                                        if (string.IsNullOrEmpty(commandDisplayingGroup) || commandDisplayingGroup != cacheGroupName)
                                            commandDisplayingGroup = cacheGroupName;
                                        else
                                            commandDisplayingGroup = null;
                                    }
                                }
                                else
                                {
                                    GUILayout.Label("Etc.", guiSkin.customStyles[2]);
                                }
                            }
                            if (!string.IsNullOrEmpty(commandDisplayingGroup) && commandDisplayingGroup == cacheGroupName)
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
                            else if (string.IsNullOrEmpty(cacheGroupName))
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
            if (GUI.GetNameOfFocusedControl() == "commandfield")
            {
                if (Event.current.isKey)
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
        }

        /// <summary>
        /// Add new command for this console.
        /// </summary>
        /// <param name="prefix">Prefix of the command.</param>
        /// <param name="execution">Execution callback function.</param>
        public void AddCommand(string prefix, LuviCommandExecution execution)
        {
            if (!commandData.ContainsKey(prefix))
                commandData.Add(prefix, execution);
            else
                throw new InvalidOperationException($"Already have {prefix} command.");
        }

        /// <summary>
        /// Remove command by prefix.
        /// </summary>
        /// <param name="prefix">Prefix of the command.</param>
        /// <returns>Is command removed?</returns>
        public bool RemoveCommand(string prefix)
        {
            return commandData.Remove(prefix);
        }

        /// <summary>
        /// Add new command preset.
        /// This command preset will represent as button for easy to access via command list in console.
        /// If group was assigned, the button will be display in the group.
        /// </summary>
        /// <param name="preset">Preset of the command.</param>
        /// <param name="name">Name of the command.</param>
        /// <param name="description">(Optional) Command description that will display in command list in console.</param>
        /// <param name="group">(Optional) Group of this command. This field can be null for unassign the group.</param>
        /// <param name="executeImmediately">(Optional) Is execute immediately when the button is press?</param>
        public void AddCommandPreset(string preset, string name, string description = null, string group = null, bool executeImmediately = false)
        {
            if (string.IsNullOrWhiteSpace(preset))
                throw new ArgumentException($"{nameof(preset)} shouldn't be null, empty or whitespace.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} shouldn't be null, empty or whitespace.");
            commandPresets.Add(new CommandPreset(preset, name.Trim(), string.IsNullOrWhiteSpace(description) ? null : description.Trim(), string.IsNullOrWhiteSpace(group) ? null : group.Trim(), executeImmediately));
            commandPresets.Sort((lhs, rhs) => string.Compare(lhs.group, rhs.group));
        }

        /// <summary>
        /// Remove command preset by name.
        /// </summary>
        /// <param name="name">Name of the command preset.</param>
        /// <returns>Is command preset removed?</returns>
        public bool RemoveCommandPreset(string name)
        {
            int index = commandPresets.FindIndex(0, (cp) => cp.name == name);
            if (index < 0)
                return false;
            else
            {
                commandPresets.RemoveAt(index);
                return true;
            }
        }

        /// <summary>
        /// Remove command preset at index.
        /// </summary>
        /// <param name="index">Index of command preset.</param>
        /// <returns>Is command preset removed?</returns>
        public bool RemoveCommandPresetAt(int index)
        {
            if (index < 0)
                return false;
            else if (index < commandPresets.Count)
            {
                commandPresets.RemoveAt(index);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Get available command presets.
        /// </summary>
        /// <returns>Readonly list of presets.</returns>
        public IReadOnlyList<string> GetAllCommandPresets()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < commandPresets.Count; ++i)
                list.Add(commandPresets[i].preset);
            return list;
        }

        /// <summary>
        /// Get all name of available command presets.
        /// </summary>
        /// <returns>Readonly list of names.</returns>
        public IReadOnlyList<string> GetAllCommandPresetsName()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < commandPresets.Count; ++i)
                list.Add(commandPresets[i].name);
            return list;
        }

        /// <summary>
        /// Remove all commands in this console.
        /// </summary>
        public void ClearAllCommands()
        {
            commandData.Clear();
        }

        /// <summary>
        /// Remove all command presets in this console.
        /// </summary>
        public void ClearAllCommandPresets()
        {
            commandPresets.Clear();
        }

        /// <summary>
        /// Execute command in this console.
        /// </summary>
        /// <param name="command">Command as string</param>
        /// <returns>Is execute success?</returns>
        public bool ExecuteCommand(string command)
        {
            command = command.Trim();
            if (commandLog)
            {
                str.Clear();
                str.Append(command);
                str.Italic();
                str.Insert(0, '>');
                str.Color(LOG_COLOR_COMMAND);
                Log(str.ToString());
            }
            if (!string.IsNullOrEmpty(command))
            {
                IReadOnlyList<string> arguments = SplitCommandArguments(command);
                if (arguments.Count > 0)
                {
                    string prefix = arguments[0];
                    if (commandData.ContainsKey(prefix))
                    {
                        commandData[prefix].Invoke(arguments);
                        LogExecuteCommands(command);
                        return true;
                    }
                    else
                    {
                        str.Clear();
                        str.Append($"No command \'{prefix}\' were found.");
                        str.Color(LOG_COLOR_COMMAND);
                        Log(str.ToString());
                        LogExecuteCommands(command);
                        return false;
                    }
                }
                else
                    return false;
            }
            else
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
            while (logExecuteCommands.Count > excuteCapacity)
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
                    {
                        arguments.Add(command.Substring(start, i + 1 - start));
                    }
                    start = i + 1;
                }
                else if (command[i] == '"' && !inJSON)
                {
                    inQuote = !inQuote;
                    if (inQuote)
                    {
                        start = i + 1;
                    }
                    else
                    {
                        if (start != i)
                        {
                            arguments.Add(command.Substring(start, i - start));
                        }
                        start = i + 1;
                    }
                }
                else if (command[i] == ' ' && !inQuote && !inJSON)
                {
                    if (start != i)
                    {
                        arguments.Add(command.Substring(start, i - start));
                    }
                    start = i + 1;
                    lastIsSpace = true;
                }
                else
                {
                    lastIsSpace = false;
                }
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
    }
}