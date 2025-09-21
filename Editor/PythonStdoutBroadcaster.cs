using System;
using UnityEngine;

namespace UnityEditor.Scripting.Python
{
    public static class PythonStdoutBroadcaster
    {
        public static event Action<string> OnPythonStdout;


        public static void BroadcastPythonStdout(string content)
        {
            try
            {
                OnPythonStdout?.Invoke(content);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}