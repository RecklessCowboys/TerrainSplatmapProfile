using UnityEngine;

// These fields are directly from SplatmapPrototype. They are duplicated here
// since SplatPrototype is not serializable.
[System.Serializable]
public class SplatPrototypeData {
    public Texture2D texture;
    public Texture2D normalMap;
    public Vector2 tileSize;
    public Vector2 tileOffset;
    public float metallic;
    public float smoothness;

    public SplatPrototype ToSplatPrototype()
    {
        var sp = new SplatPrototype();

        sp.texture = texture;
        sp.normalMap = normalMap;
        sp.tileSize = tileSize;
        sp.tileOffset = tileOffset;
        sp.metallic = metallic;
        sp.smoothness = smoothness;

        // SplatPrototype also has a specular field, but regardless what I set it to
        // when texture is null, the result is white. Also, the specular field isn't
        // in the online documentation.

        return sp;
    }
}
