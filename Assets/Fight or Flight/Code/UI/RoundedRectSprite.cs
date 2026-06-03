using UnityEngine;

public static class RoundedRectSprite
{
    private static Sprite _cached;

    public static Sprite Get()
    {
        if (_cached != null) return _cached;
        _cached = Build();
        return _cached;
    }

    private static Sprite Build()
    {
        const int size   = 64;
        const int radius = 16;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = 0f, dy = 0f;
            if (x < radius)        dx = radius - x;
            else if (x > size - 1 - radius) dx = x - (size - 1 - radius);
            if (y < radius)        dy = radius - y;
            else if (y > size - 1 - radius) dy = y - (size - 1 - radius);

            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(radius - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        sprite.name = "RoundedRect";
        return sprite;
    }
}
