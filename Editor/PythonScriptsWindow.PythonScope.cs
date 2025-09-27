using System.Collections.Generic;
using Python.Runtime;

namespace UnityEditor.Scripting.Python
{
    // Python Scope
    partial class PythonScriptsWindow
    {
        public const string MainPythonScopeName = "__main__";
        private static PyModule _mainPythonScope;
        private static readonly Dictionary<string, PyModule> _scopes = new Dictionary<string, PyModule>();

        public static PyModule GetPythonScope(string scopeName, bool createIfNotExist)
        {
            if (scopeName == null)
                return null;

            if (_scopes.TryGetValue(scopeName, out PyModule scope))
                return scope;

            if (!createIfNotExist)
                return null;

            scope = PythonBridge.CreateScope(scopeName);
            _scopes.Add(scopeName, scope);
            return scope;
        }

        public static bool DisposePythonScope(string scopeName)
        {
            PyModule scope = GetPythonScope(scopeName, false);
            if (scope == null)
                return false;

            scope.Dispose();
            _scopes.Remove(scopeName);

            if (scope == _mainPythonScope)
                _mainPythonScope = null;

            return true;
        }

        public static void DisposeAllPythonScopes()
        {
            foreach (PyModule scope in _scopes.Values)
            {
                scope?.Dispose();
            }

            _mainPythonScope = null;
            _scopes.Clear();
        }
    }
}