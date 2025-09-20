using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow : EditorWindow
    {
        [MenuItem("Tools/Python Scripting/Python Scripts Window")]
        public static void Open()
        {
            PythonScriptsWindow window = GetWindow<PythonScriptsWindow>();
            window.titleContent = new GUIContent("Python Scripts");
            window.minSize = new Vector2(550, 300);
            window.Show();
        }


        internal static string PythonScriptFolder => PythonSettings.GetPythonScriptFolder();

        internal bool IsScriptEditorSelected => _scriptPath == string.Empty;
        internal bool IsExecutableSelected => _scriptPath != null;

        [SerializeReference, HideInInspector] // can be null
        private string _scriptPath;
        [SerializeField, HideInInspector]
        private string _scriptEditorTextCache;


        private void OpenPythonScriptFolder()
        {
            if (!Directory.Exists(PythonScriptFolder))
                return;

            EditorUtility.OpenWithDefaultApp(PythonScriptFolder);
        }

        private void RefreshPythonScripts()
        {
            _scriptTreeContainer.Refresh();
        }

        private void OpenPythonSettings()
        {
            SettingsService.OpenProjectSettings(PythonSettings.SettingsPath);
        }

        private void OnPythonScriptSelected(string scriptPath)
        {
            _scriptPath = scriptPath;
            _executeScriptButton.SetEnabled(IsExecutableSelected);

            if (IsScriptEditorSelected)
            {
                _scriptTextField.SetValueWithoutNotify(_scriptEditorTextCache);
                _scriptTextField.isReadOnly = false;
                _scriptOptionalButtonContainer.style.display = DisplayStyle.Flex;
                return;
            }

            if (!IsExecutableSelected)
            {
                _scriptTextField.SetValueWithoutNotify(null);
                _scriptTextField.isReadOnly = true;
                _scriptOptionalButtonContainer.style.display = DisplayStyle.None;
                return;
            }

            try
            {
                string pythonScript = File.ReadAllText(_scriptPath);
                _scriptTextField.SetValueWithoutNotify(pythonScript);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _scriptTextField.SetValueWithoutNotify("Exception occurred while reading script file!\n" +
                    "\n" +
                    $"Script file path:\n{_scriptPath}\n" +
                    "\n" +
                    $"Exception:\n{ex.Message}");
            }
            finally
            {
                _scriptTextField.isReadOnly = true;
                _scriptOptionalButtonContainer.style.display = DisplayStyle.None;
            }
        }

        private void ExecutePythonScript()
        {
            if (File.Exists(_scriptPath))
            {
                PythonRunner.RunFile(_scriptPath);
                return;
            }

            Assert.IsFalse(_scriptTextField.isReadOnly);
            string code = _scriptTextField.value;
            PythonRunner.RunString(code, "__main__");
        }

        private void SavePythonScript()
        {
            Assert.IsFalse(_scriptTextField.isReadOnly);

            string scriptPath = EditorUtility.SaveFilePanel("Save Python Script", Application.dataPath, "", "py");
            if (string.IsNullOrEmpty(scriptPath))
                return;

            File.WriteAllText(scriptPath, _scriptTextField.value);
        }

        private void LoadPythonScript()
        {
            Assert.IsFalse(_scriptTextField.isReadOnly);

            string scriptPath = EditorUtility.OpenFilePanel("Load Python Script", Application.dataPath, "py");
            if (string.IsNullOrEmpty(scriptPath))
                return;

            string pythonScript = File.ReadAllText(scriptPath);
            _scriptEditorTextCache = pythonScript;
            _scriptTextField.SetValueWithoutNotify(pythonScript);
        }

        private void ClearPythonScript()
        {
            Assert.IsFalse(_scriptTextField.isReadOnly);
            _scriptTextField.SetValueWithoutNotify(null);
        }

        private void ClearPythonOutput()
        {
            _outputTextField.SetValueWithoutNotify(null);
        }
    }
}