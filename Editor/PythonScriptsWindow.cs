using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Python.Runtime;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UDebug = UnityEngine.Debug;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Python Scripting/Python Scripts Window", false, 1)]
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


        #region Unity  Events

        private void OnEnable()
        {
            PythonStdoutBroadcaster.OnPythonStdout -= RecordPythonOutput;
            PythonStdoutBroadcaster.OnPythonStdout += RecordPythonOutput;
        }

        private void OnDisable()
        {
            PythonStdoutBroadcaster.OnPythonStdout -= RecordPythonOutput;
            PythonRunner.DisposeScope();

            SaveScriptTreeViewState();
        }

        #endregion


        #region Toolbar

        private void OpenPythonScriptFolder()
        {
            if (!Directory.Exists(PythonScriptFolder))
                return;

            EditorUtility.OpenWithDefaultApp(PythonScriptFolder);
            // Process.Start(new ProcessStartInfo("code", PythonScriptFolder)
            // {
            //     FileName = "code",
            //     Arguments = PythonScriptFolder,
            //     WindowStyle = ProcessWindowStyle.Hidden,
            // });
        }

        private void RefreshPythonScripts()
        {
            _scriptTreeContainer.SetScriptFolder(PythonScriptFolder, true);
            OnPythonScriptSelected(_scriptPath);
        }

        private void OpenPythonSettings()
        {
            SettingsService.OpenProjectSettings(PythonSettings.SettingsPath);
        }

        #endregion


        #region Python Script

        private void OnPythonScriptSelected(string scriptPath)
        {
            if (!_isGUICreated)
                return;

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
                UDebug.LogException(ex);
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
            if (_clearOutputOnExecuteToggle.value)
                ClearPythonOutput();

            if (File.Exists(_scriptPath))
            {
                try
                {
                    PythonRunner.RunFile(_scriptPath);
                }
                catch (PythonException pyEx)
                {
                    UDebug.LogException(pyEx);
                    string pyError = ExtractPythonError(pyEx);
                    RecordPythonOutput(pyError);
                }
                catch (Exception ex)
                {
                    UDebug.LogException(ex);
                }

                return;
            }

            Assert.IsFalse(_scriptTextField.isReadOnly);
            string code = _scriptTextField.value;
            try
            {
                PythonRunner.RunString(code, "__main__");
            }
            catch (PythonException pyEx)
            {
                UDebug.LogException(pyEx);
                string pyError = ExtractPythonError(pyEx);
                RecordPythonOutput(pyError);
            }
            catch (Exception ex)
            {
                UDebug.LogException(ex);
            }
        }

        private static string ExtractPythonError(PythonException pyEx)
        {
            const string PyNetCallSite = "at Python.Runtime.PythonException.ThrowLastAsClrException";
            int pyNetCallSiteIndex = pyEx.StackTrace.IndexOf(PyNetCallSite, StringComparison.Ordinal);
            if (pyNetCallSiteIndex == -1)
                return pyEx.Message;

            string pyStackTrace = pyEx.StackTrace.Substring(0, pyNetCallSiteIndex).TrimEnd();
            string pyError = $"{pyEx.Message}\n{pyStackTrace}";
            return pyError;
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

        #endregion


        #region Python Output

        private static readonly int _pythonOutputsCapacity = 100 * 2; // 其中一半是Python自动输出的换行符
        private readonly Queue<string> _pythonOutputs = new Queue<string>(_pythonOutputsCapacity);
        private readonly StringBuilder _pythonOutputBuilder = new StringBuilder();
        [NonSerialized] // 避免重编代码后恢复窗口时出发滚动到底，导致文本框异常上移
        private bool _pythonOutputChanged;


        private void ClearPythonOutput()
        {
            _pythonOutputs.Clear();
            _pythonOutputBuilder.Clear();
            _outputTextField.SetValueWithoutNotify(null);
        }

        private void RecordPythonOutput(string content)
        {
            if (_pythonOutputs.Count == _pythonOutputsCapacity)
            {
                string oldOutput = _pythonOutputs.Dequeue();
                _pythonOutputBuilder.Remove(0, oldOutput.Length);

                if (_pythonOutputs.Peek() == "\n")
                {
                    oldOutput = _pythonOutputs.Dequeue();
                    _pythonOutputBuilder.Remove(0, oldOutput.Length);
                }
            }

            _pythonOutputs.Enqueue(content);
            _pythonOutputBuilder.Append(content); // Python会自动输出一次单独的换行符，所以不要AppendLine

            _outputTextField.SetValueWithoutNotify(_pythonOutputBuilder.ToString());
            _pythonOutputChanged = true;
        }

        private void ScrollPythonOutputToBottomIfNeeded()
        {
            if (!_pythonOutputChanged)
                return;

            _outputScrollView.verticalScroller.value = _outputScrollView.verticalScroller.highValue;
            _pythonOutputChanged = false;
        }

        #endregion


        /// <inheritdoc />
        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Force Re-Compile C# Scripts"), false, () =>
            {
                AssetDatabase.Refresh();
                CompilationPipeline.RequestScriptCompilation();
            });
        }
    }
}