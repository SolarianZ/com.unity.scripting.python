using System;
using System.IO;

namespace UnityEditor.Scripting.Python
{
    // Python Script Settings
    partial class PythonScriptsWindow
    {
        internal static string PythonScriptSettingsPath => Path.Combine(PythonScriptFolder, "settings.json");

        [NonSerialized]
        private PythonScriptSettings _pythonScriptSettings;


        private PythonScriptSettings PreparePythonScriptSettings()
        {
            if (_pythonScriptSettings != null)
                return _pythonScriptSettings;

            if (!PythonScriptSettings.TryLoad(PythonScriptSettingsPath, PythonScriptFolder, out _pythonScriptSettings))
                _pythonScriptSettings = new PythonScriptSettings(PythonScriptFolder);

            return _pythonScriptSettings;
        }

        private bool TryGetPythonScriptSpec(string path, out ScriptSpec scriptSpec)
        {
            PreparePythonScriptSettings();
            return _pythonScriptSettings.TryGetScriptSpec(path, out scriptSpec);
        }
    }
}