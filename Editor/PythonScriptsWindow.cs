using UnityEngine;

namespace UnityEditor.Scripting.Python
{
    public partial class PythonScriptsWindow : EditorWindow
    {
        [MenuItem("Tools/Python Scripting/Python Scripts Window")]
        public static void ShowWindow()
        {
            PythonScriptsWindow window = GetWindow<PythonScriptsWindow>();
            window.titleContent = new GUIContent("Python Scripts");
            window.minSize = new Vector2(550, 300);
            window.Show();
        }


        private void ExecutePythonScript()
        {
            
        }

        private void SavePythonScript()
        {
            
        }

        private void LoadPythonScript()
        {
            
        }

        private void ClearPythonScript()
        {
            
        }
        
        private void ClearPythonOutput()
        {
            
        }
    }
}