using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace LuviKunG.Console
{
    using Extension;

    [AddComponentMenu("LuviKunG/LuviConsole")]
    [HelpURL("https://github.com/LuviKunG/LuviConsole")]
    [DisallowMultipleComponent]
    public sealed partial class LuviConsole : MonoBehaviour
    {
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

        private sealed class ScreenMessage
        {
            public readonly string text;
            public float duration;
            public Label label;

            public ScreenMessage(string message, float duration, Color? color, bool bold, bool italic)
            {
                StringBuilder str = GetStringBuilder();
                str.Append(message);
                if (color.HasValue)
                    str.Color(color.Value);
                if (bold)
                    str.Bold();
                if (italic)
                    str.Italic();
                this.text = str.ToString();
                this.duration = duration;
            }
        }

        private enum LogKind
        {
            Custom,
            Verbose,
            Warning,
            Error,
            Exception,
        }

        private readonly struct PendingLog
        {
            public readonly string text;
            public readonly LogKind kind;

            public PendingLog(string text, LogKind kind)
            {
                this.text = text;
                this.kind = kind;
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

        private static readonly Color LOG_COLOR_COMMAND = new Color(0.0f, 1.0f, 1.0f, 1.0f); // Cyan
        private static readonly Color LOG_COLOR_WARNING = new Color(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        private static readonly Color LOG_COLOR_ERROR = new Color(1.0f, 0.0f, 0.0f, 1.0f);   // Red

        // Log callbacks can arrive from any thread, so each thread formats with its own builder.
        [ThreadStatic]
        private static StringBuilder threadStringBuilder;

        private static StringBuilder GetStringBuilder()
        {
            threadStringBuilder ??= new StringBuilder();
            threadStringBuilder.Clear();
            return threadStringBuilder;
        }

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
        [Tooltip("Optional style sheet that replaces the default LuviConsole style sheet.")]
        private StyleSheet styleSheet;

        [Min(0)]
        [Tooltip("The capacity of list that will show debug log on console window.")]
        public int logCapacity = 64;
        [Min(0)]
        [FormerlySerializedAs("excuteCapacity")]
        [Tooltip("The capacity of executed commands that are kept in history.")]
        public int executeCapacity = 16;
        [Range(MIN_FONT_SIZE, MAX_FONT_SIZE)]
        [Tooltip("Default font size of console when it getting start.")]
        public int defaultFontSize = 16;
        [Tooltip("Automatically show logs when player is get warning log.")]
        public bool autoShowWarning = false;
        [Tooltip("Automatically show logs when player is get error log.")]
        public bool autoShowError = false;
        [Tooltip("Automatically show logs when player is get exception log.")]
        public bool autoShowException = false;
        [Tooltip("Log the command after you executed.")]
        public bool commandLog = false;

        [Tooltip("Input action to toggle open and close of the console.")]
        public InputAction toggleConsoleAction = new InputAction("Toggle Console", InputActionType.Button, "<Keyboard>/f1");
        [Tooltip("Input action to toggle open and close of the preview.")]
        public InputAction togglePreviewAction = new InputAction("Toggle Preview", InputActionType.Button, "<Keyboard>/f2");
        [Tooltip("Input action to toggle open and close of the screen message.")]
        public InputAction toggleScreenMessageAction = new InputAction("Toggle Screen Message", InputActionType.Button, "<Keyboard>/f3");
        [Tooltip("Allow swipe gestures on touch screens. Swipe from the left half to toggle the console, from the right half to toggle the preview.")]
        public bool swipeToToggle = true;
        [Range(0.01f, 0.99f)]
        [Tooltip("The swipe distance, as a ratio of screen height, that is required to toggle the console or preview.")]
        public float swipeRatio = 0.8f;

        [Tooltip("Define anchor position of preview window.")]
        public TextAnchor previewAnchorPosition = TextAnchor.LowerRight;
        [Tooltip("Define size of preview window.")]
        public Vector2 previewSize = new Vector2(128f, 96f);
        [Tooltip("Show screen messages when the console awakes.")]
        public bool showScreenMessageOnAwake = true;
        [Tooltip("Extra inset of the UI from the screen safe area.")]
        public Extend2D extendUI = new Extend2D(0);

        #endregion

        #region Internal Data

        private readonly Dictionary<string, LuviCommandExecution> commandData = new Dictionary<string, LuviCommandExecution>();
        private readonly List<CommandPreset> commandPresets = new List<CommandPreset>();
        private readonly List<string> logExecuteCommands = new List<string>();
        private readonly List<ScreenMessage> screenMessages = new List<ScreenMessage>();
        private readonly ConcurrentQueue<PendingLog> pendingLogs = new ConcurrentQueue<PendingLog>();
        private readonly List<PendingLog> flushingLogs = new List<PendingLog>();

        private bool isShowingConsole;
        private bool isShowingPreview;
        private bool isShowingScreenMessage;
        private string commandDisplayingGroup = null;
        private int logExecutePosition;
        private int countLogVerbose;
        private int countLogWarning;
        private int countLogError;
        private int countLogException;

        #endregion

        #region Properties

        /// <summary>
        /// Show or hide the console window.
        /// </summary>
        public bool IsShowingConsole
        {
            get => isShowingConsole;
            set
            {
                if (isShowingConsole == value)
                    return;
                isShowingConsole = value;
                OnConsoleVisibilityChanged();
            }
        }

        /// <summary>
        /// Show or hide the preview window.
        /// </summary>
        public bool IsShowingPreview
        {
            get => isShowingPreview;
            set
            {
                isShowingPreview = value;
                RefreshVisibility();
            }
        }

        /// <summary>
        /// Show or hide the screen messages.
        /// </summary>
        public bool IsShowingScreenMessage
        {
            get => isShowingScreenMessage;
            set
            {
                isShowingScreenMessage = value;
                RefreshVisibility();
            }
        }

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
            if (instance != this)
                return;
            Application.logMessageReceivedThreaded += LogReceiveCallback;
            EnableInputActions();
            AttachUI();
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= LogReceiveCallback;
            DisableInputActions();
            DetachUI();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
            DestroyUI();
        }

        private void Update()
        {
            UpdateInput();
            UpdateScreenMessages(Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            AttachUI();
            FlushPendingLogs();
            UpdateUILayout();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                ApplyUISettings();
        }
#endif

        #endregion

        #region Initialization

        private void Initialize()
        {
            isShowingScreenMessage = showScreenMessageOnAwake;
            SetupUI();
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Logging Methods

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            pendingLogs.Enqueue(new PendingLog(message, LogKind.Custom));
        }

        public void Log(string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            StringBuilder str = GetStringBuilder();
            str.Append(message);
            str.Color(color);
            Log(str.ToString());
        }

        public void Log(string message, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            StringBuilder str = GetStringBuilder();
            str.Append(message);
            if (bold)
                str.Bold();
            if (italic)
                str.Italic();
            Log(str.ToString());
        }

        public void Log(string message, Color color, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            StringBuilder str = GetStringBuilder();
            str.Append(message);
            str.Color(color);
            if (bold)
                str.Bold();
            if (italic)
                str.Italic();
            Log(str.ToString());
        }

        public void LogScreen(string message, float duration)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            LogScreen(new ScreenMessage(message, duration, null, false, false));
        }

        public void LogScreen(string message, float duration, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            LogScreen(new ScreenMessage(message, duration, null, bold, italic));
        }

        public void LogScreen(string message, float duration, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            LogScreen(new ScreenMessage(message, duration, color, false, false));
        }

        public void LogScreen(string message, float duration, Color color, bool bold, bool italic)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            LogScreen(new ScreenMessage(message, duration, color, bold, italic));
        }

        private void LogScreen(ScreenMessage message)
        {
            screenMessages.Add(message);
            AddScreenMessageLabel(message);
        }

        // This callback may be invoked from any thread, so logs are queued and flushed on the main thread.
        private void LogReceiveCallback(string message, string stackTrace, LogType type)
        {
            StringBuilder str = GetStringBuilder();
            switch (type)
            {
                case LogType.Warning:
                    str.Append("Warning: ").Append(message);
                    str.Color(LOG_COLOR_WARNING).Italic();
                    pendingLogs.Enqueue(new PendingLog(str.ToString(), LogKind.Warning));
                    break;
                case LogType.Error:
                    str.Append("Error: ").Append(message);
                    str.Color(LOG_COLOR_ERROR).Bold();
                    pendingLogs.Enqueue(new PendingLog(str.ToString(), LogKind.Error));
                    break;
                case LogType.Exception:
                    str.Append("Exception: ").Append(message).Bold().AppendLine().Append(stackTrace);
                    str.Color(LOG_COLOR_ERROR);
                    pendingLogs.Enqueue(new PendingLog(str.ToString(), LogKind.Exception));
                    break;
                default:
                    if (!string.IsNullOrEmpty(message))
                        pendingLogs.Enqueue(new PendingLog(message, LogKind.Verbose));
                    break;
            }
        }

        private void FlushPendingLogs()
        {
            if (pendingLogs.IsEmpty)
                return;
            flushingLogs.Clear();
            while (pendingLogs.TryDequeue(out PendingLog pendingLog))
                flushingLogs.Add(pendingLog);
            bool shouldShowConsole = false;
            for (int i = 0; i < flushingLogs.Count; ++i)
            {
                switch (flushingLogs[i].kind)
                {
                    case LogKind.Verbose:
                        countLogVerbose++;
                        break;
                    case LogKind.Warning:
                        countLogWarning++;
                        shouldShowConsole |= autoShowWarning;
                        break;
                    case LogKind.Error:
                        countLogError++;
                        shouldShowConsole |= autoShowError;
                        break;
                    case LogKind.Exception:
                        countLogException++;
                        shouldShowConsole |= autoShowException;
                        break;
                }
            }
            // Only the newest entries that fit in the capacity will ever be visible.
            int start = Mathf.Max(0, flushingLogs.Count - logCapacity);
            for (int i = start; i < flushingLogs.Count; ++i)
                AddLogEntry(flushingLogs[i].text);
            flushingLogs.Clear();
            RefreshPreviewCounts();
            if (shouldShowConsole)
                IsShowingConsole = true;
        }

        public void ClearLogs()
        {
            ClearLogEntries();
            countLogVerbose = countLogWarning = countLogError = countLogException = 0;
            RefreshPreviewCounts();
        }

        private void UpdateScreenMessages(float deltaTime)
        {
            // Iterate backward so removals don't mess up the index.
            for (int i = screenMessages.Count - 1; i >= 0; --i)
            {
                ScreenMessage message = screenMessages[i];
                message.duration -= deltaTime;
                if (message.duration < 0.0f)
                {
                    message.label?.RemoveFromHierarchy();
                    screenMessages.RemoveAt(i);
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
            RefreshCommandInputState();
        }

        public bool RemoveCommand(string prefix)
        {
            bool removed = commandData.Remove(prefix);
            RefreshCommandInputState();
            return removed;
        }

        public void AddCommandPreset(string preset, string name, string description = null, string group = null, bool executeImmediately = false)
        {
            if (string.IsNullOrWhiteSpace(preset))
                throw new ArgumentException("Preset cannot be null or whitespace.", nameof(preset));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            CommandPreset commandPreset = new CommandPreset(preset, name.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                string.IsNullOrWhiteSpace(group) ? null : group.Trim(),
                executeImmediately);
            // Insert after every preset of the same or lower group, so the list stays sorted by group in insertion order.
            int index = commandPresets.FindIndex(cp => string.Compare(cp.group, commandPreset.group, StringComparison.Ordinal) > 0);
            commandPresets.Insert(index < 0 ? commandPresets.Count : index, commandPreset);
            RebuildCommandPresets();
        }

        public bool RemoveCommandPreset(string name)
        {
            int index = commandPresets.FindIndex(cp => cp.name == name);
            if (index < 0)
                return false;
            commandPresets.RemoveAt(index);
            RebuildCommandPresets();
            return true;
        }

        public bool RemoveCommandPresetAt(int index)
        {
            if (index < 0 || index >= commandPresets.Count)
                return false;
            commandPresets.RemoveAt(index);
            RebuildCommandPresets();
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

        public void ClearAllCommands()
        {
            commandData.Clear();
            RefreshCommandInputState();
        }

        public void ClearAllCommandPresets()
        {
            commandPresets.Clear();
            RebuildCommandPresets();
        }

        public bool ExecuteCommand(string command)
        {
            command = command?.Trim() ?? string.Empty;
            if (commandLog)
            {
                StringBuilder str = GetStringBuilder();
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
                        StringBuilder str = GetStringBuilder();
                        str.Append($"No command '{prefix}' found.").Color(LOG_COLOR_COMMAND);
                        Log(str.ToString());
                        return false;
                    }
                }
                return false;
            }
            return false;
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

        private string GetNextExecuteLog()
        {
            if (logExecuteCommands.Count == 0)
                return null;
            logExecutePosition++;
            if (logExecutePosition >= logExecuteCommands.Count)
            {
                logExecutePosition = logExecuteCommands.Count - 1;
                return string.Empty;
            }
            return logExecuteCommands[logExecutePosition];
        }

        private string GetPreviousExecuteLog()
        {
            if (logExecuteCommands.Count == 0)
                return null;
            logExecutePosition--;
            if (logExecutePosition < 0)
                logExecutePosition = 0;
            return logExecuteCommands[logExecutePosition];
        }

        #endregion
    }
}
