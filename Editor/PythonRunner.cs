using System;
using Python.Runtime;

namespace UnityEditor.Scripting.Python
{
    /// <summary>
    /// This class encapsulates the Unity Editor API support for Python.
    /// </summary>
    [Obsolete("Use 'PythonBridge' instead.")]
    public static class PythonRunner
    {
        static PyModule scope;

        /// <summary>
        /// Runs Python code in the Unity process.
        /// </summary>
        /// <param name="pythonCodeToExecute">The code to execute.</param>
        /// <param name="scopeName">Value to write to Python special variable `__name__`</param>
        public static void RunString(string pythonCodeToExecute, string scopeName = null)
        {
            if (!string.IsNullOrEmpty(scopeName))
            {
                if (scope == null)
                    using (Py.GIL())
                        scope = Py.CreateScope();

                scope.Set("__name__", scopeName);
            }

            PythonBridge.ExecuteString(pythonCodeToExecute, scope);
        }

        internal static void DisposeScope()
        {
            if (scope == null)
                return;

            using (Py.GIL())
            {
                scope.Dispose();
            }

            scope = null;
        }
    }
}