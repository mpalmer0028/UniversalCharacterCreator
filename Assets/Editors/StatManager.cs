using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class StatManager : EditorWindow
{
    [MenuItem("Window/UI Toolkit/StatManager")]
    public static void ShowExample()
    {
        StatManager wnd = GetWindow<StatManager>();
        wnd.titleContent = new GUIContent("StatManager");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);
    }
}