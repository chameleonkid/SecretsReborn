using UnityEngine;

namespace SecretsReborn
{
    // Nine-slice drawing directly from the generated atlas, without stretching its corners.
    internal static class ForestInventorySkin
    {
        private static Texture2D atlas;
        private static Texture2D glyphs;
        public static void Panel(Rect area) => Draw(area, new Rect(0, .52f, .5f, .48f), 27, true);
        public static void Leather(Rect area)
        {
            if (atlas == null) atlas = Resources.Load<Texture2D>("InventoryUI/ForestInventoryAtlas");
            if (atlas == null) return;
            const float tileSize = 100;
            var source = new Rect(.18f, .7f, .12f, .12f);
            for (float y = area.y; y < area.yMax; y += tileSize)
                for (float x = area.x; x < area.xMax; x += tileSize)
                {
                    float width = Mathf.Min(tileSize, area.xMax - x), height = Mathf.Min(tileSize, area.yMax - y);
                    GUI.DrawTextureWithTexCoords(new Rect(x, y, width, height), atlas,
                        new Rect(source.x, source.yMax - source.height * height / tileSize,
                            source.width * width / tileSize, source.height * height / tileSize));
                }
        }
        public static void Glyph(Rect area, int index)
        {
            if (glyphs == null) glyphs = Resources.Load<Texture2D>("InventoryUI/EquipmentGlyphs");
            if (glyphs != null) GUI.DrawTextureWithTexCoords(area, glyphs, new Rect(index % 4 * .25f, .75f - index / 4 * .25f, .25f, .25f));
        }
        public static void Slot(Rect area, bool selected) => Draw(area,
            selected ? new Rect(0, .036f, .5f, .484f) : new Rect(.5f, .52f, .5f, .48f), 7);
        public static void Button(Rect area) => Draw(area, new Rect(.5f, .036f, .5f, .484f), 9, true);
        private static void Draw(Rect area, Rect source, float edge, bool tile = false)
        {
            if (atlas == null) atlas = Resources.Load<Texture2D>("InventoryUI/ForestInventoryAtlas");
            if (atlas == null) return;
            edge = Mathf.Min(edge, Mathf.Min(area.width, area.height) / 2);
            const float border = .14f;
            float[] x = { area.x, area.x + edge, area.xMax - edge, area.xMax };
            float[] y = { area.y, area.y + edge, area.yMax - edge, area.yMax };
            float[] u = { source.x, source.x + source.width * border, source.xMax - source.width * border, source.xMax };
            float[] v = { source.yMax, source.yMax - source.height * border, source.y + source.height * border, source.y };
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    var target = new Rect(x[col], y[row], x[col + 1] - x[col], y[row + 1] - y[row]);
                    var uv = new Rect(u[col], v[row + 1], u[col + 1] - u[col], v[row] - v[row + 1]);
                    if (!tile || (row != 1 && col != 1)) GUI.DrawTextureWithTexCoords(target, atlas, uv);
                    else
                    {
                        // Match source aspect ratio to the corner scale, then repeat the middle.
                        float pixelScale = edge / (source.width * atlas.width * border);
                        float tw = col == 1 ? uv.width * atlas.width * pixelScale : target.width;
                        float th = row == 1 ? uv.height * atlas.height * pixelScale : target.height;
                        for (float ty = target.y; ty < target.yMax; ty += th)
                            for (float tx = target.x; tx < target.xMax; tx += tw)
                            {
                                float w = Mathf.Min(tw, target.xMax - tx), h = Mathf.Min(th, target.yMax - ty);
                                GUI.DrawTextureWithTexCoords(new Rect(tx, ty, w, h), atlas,
                                    new Rect(uv.x, uv.yMax - uv.height * h / th, uv.width * w / tw, uv.height * h / th));
                            }
                    }
                }
        }
    }
}
