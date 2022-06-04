using STerrain.EndlessTerrain;
using UnityEditor;
using UnityEngine;

namespace STerrain.Editor
{
    [CustomEditor(typeof(TerrainPreview))]
    public class TerrainPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var terrainPreview = (TerrainPreview)target;

            if (GUILayout.Button("Generate"))
            {
                EditorApplication.delayCall += terrainPreview.GenerateTerrain;
            }

            if (GUILayout.Button("Clear"))
            {
                EditorApplication.delayCall += terrainPreview.ClearExistingChunks;
            }
        }
    }
}