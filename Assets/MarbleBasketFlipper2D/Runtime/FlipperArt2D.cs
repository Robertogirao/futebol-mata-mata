using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleBasketFlipper2D
{
    /// <summary>Render-only sprites; none of these helpers creates physics components.</summary>
    internal sealed class FlipperArt2D : IDisposable
    {
        private readonly Transform root;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Sprite square, rounded, circle, ring, ball, paintedBlade, guideRail, paintedRim, paintedNet;
        private readonly float paintedBladeScaleY, paintedBladeOffsetY;
        public Texture2D PanelTexture { get; private set; }

        public FlipperArt2D(Transform parent)
        {
            root = parent;
            square = MakeSprite(MakeTexture(2, 0), "Quadrado");
            PanelTexture = MakeTexture(128, 1);
            rounded = MakeSprite(PanelTexture, "Painel arredondado");
            circle = MakeSprite(MakeTexture(128, 2), "Círculo sombreado");
            ring = MakeSprite(MakeTexture(128, 4), "Anel de água");
            ball = MakeSprite(MakeTexture(128, 3), "Bolinha brilhante");
            var bladeTexture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/EsteiraPlataforma");
            bool conveyorBlade = bladeTexture != null;
            if (bladeTexture == null) bladeTexture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/Flipper");
            if (bladeTexture != null)
            {
                // Only the child sprite is cropped and aligned; the paddle's collider is untouched.
                var rect = conveyorBlade
                    ? new Rect(0, bladeTexture.height * (226f / 724f),
                        bladeTexture.width, bladeTexture.height * (228f / 724f))
                    : new Rect(0, bladeTexture.height * .10f, bladeTexture.width, bladeTexture.height * .80f);
                paintedBlade = Sprite.Create(bladeTexture, rect, new Vector2(.5f, .5f), rect.width, 0, SpriteMeshType.FullRect);
                paintedBlade.name = "Basquete 2D · esteira ilustrada"; owned.Add(paintedBlade);
                paintedBladeScaleY = rect.width / rect.height * (conveyorBlade ? 1.4166667f : 1.15f);
                paintedBladeOffsetY = conveyorBlade ? -.3083333f : 0;
            }
            var railTexture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/TrilhoNeon");
            if (railTexture != null)
            {
                // Crop only the display rectangle; the source and the colliders are untouched.
                var rect = new Rect(railTexture.width * .009f, railTexture.height * .351f,
                    railTexture.width * .981f, railTexture.height * .304f);
                guideRail = Sprite.Create(railTexture, rect, new Vector2(.5f, .5f), rect.width, 0, SpriteMeshType.FullRect);
                guideRail.name = "Basquete 2D · trilho metálico"; owned.Add(guideRail);
            }
            var hoopTexture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/AroRede");
            if (hoopTexture != null)
            {
                // Split the existing art at the rim: the rim stays fixed and the net can sway.
                float x = hoopTexture.width * .013f, width = hoopTexture.width * .974f;
                float bottom = hoopTexture.height * .0625f, split = hoopTexture.height * .714f;
                float top = hoopTexture.height * .8825f;
                var rimRect = new Rect(x, split, width, top - split);
                var netRect = new Rect(x, bottom, width, split - bottom);
                paintedRim = Sprite.Create(hoopTexture, rimRect, new Vector2(.5f, .5f), width, 0, SpriteMeshType.FullRect);
                paintedNet = Sprite.Create(hoopTexture, netRect, new Vector2(.5f, 1f), width, 0, SpriteMeshType.FullRect);
                paintedRim.name = "Basquete 2D · aro"; paintedNet.name = "Basquete 2D · rede";
                owned.Add(paintedRim); owned.Add(paintedNet);
            }
        }

        private Texture2D MakeTexture(int size, int shape)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "Basquete 2D · textura " + shape, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var data = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
                float d = 0;
                if (shape == 1)
                {
                    float qx = Mathf.Max(Mathf.Abs(u) - .78f, 0), qy = Mathf.Max(Mathf.Abs(v) - .78f, 0);
                    d = Mathf.Sqrt(qx * qx + qy * qy) - .22f;
                }
                else if (shape == 4) d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - .78f) - .10f;
                else if (shape >= 2) d = Mathf.Sqrt(u * u + v * v) - .97f;
                float alpha = shape == 0 ? 1 : Mathf.Clamp01(-d * size * .40f + .5f);
                float edge = shape == 0 ? 1 : Mathf.Clamp01(-d * 13f);
                float light = shape == 0 ? 1 : .54f + .24f * ((v + 1) * .5f) + .19f * edge;
                if (shape == 3)
                {
                    float dx = u + .34f, dy = v - .38f;
                    light = .54f + .28f * edge + .16f * ((v + 1) * .5f) + .50f * Mathf.Exp(-(dx * dx + dy * dy) * 20f);
                }
                byte c = (byte)Mathf.Clamp(Mathf.RoundToInt(light * 255), 0, 255);
                data[y * size + x] = new Color32(c, c, c, (byte)Mathf.RoundToInt(alpha * 255));
            }
            texture.SetPixels32(data); texture.Apply(false, false); owned.Add(texture); return texture;
        }
        private Sprite MakeSprite(Texture2D texture, string name)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width, 0, SpriteMeshType.FullRect);
            sprite.name = "Basquete 2D · " + name; owned.Add(sprite); return sprite;
        }
        public SpriteRenderer Image(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order, Transform parent = null, float angle = 0)
        {
            var go = new GameObject(name); var t = go.transform; t.SetParent(parent != null ? parent : root, false);
            t.localPosition = new Vector3(position.x, position.y, 0); t.localRotation = Quaternion.Euler(0, 0, angle);
            t.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            if (paintedBlade != null && name == "Alavanca móvel")
            {
                renderer.enabled = false;
                // The image is a child: changing its aspect cannot rescale its parent's collider.
                Image("Pá ilustrada", paintedBlade, new Vector2(0, paintedBladeOffsetY), new Vector2(1, paintedBladeScaleY), Color.white, order, t);
            }
            return renderer;
        }
        public SpriteRenderer Box(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent = null, float angle = 0)
            => Image(name, rounded, position, size, color, order, parent, angle);
        public SpriteRenderer Disk(string name, Vector2 position, float diameter, Color color, int order, Transform parent = null)
            => Image(name, circle, position, Vector2.one * diameter, color, order, parent);
        public SpriteRenderer Ring(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent = null)
            => Image(name, ring, position, size, color, order, parent);
        public SpriteRenderer Marble(string name, Vector2 position, float diameter, Color color, int order, Transform parent)
            => Image(name, ball, position, Vector2.one * diameter, color, order, parent);
        public SpriteRenderer Line(string name, Vector2 a, Vector2 b, float width, Color color, int order, Transform parent = null)
        {
            Vector2 d = b - a;
            return Image(name, square, (a + b) * .5f, new Vector2(d.magnitude, width), color, order, parent, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        public bool GuideRail(string name, Vector2 a, Vector2 b, float visualWidth, int order, Transform parent = null)
        {
            if (guideRail == null) return false;
            Vector2 d = b - a;
            float nativeAspect = guideRail.rect.height / guideRail.rect.width;
            Image(name, guideRail, (a + b) * .5f, new Vector2(d.magnitude, visualWidth / nativeAspect),
                Color.white, order, parent, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return true;
        }
        public float BasketNetTopY(float y)
        {
            if (paintedRim == null || paintedNet == null) return y - .058f;
            float rimFraction = paintedRim.rect.height / (paintedRim.rect.height + paintedNet.rect.height);
            return y - .269f + .716f * .5f - .716f * rimFraction;
        }
        public bool BasketHoop(string name, float left, float right, float y, Transform netParent, Transform hoopParent = null)
        {
            if (paintedRim == null || paintedNet == null) return false;
            float width = (right - left) * 1.027f, fullHeight = .716f;
            float rimFraction = paintedRim.rect.height / (paintedRim.rect.height + paintedNet.rect.height);
            float rimHeight = fullHeight * rimFraction, netHeight = fullHeight - rimHeight;
            float top = y - .269f + fullHeight * .5f;
            Image(name + " · aro ilustrado", paintedRim, new Vector2((left + right) * .5f, top - rimHeight * .5f),
                new Vector2(width, rimHeight / (paintedRim.rect.height / paintedRim.rect.width)), Color.white, 5, hoopParent);
            Image(name + " · rede ilustrada", paintedNet, Vector2.zero,
                new Vector2(width, netHeight / (paintedNet.rect.height / paintedNet.rect.width)), Color.white, 5, netParent);
            return true;
        }
        public void Tube(string name, Vector2[] points, Color accent)
        {
            if (name == "Retorno inferior") accent = new Color(.80f, .22f, 1f);
            var glow = new Color(accent.r, accent.g, accent.b, .22f);
            var frame = new Color(.07f, .09f, .16f, 1f);
            var metal = new Color(.52f, .63f, .81f, 1f);
            var glass = new Color(.09f + accent.r * .20f, .11f + accent.g * .16f, .20f + accent.b * .20f, .92f);
            var inner = new Color(.055f, .07f, .14f, .72f);
            var highlight = new Color(.85f, .94f, 1f, .72f);
            Vector2 offset = name == "Rampa" ? Vector2.up * .16f : Vector2.zero;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[i] + offset, b = points[i + 1] + offset;
                Line(name + " · brilho difuso", a, b, .88f, glow, -9);
                Line(name + " · borda escura", a, b, .78f, frame, -8);
                Line(name + " · metal", a, b, .70f, metal, -7);
                Line(name + " · acrílico colorido", a, b, .61f, glass, -6);
                Line(name + " · interior", a, b, .52f, inner, -5);
                Vector2 perpendicular = new Vector2(-(b - a).y, (b - a).x).normalized;
                Line(name + " · reflexo", a + perpendicular * .20f, b + perpendicular * .20f, .045f, highlight, -4);
                Line(name + " · luz", a - perpendicular * .29f, b - perpendicular * .29f, .025f, accent, -3);
            }
            foreach (var point in points)
            {
                Vector2 p = point + offset;
                Disk(name + " · luz da curva", p, .88f, glow, -9);
                Disk(name + " · borda da curva", p, .78f, frame, -8);
                Disk(name + " · curva colorida", p, .61f, glass, -6);
                Disk(name + " · curva transparente", p, .52f, inner, -5);
            }
        }
        public void Background(TemaArena2D theme)
        {
            string imageName = theme == TemaArena2D.Agua ? "ArenaAgua" :
                theme == TemaArena2D.Eletricidade ? "ArenaEletricidade" : "ArenaFogo";
            var texture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/" + imageName);
            if (texture == null)
            {
                Debug.LogWarning("Imagem do tema não encontrada: " + imageName + ". Usando o fundo anterior.");
                texture = Resources.Load<Texture2D>("MarbleBasketFlipper2D/Arena");
            }
            if (texture == null) return;
            var sprite = MakeSprite(texture, "Cenário da arena");
            // The source is widescreen; one sprite unit spans the image width.
            Image("Arena · fundo", sprite, Vector2.zero, new Vector2(16, 16), Color.white, -50);
        }
        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                if (owned[i] == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(owned[i]); else UnityEngine.Object.DestroyImmediate(owned[i]);
            }
            owned.Clear();
        }
    }
}
