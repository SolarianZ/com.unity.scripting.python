using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Scripting.Python
{
    [Serializable]
    public class ScriptSpec
    {
        public string Path;
        public string Scope;
        public string DisplayName;
        public string Description;
    }

    [Serializable]
    public class PythonScriptSettings
    {
        [NonSerialized]
        public string RootFolder;
        public List<ScriptSpec> ScriptSpecs = new List<ScriptSpec>();

        private Dictionary<string, ScriptSpec> _path2SpecDict;


        internal PythonScriptSettings(string rootFolder) => RootFolder = rootFolder;

        public bool TryGetScriptSpec(string scriptPath, out ScriptSpec scriptSpec)
        {
            if (ScriptSpecs.Count == 0 || string.IsNullOrEmpty(scriptPath))
            {
                scriptSpec = null;
                return false;
            }

            if (_path2SpecDict == null)
            {
                _path2SpecDict = new Dictionary<string, ScriptSpec>(ScriptSpecs.Count);
                foreach (ScriptSpec tmpScriptSpec in ScriptSpecs)
                {
                    _path2SpecDict[tmpScriptSpec.Path] = tmpScriptSpec;
                }
            }

            string scriptFullPath;
            try
            {
                scriptFullPath = Path.GetFullPath(scriptPath);
                scriptFullPath = NormalizePath(scriptFullPath);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError(ex.Message);
                scriptSpec = null;
                return false;
            }

            return _path2SpecDict.TryGetValue(scriptFullPath, out scriptSpec);
        }


        public static bool TryLoad(string settingsPath, string scriptsRootFolder, out PythonScriptSettings settings)
        {
            if (!File.Exists(settingsPath))
            {
                settings = null;
                return false;
            }

            string json = File.ReadAllText(settingsPath);
            try
            {
                settings = JsonUtility.FromJson<PythonScriptSettings>(json);
                if (settings == null)
                    return false;

                settings.ScriptSpecs = settings.ScriptSpecs ?? new List<ScriptSpec>();

                settings.RootFolder = NormalizePath(scriptsRootFolder);
                foreach (ScriptSpec scriptSpec in settings.ScriptSpecs)
                {
                    if (string.IsNullOrEmpty(scriptSpec.Path))
                        continue;

                    try
                    {
                        string scriptFullPath = Path.GetFullPath(Path.Combine(scriptsRootFolder, scriptSpec.Path));
                        scriptSpec.Path = NormalizePath(scriptFullPath);
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.LogError(ex.Message);
                    }
                }

                return true;
            }
            catch
            {
                settings = null;
                return false;
            }
        }

        private static string NormalizePath(string path) => path.Replace('\\', '/');
    }
}