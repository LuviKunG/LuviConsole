using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuviKunG.Console
{
    public sealed partial class LuviConsole
    {
        #region UI Constants

        private const float PANEL_SORTING_ORDER = 30000.0f;
        private const int MIN_FONT_SIZE = 8;
        private const int MAX_FONT_SIZE = 64;
        private const int FONT_SIZE_STEP = 2;

        private const string USS_ROOT = "luvi-console";
        private const string USS_SAFE_AREA = USS_ROOT + "__safe-area";
        private const string USS_CONSOLE = USS_ROOT + "__console";
        private const string USS_CONSOLE_PORTRAIT = USS_CONSOLE + "--portrait";
        private const string USS_PANE = USS_ROOT + "__pane";
        private const string USS_PANEL = USS_ROOT + "__panel";
        private const string USS_PANEL_LOG = USS_PANEL + "--log";
        private const string USS_PANEL_COMMAND = USS_PANEL + "--command";
        private const string USS_PANEL_HEADER = USS_ROOT + "__panel-header";
        private const string USS_PANEL_TITLE = USS_ROOT + "__panel-title";
        private const string USS_LOG_SCROLL = USS_ROOT + "__log-scroll";
        private const string USS_LOG_ENTRY = USS_ROOT + "__log-entry";
        private const string USS_TOOLBAR = USS_ROOT + "__toolbar";
        private const string USS_TOOLBAR_HEADER = USS_TOOLBAR + "--header";
        private const string USS_TOOLBAR_FOOTER = USS_TOOLBAR + "--footer";
        private const string USS_SPACER = USS_ROOT + "__spacer";
        private const string USS_BUTTON = USS_ROOT + "__button";
        private const string USS_BUTTON_ACCENT = USS_BUTTON + "--accent";
        private const string USS_BUTTON_WIDE = USS_BUTTON + "--wide";
        private const string USS_COMMAND_FIELD = USS_ROOT + "__command-field";
        private const string USS_COMMAND_HELP = USS_ROOT + "__command-help";
        private const string USS_PRESET_SCROLL = USS_ROOT + "__preset-scroll";
        private const string USS_PRESET_GROUP = USS_ROOT + "__preset-group";
        private const string USS_PRESET_GROUP_EXPANDED = USS_PRESET_GROUP + "--expanded";
        private const string USS_PRESET = USS_ROOT + "__preset";
        private const string USS_PREVIEW_LAYER = USS_ROOT + "__preview-layer";
        private const string USS_PREVIEW = USS_ROOT + "__preview";
        private const string USS_PREVIEW_ROW = USS_ROOT + "__preview-row";
        private const string USS_PREVIEW_LABEL = USS_ROOT + "__preview-label";
        private const string USS_PREVIEW_VERBOSE = USS_PREVIEW_ROW + "--verbose";
        private const string USS_PREVIEW_WARNING = USS_PREVIEW_ROW + "--warning";
        private const string USS_PREVIEW_ERROR = USS_PREVIEW_ROW + "--error";
        private const string USS_PREVIEW_EXCEPTION = USS_PREVIEW_ROW + "--exception";
        private const string USS_SCREEN_MESSAGES = USS_ROOT + "__screen-messages";
        private const string USS_SCREEN_MESSAGE = USS_ROOT + "__screen-message";

#if UNITY_EDITOR
        // GUIDs of Styles/LuviConsoleStyle.uss and Styles/LuviConsoleTheme.tss, which work wherever the package is installed.
        private const string DEFAULT_STYLE_SHEET_GUID = "de9ba989d83dbcd49b0d60a07db1fbef";
        private const string DEFAULT_THEME_STYLE_SHEET_GUID = "3c561d7afc7b1ef4cadac8e190b31495";
#endif

        #endregion

        #region UI Fields

        private UIDocument uiDocument;
        private PanelSettings runtimePanelSettings;
        private VisualElement uiRoot;
        private VisualElement uiSafeArea;
        private VisualElement uiConsole;
        private ScrollView uiLogScroll;
        private TextField uiCommandField;
        private Button uiSendButton;
        private Label uiCommandHelp;
        private ScrollView uiPresetScroll;
        private VisualElement uiPreviewLayer;
        private VisualElement uiPreview;
        private Label uiCountVerbose;
        private Label uiCountWarning;
        private Label uiCountError;
        private Label uiCountException;
        private VisualElement uiScreenMessages;

        private int logFontSize;
        private bool isLogScrollPending;
        private bool isCommandFieldFocusPending;
        private bool isLogDragging;
        private Vector2 logDragPosition;
        private Rect cachedSafeArea;
        private Vector2Int cachedScreenSize;
        private Vector2 cachedPanelSize;
        private Extend2D cachedExtendUI;

        #endregion

        #region UI Setup

#if UNITY_EDITOR
        // Assign the default style sheets when the component is added in the editor.
        private void Reset()
        {
            styleSheet = LoadAssetByGUID<StyleSheet>(DEFAULT_STYLE_SHEET_GUID);
            themeStyleSheet = LoadAssetByGUID<ThemeStyleSheet>(DEFAULT_THEME_STYLE_SHEET_GUID);
        }

        private static T LoadAssetByGUID<T>(string guid) where T : UnityEngine.Object =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#endif

        private void SetupUI()
        {
            logFontSize = Mathf.Clamp(defaultFontSize, MIN_FONT_SIZE, MAX_FONT_SIZE);
            if (!TryGetComponent(out uiDocument))
                uiDocument = gameObject.AddComponent<UIDocument>();
            // Respect panel settings that were assigned to the UIDocument, otherwise create the default one.
            if (uiDocument.panelSettings == null)
            {
                runtimePanelSettings = CreateDefaultPanelSettings();
                uiDocument.panelSettings = runtimePanelSettings;
            }
            uiDocument.sortingOrder = PANEL_SORTING_ORDER;
            BuildUI();
            ApplyUISettings();
            RefreshVisibility();
            RefreshCommandInputState();
            RebuildCommandPresets();
            RefreshPreviewCounts();
        }

        private PanelSettings CreateDefaultPanelSettings()
        {
            if (themeStyleSheet == null)
                Debug.LogWarning($"{nameof(LuviConsole)} has no theme style sheet assigned.", this);
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = $"{nameof(LuviConsole)} Panel Settings";
            settings.themeStyleSheet = themeStyleSheet;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1.0f;
            settings.sortingOrder = PANEL_SORTING_ORDER;
            return settings;
        }

        private void BuildUI()
        {
            uiRoot = new VisualElement { name = USS_ROOT, pickingMode = PickingMode.Ignore };
            uiRoot.AddToClassList(USS_ROOT);
            if (styleSheet != null)
                uiRoot.styleSheets.Add(styleSheet);
            else
                Debug.LogWarning($"{nameof(LuviConsole)} has no style sheet assigned.", this);

            uiSafeArea = AddElement<VisualElement>(uiRoot, USS_SAFE_AREA);
            uiSafeArea.pickingMode = PickingMode.Ignore;
            // Switch between landscape and portrait layout whenever the safe area changes its aspect ratio.
            uiSafeArea.RegisterCallback<GeometryChangedEvent>(OnSafeAreaGeometryChanged);

            // Console window. Landscape: logs on the left, commands on the right. Portrait: logs on top, commands below.
            uiConsole = AddElement<VisualElement>(uiSafeArea, USS_CONSOLE);
            uiConsole.pickingMode = PickingMode.Ignore;

            VisualElement logPane = AddElement<VisualElement>(uiConsole, USS_PANE);
            VisualElement logPanel = AddElement<VisualElement>(logPane, USS_PANEL);
            logPanel.AddToClassList(USS_PANEL_LOG);
            AddPanelHeader(logPanel, "LOGS");
            uiLogScroll = AddElement<ScrollView>(logPanel, USS_LOG_SCROLL);
            uiLogScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            uiLogScroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnLogContentGeometryChanged);
            RegisterLogDragScroll(uiLogScroll.contentViewport);
            VisualElement logToolbar = AddElement<VisualElement>(logPane, USS_TOOLBAR);
            logToolbar.AddToClassList(USS_TOOLBAR_FOOTER);
            AddButton(logToolbar, "Clear", ClearLogs).AddToClassList(USS_BUTTON_WIDE);
            AddElement<VisualElement>(logToolbar, USS_SPACER).pickingMode = PickingMode.Ignore;
            AddButton(logToolbar, "A+", () => SetLogFontSize(logFontSize + FONT_SIZE_STEP));
            AddButton(logToolbar, "A-", () => SetLogFontSize(logFontSize - FONT_SIZE_STEP));

            VisualElement commandPane = AddElement<VisualElement>(uiConsole, USS_PANE);
            VisualElement inputRow = AddElement<VisualElement>(commandPane, USS_TOOLBAR);
            inputRow.AddToClassList(USS_TOOLBAR_HEADER);
            uiCommandField = AddElement<TextField>(inputRow, USS_COMMAND_FIELD);
            uiCommandField.textEdition.placeholder = "Enter command...";
            // Keep the caret at the end of a preset or history command instead of selecting all of it on focus.
            uiCommandField.textSelection.selectAllOnFocus = false;
            uiCommandField.textSelection.selectAllOnMouseUp = false;
            uiCommandField.RegisterCallback<KeyDownEvent>(OnCommandFieldKeyDown, TrickleDown.TrickleDown);
            uiCommandField.RegisterCallback<NavigationMoveEvent>(OnCommandFieldNavigationMove, TrickleDown.TrickleDown);
            uiSendButton = AddButton(inputRow, "Send", ExecuteCurrentCommand);
            uiSendButton.AddToClassList(USS_BUTTON_ACCENT);
            VisualElement commandPanel = AddElement<VisualElement>(commandPane, USS_PANEL);
            commandPanel.AddToClassList(USS_PANEL_COMMAND);
            AddPanelHeader(commandPanel, "COMMANDS");
            uiCommandHelp = AddElement<Label>(commandPanel, USS_COMMAND_HELP);
            uiPresetScroll = AddElement<ScrollView>(commandPanel, USS_PRESET_SCROLL);
            uiPresetScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // Preview window, aligned inside a full size layer by the preview anchor position.
            uiPreviewLayer = AddElement<VisualElement>(uiSafeArea, USS_PREVIEW_LAYER);
            uiPreviewLayer.pickingMode = PickingMode.Ignore;
            uiPreview = AddElement<VisualElement>(uiPreviewLayer, USS_PREVIEW);
            uiPreview.pickingMode = PickingMode.Ignore;
            uiCountVerbose = AddPreviewRow(uiPreview, "Verbose", USS_PREVIEW_VERBOSE);
            uiCountWarning = AddPreviewRow(uiPreview, "Warning", USS_PREVIEW_WARNING);
            uiCountError = AddPreviewRow(uiPreview, "Error", USS_PREVIEW_ERROR);
            uiCountException = AddPreviewRow(uiPreview, "Exception", USS_PREVIEW_EXCEPTION);

            // Screen messages are drawn on top of everything and never block the pointer.
            uiScreenMessages = AddElement<VisualElement>(uiSafeArea, USS_SCREEN_MESSAGES);
            uiScreenMessages.pickingMode = PickingMode.Ignore;
            foreach (ScreenMessage message in screenMessages)
                AddScreenMessageLabel(message);
        }

        private void AttachUI()
        {
            if (uiRoot == null || uiDocument == null)
                return;
            VisualElement documentRoot = uiDocument.rootVisualElement;
            if (documentRoot == null || uiRoot.parent == documentRoot)
                return;
            documentRoot.Add(uiRoot);
            isLogScrollPending = true;
        }

        private void DetachUI()
        {
            uiRoot?.RemoveFromHierarchy();
        }

        private void DestroyUI()
        {
            DetachUI();
            if (runtimePanelSettings != null)
            {
                if (uiDocument != null && uiDocument.panelSettings == runtimePanelSettings)
                    uiDocument.panelSettings = null;
                Destroy(runtimePanelSettings);
                runtimePanelSettings = null;
            }
        }

        private void ApplyUISettings()
        {
            if (uiRoot == null)
                return;
            ApplyPreviewLayout();
            TrimLogEntries();
            UpdateSafeArea(true);
        }

        private void UpdateUILayout()
        {
            if (uiRoot == null)
                return;
            UpdateSafeArea(false);
            if (isLogScrollPending)
                ScrollLogToBottom();
            if (isCommandFieldFocusPending && uiCommandField.panel != null && uiConsole.resolvedStyle.display == DisplayStyle.Flex)
            {
                isCommandFieldFocusPending = false;
                if (uiCommandField.enabledInHierarchy)
                    uiCommandField.Focus();
            }
        }

        private void UpdateSafeArea(bool force)
        {
            if (uiRoot.panel == null)
                return;
            Vector2 panelSize = uiRoot.layout.size;
            if (float.IsNaN(panelSize.x) || float.IsNaN(panelSize.y) || panelSize.x <= 0.0f || panelSize.y <= 0.0f)
                return;
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;
            if (!force && safeArea == cachedSafeArea && screenSize == cachedScreenSize && panelSize == cachedPanelSize && extendUI == cachedExtendUI)
                return;
            cachedSafeArea = safeArea;
            cachedScreenSize = screenSize;
            cachedPanelSize = panelSize;
            cachedExtendUI = extendUI;
            // Screen safe area is in pixels with bottom-left origin, the panel uses its own units with top-left origin.
            float scaleX = panelSize.x / screenSize.x;
            float scaleY = panelSize.y / screenSize.y;
            IStyle style = uiSafeArea.style;
            style.left = safeArea.xMin * scaleX + extendUI.left;
            style.right = (screenSize.x - safeArea.xMax) * scaleX + extendUI.right;
            style.top = (screenSize.y - safeArea.yMax) * scaleY + extendUI.top;
            style.bottom = safeArea.yMin * scaleY + extendUI.bottom;
        }

        private void OnSafeAreaGeometryChanged(GeometryChangedEvent evt)
        {
            Rect rect = evt.newRect;
            uiConsole.EnableInClassList(USS_CONSOLE_PORTRAIT, rect.width <= rect.height);
        }

        private void ApplyPreviewLayout()
        {
            // TextAnchor is ordered as Upper/Middle/Lower rows of Left/Center/Right columns.
            int anchor = (int)previewAnchorPosition;
            int row = anchor / 3;
            int column = anchor % 3;
            uiPreviewLayer.style.justifyContent = row == 0 ? Justify.FlexStart : row == 1 ? Justify.Center : Justify.FlexEnd;
            uiPreviewLayer.style.alignItems = column == 0 ? Align.FlexStart : column == 1 ? Align.Center : Align.FlexEnd;
            uiPreview.style.width = previewSize.x;
            uiPreview.style.height = previewSize.y;
        }

        private void RefreshVisibility()
        {
            if (uiRoot == null)
                return;
            SetDisplay(uiConsole, isShowingConsole);
            SetDisplay(uiPreview, isShowingPreview);
            SetDisplay(uiScreenMessages, isShowingScreenMessage);
        }

        private void OnConsoleVisibilityChanged()
        {
            RefreshVisibility();
            if (isShowingConsole)
                isLogScrollPending = true;
            else
            {
                isCommandFieldFocusPending = false;
                uiCommandField?.Blur();
            }
        }

        private void RequestCommandFieldFocus()
        {
            isCommandFieldFocusPending = true;
        }

        #endregion

        #region Log View

        private void AddLogEntry(string text)
        {
            if (uiLogScroll == null || logCapacity <= 0)
                return;
            VisualElement content = uiLogScroll.contentContainer;
            Label label;
            if (content.childCount >= logCapacity)
            {
                // Recycle the oldest entry instead of creating a new one.
                label = (Label)content[0];
                label.text = text;
                label.BringToFront();
            }
            else
            {
                label = AddElement<Label>(uiLogScroll, USS_LOG_ENTRY);
                label.text = text;
                label.style.fontSize = logFontSize;
            }
            TrimLogEntries();
            isLogScrollPending = true;
        }

        private void TrimLogEntries()
        {
            if (uiLogScroll == null)
                return;
            VisualElement content = uiLogScroll.contentContainer;
            int capacity = Mathf.Max(0, logCapacity);
            while (content.childCount > capacity)
                content.RemoveAt(0);
        }

        private void ClearLogEntries()
        {
            uiLogScroll?.Clear();
        }

        private void ScrollLogToBottom()
        {
            Scroller scroller = uiLogScroll.verticalScroller;
            // The high value is negative while the content is shorter than the viewport.
            scroller.value = Mathf.Max(scroller.lowValue, scroller.highValue);
        }

        private void OnLogContentGeometryChanged(GeometryChangedEvent evt)
        {
            // The scroll view updates its scroller range on the same event before this callback.
            if (!isLogScrollPending)
                return;
            ScrollLogToBottom();
            isLogScrollPending = false;
        }

        private void SetLogFontSize(int size)
        {
            logFontSize = Mathf.Clamp(size, MIN_FONT_SIZE, MAX_FONT_SIZE);
            foreach (VisualElement entry in uiLogScroll.contentContainer.Children())
                entry.style.fontSize = logFontSize;
            foreach (ScreenMessage message in screenMessages)
            {
                if (message.label != null)
                    message.label.style.fontSize = logFontSize;
            }
        }

        // Touch dragging is handled by the scroll view itself, this adds the same behaviour to the mouse.
        private void RegisterLogDragScroll(VisualElement viewport)
        {
            viewport.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse || evt.button != 0)
                    return;
                isLogDragging = true;
                logDragPosition = evt.position;
                viewport.CapturePointer(evt.pointerId);
            });
            viewport.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isLogDragging || !viewport.HasPointerCapture(evt.pointerId))
                    return;
                Vector2 position = evt.position;
                uiLogScroll.scrollOffset -= position - logDragPosition;
                logDragPosition = position;
            });
            viewport.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (viewport.HasPointerCapture(evt.pointerId))
                    viewport.ReleasePointer(evt.pointerId);
                isLogDragging = false;
            });
            viewport.RegisterCallback<PointerCaptureOutEvent>(_ => isLogDragging = false);
        }

        #endregion

        #region Command View

        private void RefreshCommandInputState()
        {
            bool hasCommand = commandData.Count > 0;
            uiCommandField?.SetEnabled(hasCommand);
            uiSendButton?.SetEnabled(hasCommand);
        }

        private void RebuildCommandPresets()
        {
            if (uiPresetScroll == null)
                return;
            uiPresetScroll.Clear();
            string cacheGroupName = null;
            foreach (CommandPreset commandPreset in commandPresets)
            {
                if (cacheGroupName != commandPreset.group)
                {
                    cacheGroupName = commandPreset.group;
                    if (!string.IsNullOrEmpty(cacheGroupName))
                    {
                        string group = cacheGroupName;
                        Button groupButton = AddButton(uiPresetScroll, group, () => ToggleCommandPresetGroup(group));
                        groupButton.AddToClassList(USS_PRESET_GROUP);
                        groupButton.EnableInClassList(USS_PRESET_GROUP_EXPANDED, commandDisplayingGroup == group);
                    }
                    else
                        AddElement<Label>(uiPresetScroll, USS_PRESET_GROUP).text = "Etc.";
                }
                if (!string.IsNullOrEmpty(commandDisplayingGroup) && commandDisplayingGroup == cacheGroupName ||
                    string.IsNullOrEmpty(cacheGroupName))
                {
                    CommandPreset preset = commandPreset;
                    AddButton(uiPresetScroll, preset.name, () => OnCommandPresetClicked(preset)).AddToClassList(USS_PRESET);
                }
            }
        }

        private void ToggleCommandPresetGroup(string group)
        {
            commandDisplayingGroup = commandDisplayingGroup == group ? null : group;
            RebuildCommandPresets();
        }

        private void OnCommandPresetClicked(CommandPreset commandPreset)
        {
            uiCommandHelp.text = commandPreset.description ?? string.Empty;
            if (commandPreset.executeImmediately)
                _ = ExecuteCommand(commandPreset.preset);
            else
            {
                SetCommandFieldText(commandPreset.preset);
                RequestCommandFieldFocus();
            }
        }

        private void ExecuteCurrentCommand()
        {
            string command = uiCommandField.value;
            uiCommandField.SetValueWithoutNotify(string.Empty);
            _ = ExecuteCommand(command);
        }

        private void SetCommandFieldText(string text)
        {
            if (text == null)
                return;
            uiCommandField.SetValueWithoutNotify(text);
            uiCommandField.SelectRange(text.Length, text.Length);
        }

        private void OnCommandFieldKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    SetCommandFieldText(GetPreviousExecuteLog());
                    break;
                case KeyCode.DownArrow:
                    SetCommandFieldText(GetNextExecuteLog());
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    ExecuteCurrentCommand();
                    break;
                default:
                    return;
            }
            evt.StopPropagation();
            uiCommandField.focusController?.IgnoreEvent(evt);
        }

        // Up and down arrows browse the command history, so they must not move the focus away.
        private void OnCommandFieldNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Up && evt.direction != NavigationMoveEvent.Direction.Down)
                return;
            evt.StopPropagation();
            uiCommandField.focusController?.IgnoreEvent(evt);
        }

        #endregion

        #region Preview and Screen Message View

        private void RefreshPreviewCounts()
        {
            if (uiCountVerbose == null)
                return;
            uiCountVerbose.text = countLogVerbose.ToString("N0");
            uiCountWarning.text = countLogWarning.ToString("N0");
            uiCountError.text = countLogError.ToString("N0");
            uiCountException.text = countLogException.ToString("N0");
        }

        private void AddScreenMessageLabel(ScreenMessage message)
        {
            if (uiScreenMessages == null)
                return;
            message.label = AddElement<Label>(uiScreenMessages, USS_SCREEN_MESSAGE);
            message.label.text = message.text;
            message.label.pickingMode = PickingMode.Ignore;
            message.label.style.fontSize = logFontSize;
        }

        #endregion

        #region Element Helpers

        private static T AddElement<T>(VisualElement parent, string className) where T : VisualElement, new()
        {
            T element = new T();
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        private static Button AddButton(VisualElement parent, string text, Action clicked)
        {
            Button button = new Button(clicked) { text = text };
            button.AddToClassList(USS_BUTTON);
            parent.Add(button);
            return button;
        }

        private static void AddPanelHeader(VisualElement parent, string title)
        {
            VisualElement header = AddElement<VisualElement>(parent, USS_PANEL_HEADER);
            AddElement<Label>(header, USS_PANEL_TITLE).text = title;
        }

        private static Label AddPreviewRow(VisualElement parent, string title, string modifierClassName)
        {
            VisualElement row = AddElement<VisualElement>(parent, USS_PREVIEW_ROW);
            row.AddToClassList(modifierClassName);
            row.pickingMode = PickingMode.Ignore;
            Label titleLabel = AddElement<Label>(row, USS_PREVIEW_LABEL);
            titleLabel.text = title;
            titleLabel.pickingMode = PickingMode.Ignore;
            Label countLabel = AddElement<Label>(row, USS_PREVIEW_LABEL);
            countLabel.pickingMode = PickingMode.Ignore;
            return countLabel;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion
    }
}
