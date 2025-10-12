using System;
using System.Diagnostics;
using System.IO;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow
    {
        private const string Key_UseVSCode = "UnityEditor.Scripting.Python.PythonScriptsWindow::UseVSCode";
        private const string Img_VSCodeInactive = "vscode-alt";
        private const string Img_VSCodeActive = "vscode";
        private static bool? _isVSCodeAvailable;
        private static bool _useVSCode;


        private static bool GetUseVSCode()
        {
            string config = EditorUserSettings.GetConfigValue(Key_UseVSCode);
            return bool.TryParse(config, out _useVSCode) && _useVSCode;
        }

        private static void SetUseVSCode(bool useVSCode)
        {
            _useVSCode = useVSCode;
            EditorUserSettings.SetConfigValue(Key_UseVSCode, _useVSCode.ToString());
        }


        internal static bool IsVSCodeAvailable()
        {
            if (_isVSCodeAvailable.HasValue)
                return _isVSCodeAvailable.Value;

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo("code", "--version")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    };
                    process.Start();
                    process.WaitForExit(500);
                    _isVSCodeAvailable = process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                _isVSCodeAvailable = false;
            }

            return _isVSCodeAvailable.Value;
        }

        internal static void OpenPythonScriptFolderInVSCode()
        {
            if (!Directory.Exists(PythonScriptFolder))
                return;

            try
            {
                Process.Start(new ProcessStartInfo("code", PythonScriptFolder)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch
            {
                // ignored(vscode not available)
            }
        }

        internal static void OpenPythonScriptInVSCode(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo("code", $"{PythonScriptFolder} {filePath}")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch
            {
                // ignored(vscode not available)
            }
        }
    }
}