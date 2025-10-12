using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Python.Runtime;
using UnityEngine;

namespace UnityEditor.Scripting.Python
{
    public class PythonEditorWindow : EditorWindow, IHasCustomMenu
    {
        public static PythonEditorWindow Open(GUIContent windowTitle, string windowID, string pythonScriptPath,
            IDictionary<string, object> userData = null, PyModule scope = null, bool disposeScopeOnDestroy = false)
        {
            PythonEditorWindow window = Open<PythonEditorWindow>(windowTitle, windowID, pythonScriptPath, userData, scope, disposeScopeOnDestroy);
            return window;
        }

        public static T Open<T>(GUIContent windowTitle, string windowID, string pythonScriptPath,
            IDictionary<string, object> userData = null, PyModule scope = null, bool disposeScopeOnDestroy = false)
            where T : PythonEditorWindow
        {
            PythonEditorWindow window = GetOrCreate<T>(windowTitle, windowID, pythonScriptPath, userData, scope, disposeScopeOnDestroy);
            window.Show();
            return (T)window;
        }

        /// <summary>
        /// Get or create a PythonEditorWindow of specified ID and type.
        /// This method does not automatically invoke the window's Show method, so you can decide how the window is shown (e.g., Normal, Utility, Modal, etc.).
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static T GetOrCreate<T>(GUIContent windowTitle, string windowID, string pythonScriptPath,
            IDictionary<string, object> userData = null, PyModule scope = null, bool disposeScopeOnDestroy = false)
            where T : PythonEditorWindow
        {
            if (windowID == null)
                throw new InvalidOperationException("Window ID cannot be null.");
            if (pythonScriptPath == null)
                throw new InvalidOperationException("Python script path cannot be null.");
            if (!File.Exists(pythonScriptPath))
                throw new InvalidOperationException($"Python script path does not exist: {pythonScriptPath}");

            PythonEditorWindow window = GetWindowByID(windowID);
            if (!window)
                window = CreateInstance<T>();
            else if (!(window is T))
                throw new InvalidOperationException($"Window ID '{windowID}' is already used by another window type '{window.GetType().FullName}'.");

            window.titleContent = windowTitle ?? new GUIContent(windowID);
            window.PyInit(windowID, pythonScriptPath, userData, scope, disposeScopeOnDestroy);
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
        private IDictionary<string, object> _userData;
        private PyModule _scope;
        [SerializeField, HideInInspector]
        private bool _disposeScopeOnDestroy;

        public string WindowID => _windowID;
        public string PythonScriptPath => _pythonScriptPath;
        public IDictionary<string, object> UserData
        {
            get
            {
                _userData = _userData ?? new Dictionary<string, object>();
                return _userData;
            }
        }

        public Action PyInitHandler; // OnEnable
        public Action PyCreateGUIHandler;
        public Action PyUpdateHandler;
        public Action PyOnGUIHandler;
        public Action<Rect> PyShowButtonHandler;
        public Action PyOnFocusHandler;
        public Action PyOnLostFocusHandler;
        public Action PyOnDisableHandler;
        public Action PyCleanHandler; // OnDestroy
#if UNITY_2020_2_OR_NEWER
        public Action PySaveChangesHandler;
        public Action PyDiscardChangesHandler;
#endif


        private void PyInit(string windowID, string pythonScriptPath,
            IDictionary<string, object> userData, PyModule scope, bool disposeScopeOnDestroy)
        {
            _windowID = windowID;
            _pythonScriptPath = pythonScriptPath;
            _userData = userData ?? _userData;
            _scope = scope;
            _disposeScopeOnDestroy = disposeScopeOnDestroy;

            PythonBridge.ExecuteFile(pythonScriptPath, scope);
            PyInitHandler?.Invoke();
        }

        protected virtual void OnEnable()
        {
            // 编译后恢复
            if (WindowID != null && PythonScriptPath != null)
            {
                PythonBridge.ExecuteFile(PythonScriptPath, _scope);
                PyInitHandler?.Invoke();
            }
        }

        protected virtual void CreateGUI() => PyCreateGUIHandler?.Invoke();
        protected virtual void Update() => PyUpdateHandler?.Invoke();
        protected virtual void OnGUI() => PyOnGUIHandler?.Invoke();
        protected virtual void ShowButton(Rect rect) => PyShowButtonHandler?.Invoke(rect);
        protected virtual void OnFocus() => PyOnFocusHandler?.Invoke();
        protected virtual void OnLostFocus() => PyOnLostFocusHandler?.Invoke();
        protected virtual void OnDisable() => PyOnDisableHandler?.Invoke();

        protected virtual void OnDestroy()
        {
            PyCleanHandler?.Invoke();

            if (_disposeScopeOnDestroy)
                _scope?.Dispose();
        }


#if UNITY_2021_3_OR_NEWER
        public override void SaveChanges()
        {
            PySaveChangesHandler?.Invoke();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            PyDiscardChangesHandler?.Invoke();
            base.DiscardChanges();
        }
#endif


        /// <inheritdoc />
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload Python Script"), false, () =>
            {
                PyCleanHandler?.Invoke();
                PythonBridge.ExecuteFile(PythonScriptPath);
                PyInitHandler?.Invoke();
            });
        }
    }
}