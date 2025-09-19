using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Scripting.Python.Samples
{
    public class CustomPythonEditorWindow : PythonEditorWindow
    {
        private const string WINDOW_ID = "PythonEditorWindowSample";

        [MenuItem("Samples/Python Scripting/Editor Window/Default Editor Window")]
        public static void OpenDefault()
        {
            PythonEditorWindow window = GetWindowByID(WINDOW_ID);
            if (window)
                window.Close();

            string pythonScriptPath = GetPythonScriptPath();
            Open(new GUIContent("Python Window(Default)"), WINDOW_ID, pythonScriptPath);
        }

        [MenuItem("Samples/Python Scripting/Editor Window/Custom Editor Window")]
        public static void OpenCustom()
        {
            PythonEditorWindow window = GetWindowByID(WINDOW_ID);
            if (window)
                window.Close();

            string pythonScriptPath = GetPythonScriptPath();
            Open<CustomPythonEditorWindow>(new GUIContent("Python Window(Custom)"), WINDOW_ID, pythonScriptPath);
        }

        /// <inheritdoc />
        protected override void OnGUI()
        {
            GUILayout.Label("Custom Window Type(Draw from C#)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            base.OnGUI();
        }

        private static string GetPythonScriptPath([CallerFilePath] string callerFilePath = "")
        {
            string callerFolder = Path.GetDirectoryName(callerFilePath);
            Assert.IsNotNull(callerFolder);
            string pythonScriptPath = Path.Combine(callerFolder, "editor_window_sample.py");
            return pythonScriptPath;
        }
    }
}