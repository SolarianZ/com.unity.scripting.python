using System.IO;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow
    {
        internal static string PythonScriptSettingsPath => Path.Combine(PythonScriptFolder, "settings.json");
    }
}