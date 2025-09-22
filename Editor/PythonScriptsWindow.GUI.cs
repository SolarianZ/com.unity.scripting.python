using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor.Scripting.Python
{
    partial class PythonScriptsWindow
    {
        private ScriptTreeViewContainer _scriptTreeContainer;
        private ToolbarButton _executeScriptButton;
        private VisualElement _scriptOptionalButtonContainer;
        private TextField _scriptTextField;
        private ScrollView _outputScrollView;
        private ToolbarToggle _clearOutputOnExecuteToggle;
        private TextField _outputTextField;
        [SerializeField, HideInInspector]
        private bool _isFirstTimeCreateGUI = true;


        private void CreateGUI()
        {
            #region Main Toolbar

            // main-toolbar
            Toolbar mainToolbar = new Toolbar
            {
                name = "main-toolbar"
            };
            rootVisualElement.Add(mainToolbar);

            // refresh-python-scripts-button
            ToolbarButton refreshPythonScriptsButton = CreateToolbarButton("Refresh Python Scripts", "refresh-python-scripts-button", RefreshPythonScripts);
            mainToolbar.Add(refreshPythonScriptsButton);

            // open-script-folder-button
            mainToolbar.Add(new ToolbarSpacer());
            ToolbarButton openScriptFolderButton = CreateToolbarButton("Open Script Folder", "open-script-folder-button", OpenPythonScriptFolder);
            mainToolbar.Add(openScriptFolderButton);

            // main-toolbar-placeholder
            mainToolbar.Add(new VisualElement
            {
                name = "main-toolbar-placeholder",
                style = { flexGrow = 1 }
            });

            // open-python-settings-button
            mainToolbar.Add(new ToolbarSpacer());
            ToolbarButton openPythonSettingsButton = CreateToolbarButton("Open Python Settings", "open-python-settings-button", OpenPythonSettings);
            mainToolbar.Add(openPythonSettingsButton);

            #endregion


            // horizontal split view
            TwoPaneSplitView horizontalSplitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(horizontalSplitView);


            #region Script Tree

            // script-tree-container
            _scriptTreeContainer = new ScriptTreeViewContainer(_scriptTreeViewState)
            {
                name = "script-tree-container"
            };
            _scriptTreeContainer.ScriptSelected += OnPythonScriptSelected;
            _scriptTreeContainer.SetScriptFolder(PythonScriptFolder, false);
            horizontalSplitView.Add(_scriptTreeContainer);

            #endregion


            // vertical split view
            TwoPaneSplitView verticalSplitView = new TwoPaneSplitView(1, 200, TwoPaneSplitViewOrientation.Vertical);
            horizontalSplitView.Add(verticalSplitView);


            #region Python Script

            // script-editor-container
            VisualElement scriptEditorContainer = new VisualElement
            {
                name = "script-editor-container"
            };
            verticalSplitView.Add(scriptEditorContainer);

            // script-editor-toolbar
            Toolbar scriptEditorToolbar = new Toolbar
            {
                name = "script-editor-toolbar"
            };
            scriptEditorContainer.Add(scriptEditorToolbar);

            // script-editor-label
            Label scriptEditorLabel = CreateToolbarLabel("Script", "script-editor-label");
            scriptEditorToolbar.Add(scriptEditorLabel);

            // execute-script-button
            scriptEditorToolbar.Add(new ToolbarSpacer());
            _executeScriptButton = CreateToolbarButton("Execute", "execute-script-button", ExecutePythonScript);
            scriptEditorToolbar.Add(_executeScriptButton);


            #region Optional Buttons

            // script-optional-button-container
            scriptEditorToolbar.Add(new ToolbarSpacer());
            _scriptOptionalButtonContainer = new VisualElement
            {
                name = "script-optional-button-container",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    justifyContent = Justify.SpaceBetween,
                }
            };
            scriptEditorToolbar.Add(_scriptOptionalButtonContainer);


            #region Save & Load Buttons

            // script-save-load-button-container
            VisualElement scriptSaveLoadButtonContainer = new VisualElement
            {
                name = "script-save-load-button-container",
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };
            _scriptOptionalButtonContainer.Add(scriptSaveLoadButtonContainer);

            // save-script-button
            ToolbarButton saveScriptButton = CreateToolbarButton("Save", "save-script-button", SavePythonScript);
            scriptSaveLoadButtonContainer.Add(saveScriptButton);

            // load-script-button
            scriptSaveLoadButtonContainer.Add(new ToolbarSpacer());
            ToolbarButton loadScriptButton = CreateToolbarButton("Load", "load-script-button", LoadPythonScript);
            scriptSaveLoadButtonContainer.Add(loadScriptButton);

            #endregion


            // clear-script-button
            ToolbarButton clearScriptButton = CreateToolbarButton("Clear", "clear-script-button", ClearPythonScript);
            _scriptOptionalButtonContainer.Add(clearScriptButton);

            #endregion


            // script-scroll-view
            ScrollView scriptScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "script-scroll-view",
                style =
                {
                    flexGrow = 1,
                }
            };
            scriptScrollView.Q(className: ScrollView.contentUssClassName).style.minHeight = Length.Percent(100);
            scriptEditorContainer.Add(scriptScrollView);

            // script-text-field
            _scriptTextField = new TextField
            {
                multiline = true,
                name = "script-text-field",
                style =
                {
                    flexGrow = 1,
                }
            };
            _scriptTextField.Q(className: TextField.inputUssClassName).style.unityTextAlign = TextAnchor.UpperLeft;
            _scriptTextField.RegisterValueChangedCallback(evt => _scriptEditorTextCache = evt.newValue);
            scriptScrollView.Add(_scriptTextField);

            #endregion


            #region Python Output

            // output-container
            VisualElement outputContainer = new VisualElement
            {
                name = "output-container"
            };
            verticalSplitView.Add(outputContainer);

            // output-toolbar
            Toolbar outputToolbar = new Toolbar
            {
                name = "output-toolbar",
            };
            outputContainer.Add(outputToolbar);

            // output-label
            Label outputLabel = CreateToolbarLabel("Output", "output-label");
            outputToolbar.Add(outputLabel);

            // output-toolbar-placeholder
            outputToolbar.Add(new VisualElement
            {
                name = "output-toolbar-placeholder",
                style = { flexGrow = 1 }
            });

            // clear-output-on-execute-toggle
            _clearOutputOnExecuteToggle = new ToolbarToggle
            {
                name = "clear-output-on-execute-toggle",
                viewDataKey = $"{GetType().AssemblyQualifiedName}::clear-output-on-execute-toggle",
                text = "Clear on Execute",
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            outputToolbar.Add(_clearOutputOnExecuteToggle);

            // clear-output-button
            ToolbarButton clearOutputButton = CreateToolbarButton("Clear", "clear-output-button", ClearPythonOutput);
            outputToolbar.Add(clearOutputButton);

            // output-scroll-view
            _outputScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "output-scroll-view",
                style =
                {
                    flexGrow = 1,
                }
            };
            _outputScrollView.Q(className: ScrollView.contentUssClassName).style.minHeight = Length.Percent(100);
            outputContainer.Add(_outputScrollView);

            // output-text-field
            _outputTextField = new TextField
            {
                multiline = true,
                name = "output-text-field",
                isReadOnly = true,
                style =
                {
                    flexGrow = 1,
                    whiteSpace = WhiteSpace.Normal,
                }
            };
            _outputTextField.Q(className: TextField.inputUssClassName).style.unityTextAlign = TextAnchor.UpperLeft;
            _outputTextField.RegisterCallback<GeometryChangedEvent>(evt => EditorApplication.delayCall += ScrollPythonOutputToBottomIfNeeded);
            _outputScrollView.Add(_outputTextField);

            #endregion


            // Init GUI
            InitGUIState();
        }

        private void InitGUIState()
        {
            if (_isFirstTimeCreateGUI)
                _scriptTreeContainer.SelectScriptEditor();

            _isFirstTimeCreateGUI = false;
        }


        #region Script Tree View State

        private static string ScriptTreeViewStateKey => $"{typeof(PythonScriptsWindow).AssemblyQualifiedName}::{nameof(ScriptTreeViewStateKey)}";

        [NonSerialized]
        private TreeViewState _scriptTreeViewState;


        private void SaveScriptTreeViewState()
        {
            string json = _scriptTreeViewState == null ? null : JsonUtility.ToJson(_scriptTreeViewState);
            EditorUserSettings.SetConfigValue(ScriptTreeViewStateKey, json);
        }

        private void LoadScriptTreeViewState()
        {
            string json = EditorUserSettings.GetConfigValue(ScriptTreeViewStateKey);
            _scriptTreeViewState = (string.IsNullOrEmpty(json)
                ? new TreeViewState()
                : JsonUtility.FromJson<TreeViewState>(json)) ?? new TreeViewState();
        }

        #endregion


        private static Label CreateToolbarLabel(string text, string name)
        {
            Label toolbarLabel = new Label(text)
            {
                name = name,
                style =
                {
                    marginLeft = 6,
                    marginRight = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            return toolbarLabel;
        }

        private static ToolbarButton CreateToolbarButton(string text, string name, Action onClick)
        {
            ToolbarButton toolbarButton = new ToolbarButton(onClick)
            {
                text = text,
                name = name,
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            return toolbarButton;
        }
    }

    class ScriptTreeViewContainer : IMGUIContainer
    {
        public Action<string> ScriptSelected;

        private readonly ScriptTreeView _treeView;
        private readonly GUILayoutOption[] _layoutOptions =
        {
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true),
        };


        public ScriptTreeViewContainer(TreeViewState treeViewState)
        {
            _treeView = new ScriptTreeView(treeViewState);
            _treeView.ScriptSelected += scriptPath => ScriptSelected?.Invoke(scriptPath);
            _treeView.Reload();

            onGUIHandler = OnGUI;
        }

        public void SelectScriptEditor()
        {
            _treeView.SelectScriptEditor();
        }

        public void SetScriptFolder(string scriptFolder, bool keepSelection)
        {
            _treeView.SetScriptFolder(scriptFolder, keepSelection);
        }

        public void Refresh()
        {
            _treeView.Refresh();
        }

        private void OnGUI()
        {
            Rect rect = EditorGUILayout.GetControlRect(_layoutOptions);
            _treeView.OnGUI(rect);
        }


        class ScriptTreeView : TreeView
        {
            private const int ScriptEditorID = 1;

            public Action<string> ScriptSelected;

            private readonly Dictionary<int, string> _id2Path = new Dictionary<int, string>();
            private string _scriptFolder;


            /// <inheritdoc />
            public ScriptTreeView(TreeViewState state) : base(state) { }

            public void SelectScriptEditor()
            {
                SetSelection(new int[] { ScriptEditorID }, TreeViewSelectionOptions.FireSelectionChanged);
            }

            public void SetScriptFolder(string scriptFolder, bool keepSelection)
            {
                IList<int> selection = keepSelection ? GetSelection() : null;
                _scriptFolder = scriptFolder;
                SetSelection(Array.Empty<int>());
                Reload();

                if (keepSelection)
                    SetSelection(selection);
            }

            public void Refresh()
            {
                IList<int> selection = GetSelection();
                SetSelection(Array.Empty<int>());
                Reload();
                SetSelection(selection);
            }

            /// <inheritdoc />
            protected override TreeViewItem BuildRoot()
            {
                _id2Path.Clear();

                // Root
                TreeViewItem root = new TreeViewItem(ScriptEditorID - 1, -1);

                // Script Editor
                TreeViewItem scriptEditorItem = new TreeViewItem(ScriptEditorID)
                {
                    displayName = "Script Editor",
                };
                _id2Path.Add(ScriptEditorID, string.Empty);
                root.AddChild(scriptEditorItem);

                int nextID = ScriptEditorID + 1;
                BuildScriptHierarchy(_id2Path, root, _scriptFolder, ref nextID);

                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            private static void BuildScriptHierarchy(Dictionary<int, string> id2Path, TreeViewItem parent, string folder, ref int nextID)
            {
                if (!Directory.Exists(folder))
                    return;

                string[] pyFiles = Directory.GetFiles(folder, "*.py", SearchOption.TopDirectoryOnly);
                if (pyFiles.Length == 0)
                {
                    bool hasScripts = Directory.EnumerateFiles(folder, "*.py", SearchOption.AllDirectories).Any();
                    if (!hasScripts)
                        return;
                }

                // Folder item
                string folderName = Path.GetFileNameWithoutExtension(folder);
                TreeViewItem folderItem = new TreeViewItem(nextID)
                {
                    displayName = folderName
                };
                // id2Path.Add(nextID, folder);
                parent.AddChild(folderItem);
                nextID++;

                // Sub folders
                string[] subFolders = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly);
                foreach (string subFolder in subFolders)
                {
                    BuildScriptHierarchy(id2Path, folderItem, subFolder, ref nextID);
                }

                // Direct files
                foreach (string pyFile in pyFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(pyFile);
                    TreeViewItem fileItem = new TreeViewItem(nextID)
                    {
                        displayName = fileName
                    };
                    id2Path.Add(nextID, pyFile);
                    folderItem.AddChild(fileItem);
                    nextID++;
                }
            }

            /// <inheritdoc />
            protected override bool CanMultiSelect(TreeViewItem item) => false;

            /// <inheritdoc />
            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);

                if (selectedIds == null || selectedIds.Count == 0)
                {
                    ScriptSelected?.Invoke(null);
                }
                else
                {
                    int scriptID = selectedIds[0];
                    _id2Path.TryGetValue(scriptID, out string scriptPath);
                    ScriptSelected?.Invoke(scriptPath);
                }
            }
        }
    }
}