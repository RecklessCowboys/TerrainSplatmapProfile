using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainSplatmapProfile))]
public class TerrainSplatmapProfileEditor : Editor
{
    private List<string> reasonsWhyCannotApply = null;


    private void OnEnable()
    {
        Undo.undoRedoPerformed += MyUndoRedoCallback;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= MyUndoRedoCallback;
    }

    [MenuItem("Assets/Create/Terrain Splatmap Profile")]
    private static void CreateTerrainSettings()
    {
        var ts = NewDefaultTerrainSplatmapProfile();

        // ProjectWindowUtil is not fully documented, but is the only way I could figure out
        // to get the Right Click > Create asset behavior to match what the
        // CreateAssetMenu attribute does. I couldn't use CreateAssetMenu because 
        // I needed to do some work to initialize the instance outside of the constructor.

        ProjectWindowUtil.CreateAsset(ts, "New Terrain Splatmap Profile.asset");
    }

    private static TerrainSplatmapProfile NewDefaultTerrainSplatmapProfile()
    {
        var ts = ScriptableObject.CreateInstance<TerrainSplatmapProfile>();

        if (Terrain.activeTerrain != null)
        {
            // most likely the user will want to use settings for the terrain in the current
            // scene, so start with that.
            ts.target = Terrain.activeTerrain.terrainData;
        }

        ts.textureDefinitions = new List<TextureDefinition>();
        ts.textureDefinitions.Add(NewTextureDefinition());

        return ts;
    }

    public override void OnInspectorGUI()
    {
        var profile = (TerrainSplatmapProfile)target;

        if (reasonsWhyCannotApply == null)
        {
            // CanApply is expensive, so the results are cached and recalculated below
            // by setting reasonsWhyCannotApply to null when GUI.changed is set.
            reasonsWhyCannotApply = new List<string>();
            profile.CanApply(reasonsWhyCannotApply);
        }

        var newTerrainData = (TerrainData)EditorGUILayout.ObjectField("Target", profile.target, typeof(TerrainData), false, null);

        if (newTerrainData != profile.target)
        {
            undo("Set Terrain Data Target");
            profile.target = newTerrainData;
        }

        using (var scope = new EditorGUI.DisabledGroupScope(reasonsWhyCannotApply.Count > 0))
        {
            if (GUILayout.Button("Apply Profile to Target", GUILayout.ExpandWidth(false)))
            {
                Debug.Log(string.Format("Applying {0} to {1}", profile, profile.target));
                profile.Apply();
            }
        }

        foreach(var reason in reasonsWhyCannotApply)
        {
            EditorGUILayout.HelpBox(reason, MessageType.Error);
        }

        EditorGUILayout.LabelField("Texture Definitions", EditorStyles.boldLabel);

        if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
        {
            undo("Add Terrain Texture");
            profile.textureDefinitions.Add(NewTextureDefinition());
        }

        int removeTextureDefinitionIndex = -1;

        for(int i = 0; i < profile.textureDefinitions.Count; i++)
        {
            var td = profile.textureDefinitions[i];

            using (var scopeTextureDefinition = new EditorGUILayout.VerticalScope())
            {
                using (var scopeTextureSet = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(string.Format("Texture {0}", i), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X"))
                    {
                        removeTextureDefinitionIndex = i;
                    }
                }

                var problems = new List<string>();
                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    const bool REQUIRED = true;
                    const bool OPTIONAL = false;
                    CheckedTextureField("Texture", ref td.splatPrototypeData.texture, REQUIRED, TextureImporterType.Default, false, problems);
                    CheckedTextureField("Normal Map", ref td.splatPrototypeData.normalMap, OPTIONAL, TextureImporterType.NormalMap, false, problems);
                    CheckedTextureField("Alpha Map", ref td.alphamap, REQUIRED, TextureImporterType.Default, true, problems);
                }

                using(var scope = new EditorGUILayout.VerticalScope())
                {
                    foreach (var problem in problems)
                    {
                        EditorGUILayout.HelpBox(problem, MessageType.Warning);
                    }

                    var smoothness = EditorGUILayout.Slider("Smoothness", td.splatPrototypeData.smoothness, 0, 1);
                    if (smoothness != td.splatPrototypeData.smoothness)
                    {
                        undo("Smoothness");
                        td.splatPrototypeData.smoothness = smoothness;
                    }

                    var metallic = EditorGUILayout.Slider("Metallic", td.splatPrototypeData.metallic, 0, 1);
                    if (metallic != td.splatPrototypeData.metallic)
                    {
                        undo("Metallic");
                        td.splatPrototypeData.metallic = metallic;
                    }

                    var tileSize = EditorGUILayout.Vector2Field("Tile Size", td.splatPrototypeData.tileSize);
                    if (tileSize != td.splatPrototypeData.tileSize)
                    {
                        undo("Tile Size");
                        td.splatPrototypeData.tileSize = tileSize;
                    }

                    var tileOffset = EditorGUILayout.Vector2Field("Tile Offset", td.splatPrototypeData.tileOffset);
                    if (tileOffset != td.splatPrototypeData.tileOffset)
                    {
                        undo("Tile Offset");
                        td.splatPrototypeData.tileOffset = tileOffset;
                    }
                }
            }
        }

        if (GUI.changed)
        {
            // Need to recalculate expensive CanApply
            reasonsWhyCannotApply = null;
        }

        if (removeTextureDefinitionIndex != -1)
        {
            undo("Remove Terrain Texture");
            profile.textureDefinitions.RemoveAt(removeTextureDefinitionIndex);
            reasonsWhyCannotApply = null;
        }
    }

    private static TextureDefinition NewTextureDefinition()
    {
        var whiteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainSplatmapProfile/Editor/white.bmp");

        var s = new SplatPrototypeData();
        s.texture = whiteTexture;
        s.tileSize = new Vector2(15, 15); // Values taken from terrain editor texture settings.

        var td = new TextureDefinition();
        td.alphamap = whiteTexture;
        td.splatPrototypeData = s;

        return td;
    }

    private void CheckedTextureField(string name, ref Texture2D texture, bool required, TextureImporterType expectedType, bool mustBeReadable, List<string> problems)
    {
        Texture2D t = GUIHelpers.TextureField(name, texture);
        if (t != texture)
        {
            undo(string.Format("{0} Change", name));
            texture = t;
        }

        if (required && t == null)
        {
            problems.Add(string.Format("{0} is required but not set.", name));
        }

        GUIHelpers.CheckTextureImportSettings(name, t, expectedType, mustBeReadable, problems);
    }

    private void undo(string name)
    {
        Undo.RecordObject(target, name);
        EditorUtility.SetDirty(target);
    }

    private void MyUndoRedoCallback()
    {
        reasonsWhyCannotApply = null;
    }
}
