from typing import Optional
from UnityEngine import *
from UnityEngine import Debug as UDebug
from UnityEngine import Object as UObject
from UnityEditor import *
from System import Action
from UnityEditor.Scripting.Python import PythonEditorWindow

# Window
WINDOW_ID: str = "PythonEditorWindowSample"
window: PythonEditorWindow | None = None

# Fields
ASSET_KEY: str = "Asset"
asset: UObject | None = None


# Handlers
def handle_unity_init():
    global window, asset
    _, asset = window.UserData.TryGetValue(ASSET_KEY, None)


def handle_unity_update():
    if window is not None:
        window.Repaint()


def handle_unity_ongui():
    # Label
    GUILayout.Label("Python Editor Window Sample", EditorStyles.boldLabel)

    # Asset
    GUILayout.Space(10)
    global asset
    EditorGUI.BeginChangeCheck()
    asset = EditorGUILayout.ObjectField("Asset", asset, UObject, True)
    if EditorGUI.EndChangeCheck():
        global window
        window.UserData[ASSET_KEY] = asset

    # Button
    GUILayout.Space(10)
    if GUILayout.Button("Ping Asset"):
        if asset is None:
            EditorUtility.DisplayDialog("Error", "Asset is None!", "OK")
        else:
            EditorGUIUtility.PingObject(asset)


def handle_unity_clean():
    global window
    if window is not None:
        window.PyInitHandler = None
        window.PyUpdateHandler = None
        window.PyOnGUIHandler = None
        window.PyCleanHandler = None
        window = None
        UDebug.Log("[PythonEditorWindow] Handlers cleaned")


# Register handlers
def register_unity_handlers():
    new_window = PythonEditorWindow.GetWindowByID(WINDOW_ID)
    if new_window is None:
        UDebug.LogError(f"[PythonEditorWindow] Window not found: {WINDOW_ID}")
        return

    global window
    if window is not None:
        handle_unity_clean()

    window = new_window
    window.PyInitHandler = Action(handle_unity_init)
    window.PyUpdateHandler = Action(handle_unity_update)
    window.PyOnGUIHandler = Action(handle_unity_ongui)
    window.PyCleanHandler = Action(handle_unity_clean)
    UDebug.Log("[PythonEditorWindow] Handlers registered")


# Init
register_unity_handlers()
