using System.Collections.Generic;
using Python.Runtime;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow
    {
        #region Scopes

        public const string MainPythonScopeName = "__main__";
        private static PyModule _mainPythonScope;
        private static readonly Dictionary<string, PyModule> _scopes = new Dictionary<string, PyModule>();

        public static PyModule GetPythonScope(string scopeName)
        {
            if (scopeName == null)
                return null;

            if (_scopes.TryGetValue(scopeName, out PyModule scope))
                return scope;

            return null;
        }

        public static bool DisposePythonScope(string scopeName)
        {
            PyModule scope = GetPythonScope(scopeName);
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

        #endregion
    }
}