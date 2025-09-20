using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Scripting.Python
{
    partial class PythonScriptsWindow
    {
        private VisualElement _scriptOptionalButtonContainer;
        private TextField _scriptTextField;
        private TextField _outputTextField;


        private void CreateGUI()
        {
            #region Main Toolbar

            // main-toolbar
            Toolbar mainToolbar = new Toolbar
            {
                name = "main-toolbar"
            };
            rootVisualElement.Add(mainToolbar);

            #endregion


            // horizontal split view
            TwoPaneSplitView horizontalSplitView = new TwoPaneSplitView(0, 200, TwoPaneSplitView.Orientation.Horizontal, 0);
            rootVisualElement.Add(horizontalSplitView);


            #region Script Tree

            // script-tree-container
            VisualElement scriptTreeContainer = new VisualElement
            {
                name = "script-tree-container"
            };
            horizontalSplitView.Add(scriptTreeContainer);

            #endregion


            // vertical split view
            TwoPaneSplitView verticalSplitView = new TwoPaneSplitView(1, 200, TwoPaneSplitView.Orientation.Vertical, 0);
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
            ToolbarButton executeScriptButton = CreateToolbarButton("Execute", "execute-script-button", ExecutePythonScript);
            scriptEditorToolbar.Add(executeScriptButton);


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
                style =
                {
                    justifyContent = Justify.SpaceBetween,
                }
            };
            outputContainer.Add(outputToolbar);

            // output-label
            Label outputLabel = CreateToolbarLabel("Output", "output-label");
            outputToolbar.Add(outputLabel);

            // clear-output-button
            ToolbarButton clearOutputButton = CreateToolbarButton("Clear", "clear-output-button", ClearPythonOutput);
            outputToolbar.Add(clearOutputButton);

            // output-scroll-view
            ScrollView outputScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "output-scroll-view",
                style =
                {
                    flexGrow = 1,
                }
            };
            outputScrollView.Q(className: ScrollView.contentUssClassName).style.minHeight = Length.Percent(100);
            outputContainer.Add(outputScrollView);

            // output-text-field
            _outputTextField = new TextField
            {
                multiline = true,
                name = "output-text-field",
                isReadOnly = true,
                style =
                {
                    flexGrow = 1,
                }
            };
            _outputTextField.Q(className: TextField.inputUssClassName).style.unityTextAlign = TextAnchor.UpperLeft;
            outputScrollView.Add(_outputTextField);

            #endregion
        }

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
}