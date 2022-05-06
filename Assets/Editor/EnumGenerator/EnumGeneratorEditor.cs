using UnityEditor;
using UnityEngine;

namespace EnumGeneration
{
    [CustomEditor(typeof(EnumGenerator))]
    public class EnumGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EnumGenerator myTarget = (EnumGenerator)target;

            if (GUILayout.Button("Generate Enums"))
            {
                myTarget.GenerateEnumFiles();
            }
        }
    }
}
