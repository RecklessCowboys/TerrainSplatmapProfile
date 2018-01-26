using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GUIHelpers {

    // Display a texture with a label centered on top, similar to how textures are shown
    // in the Add Terrain Texture editor window.
    public static Texture2D TextureField(string name, Texture2D texture)
    {
        const float width = 70;

        using (var scope = new GUILayout.VerticalScope())
        {
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = width;
            GUILayout.Label(name, style);
            var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(width), GUILayout.Height(width));
            return result;
        }
    }

    // Check a texture's import settings. If there are problems, add user display strings to the problems List.
    public static void CheckTextureImportSettings(
        string name,
        Texture texture,
        TextureImporterType expectedType,
        bool mustBeReadable,
        List<string> problems)
    {
        if (texture == null)
            return;

        var importer = ImporterForTexture(texture);
        if (importer == null)
            return;

        if (importer.textureType != expectedType)
        {
            var expected = TextureImporterTypeToString(expectedType);
            var actual = TextureImporterTypeToString(importer.textureType);
            var msg = string.Format("{0} has Texture Type {1} but should be {2}.", name, actual, expected);
            problems.Add(msg);
        }

        if (mustBeReadable && !importer.isReadable)
        {
            var msg = string.Format("{0} must be readable.", name);
            problems.Add(msg);
        }
    }

    // May return null. For example, if the the texture has no backing asset as in the case
    // of Texture2D.whiteTexture.
    private static TextureImporter ImporterForTexture(Texture texture)
    {
        var path = AssetDatabase.GetAssetPath(texture);
        return (TextureImporter)AssetImporter.GetAtPath(path);
    }

    private static string TextureImporterTypeToString(TextureImporterType type)
    {
        switch (type)
        {
            case TextureImporterType.NormalMap:
                // Without this, ToString() returns "Bump".
                return "Normal Map";
            default:
                return type.ToString();
        }
    }
}
