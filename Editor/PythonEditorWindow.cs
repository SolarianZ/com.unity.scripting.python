using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Scripting.Python
{
    public class PythonEditorWindow : EditorWindow, IHasCustomMenu
    {
        public static PythonEditorWindow Open(GUIContent windowTitle, string windowID, string pythonScriptPath,
            IDictionary<string, object> userData = null)
        {
            if (windowID == null)
                throw new InvalidOperationException("Window ID cannot be null.");
            if (pythonScriptPath == null)
                throw new InvalidOperationException("Python script path cannot be null.");
            if (!File.Exists(pythonScriptPath))
                throw new InvalidOperationException("Python script path does not exist.");

            PythonEditorWindow window = GetWindowByID(windowID);
            if (!window)
                window = CreateWindow<PythonEditorWindow>();
            window.titleContent = windowTitle ?? new GUIContent(windowID);
            window.PyInit(windowID, pythonScriptPath, userData);
            return window;
        }

        public static T Open<T>(GUIContent windowTitle, string windowID, string pythonScriptPath,
            IDictionary<string, object> userData = null) where T : PythonEditorWindow
        {
            if (windowID == null)
                throw new InvalidOperationException("Window ID cannot be null.");
            if (pythonScriptPath == null)
                throw new InvalidOperationException("Python script path cannot be null.");
            if (!File.Exists(pythonScriptPath))
                throw new InvalidOperationException("Python script path does not exist.");

            PythonEditorWindow window = GetWindowByID(windowID);
            if (!window)
                window = CreateWindow<PythonEditorWindow>();
            else if (!(window is T))
                throw new InvalidOperationException($"Window ID '{windowID}' is already used by another window type '{window.GetType().FullName}'.");

            window.titleContent = windowTitle ?? new GUIContent(windowID);
            window.PyInit(windowID, pythonScriptPath, userData);
            return (T)window;
        }

        public static PythonEditorWindow GetWindowByID(string windowID)
        {
            if (windowID == null)
                return null;

            PythonEditorWindow window = Resources.FindObjectsOfTypeAll<PythonEditorWindow>()
                                                 .FirstOrDefault(wnd => wnd.WindowID == windowID);
            return window;
        }


        [SerializeField, HideInInspector]
        private string _windowID;
        [SerializeField, HideInInspector]
        private string _pythonScriptPath;

        public string WindowID => _windowID;
        public string PythonScriptPath => _pythonScriptPath;
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly IDictionary<string, object> UserData = new Dictionary<string, object>();

        public Action PyInitHandler;
        public Action PyUpdateHandler;
        public Action PyOnGUIHandler;
        public Action PyCleanHandler;


        private void PyInit(string windowID, string pythonScriptPath, IDictionary<string, object> userData)
        {
            _windowID = windowID;
            _pythonScriptPath = pythonScriptPath;

            if (userData != null)
            {
                UserData.Clear();
                foreach (KeyValuePair<string, object> kvp in userData)
                {
                    UserData.Add(kvp.Key, kvp.Value);
                }
            }

            PythonRunner.EnsureInitialized();
            PythonRunner.RunFile(pythonScriptPath);
            PyInitHandler?.Invoke();
        }

        protected virtual void OnEnable()
        {
            // 编译后恢复
            if (WindowID != null && PythonScriptPath != null)
            {
                PythonRunner.EnsureInitialized();
                PythonRunner.RunFile(PythonScriptPath);
                PyInitHandler?.Invoke();
            }
        }

        protected virtual void Update()
        {
            PyUpdateHandler?.Invoke();
        }

        protected virtual void OnGUI()
        {
            PyOnGUIHandler?.Invoke();
        }

        protected virtual void OnDisable()
        {
            PyCleanHandler?.Invoke();
        }

        /// <inheritdoc />
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload Python Script"), false, () =>
            {
                PyCleanHandler?.Invoke();
                PythonRunner.RunFile(PythonScriptPath);
                PyInitHandler?.Invoke();
            });
        }
    }
}