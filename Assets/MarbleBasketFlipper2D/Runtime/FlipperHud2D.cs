using UnityEngine;

namespace MarbleBasketFlipper2D
{
    public sealed class FlipperHud2D : MonoBehaviour
    {
        public FlipperArena2D arena;
        public Texture2D panel;
        private GUIStyle team, number, timer, points, callout;
        private static readonly string[] Callouts = { "SUBSCRIBE!", "LIKE", "SHARE" };
        private static readonly Vector2[] GlowOuter = { new Vector2(-3, 0), new Vector2(3, 0), new Vector2(0, -3), new Vector2(0, 3),
            new Vector2(-2, -2), new Vector2(2, -2), new Vector2(-2, 2), new Vector2(2, 2) };
        private static readonly Vector2[] GlowInner = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1) };
        private const float CalloutSeconds = 3.5f;
        private float calloutStartedAt;
        private Texture2D integratedSign;

        private void OnEnable() { calloutStartedAt = Time.unscaledTime; }
        private void OnDisable()
        {
            if (integratedSign == null) return;
            Destroy(integratedSign); integratedSign = null;
        }
        private void InitStyles()
        {
            if (team != null) return;
            team = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 31, clipping = TextClipping.Clip };
            number = new GUIStyle(team) { fontSize = 58 };
            timer = new GUIStyle(team) { fontSize = 27 };
            points = new GUIStyle(team) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            callout = new GUIStyle(team) { fontSize = 23 };
            team.normal.textColor = number.normal.textColor = timer.normal.textColor = points.normal.textColor = callout.normal.textColor = Color.white;
        }
        private void OnGUI()
        {
            if (arena == null || panel == null || arena.Camera == null) return;
            InitStyles(); var pixel = arena.Camera.pixelRect;
            if (pixel.width < 1 || pixel.height < 1) return;
            var oldMatrix = GUI.matrix; var oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(pixel.x, Screen.height - pixel.yMax, 0), Quaternion.identity,
                new Vector3(pixel.width / 1600f, pixel.height / 900f, 1));
            try
            {
                Draw(new Rect(340, 12, 920, 94), new Color(.18f, .70f, 1f));
                Draw(new Rect(345, 17, 910, 84), new Color(.025f, .032f, .052f));
                Draw(new Rect(350, 20, 225, 78), new Color(1f, .12f, .18f));
                Draw(new Rect(1025, 20, 225, 78), new Color(.20f, .53f, 1f));
                Draw(new Rect(355, 24, 215, 70), new Color(.72f, .025f, .05f));
                Draw(new Rect(1030, 24, 215, 70), new Color(.025f, .15f, .80f));
                GUI.color = Color.white;
                GUI.Label(new Rect(360, 24, 205, 70), arena.RedName, team);
                GUI.Label(new Rect(1035, 24, 205, 70), arena.BlueName, team);
                GUI.Label(new Rect(584, 18, 120, 78), arena.RedScore.ToString("00"), number);
                GUI.Label(new Rect(896, 18, 120, 78), arena.BlueScore.ToString("00"), number);
                int seconds = Mathf.CeilToInt(Mathf.Max(0, arena.RemainingSeconds));
                GUI.Label(new Rect(710, 25, 180, 60), string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60), timer);
                Vector2 highOffset = arena.BasketVisualOffset(3), lowOffset = arena.BasketVisualOffset(2);
                ScoreMarker(new Rect(1416 + highOffset.x * 100, 235 - highOffset.y * 100, 80, 32), "3 PTS");
                ScoreMarker(new Rect(1276 + lowOffset.x * 100, 445 - lowOffset.y * 100, 80, 32), "2 PTS");
                SocialPanel(Mathf.Max(0, Time.unscaledTime - calloutStartedAt));
                if (arena.Finished) { GUI.color = Color.white; GUI.Label(new Rect(610, 110, 380, 48), "FIM DE JOGO", team); }
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }
        private void ScoreMarker(Rect r, string text)
        {
            // Neon lettering follows the backboard without placing a plaque in front of it.
            NeonLabel(r, text, points, 1f);
        }
        private void SocialPanel(float seconds)
        {
            float phase = seconds % CalloutSeconds;
            int index = Mathf.FloorToInt(seconds / CalloutSeconds) % Callouts.Length;
            float alpha = Mathf.Clamp01(Mathf.Min(phase / .25f, (CalloutSeconds - phase) / .25f));
            // Replace only the striped inset, preserving the original beveled metal frame.
            if (integratedSign == null) integratedSign = CreateIntegratedSign();
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(695, 835, 225, 39), integratedSign, ScaleMode.StretchToFill, true);
            NeonLabel(new Rect(713, 837, 188, 34), Callouts[index], callout, alpha);
        }
        private static Texture2D CreateIntegratedSign()
        {
            const int width = 225, height = 39;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "Painel integrado de neon", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                // Texture pixels start at the bottom; the bezel coordinates start at the top.
                float topY = height - y - .5f;
                float left, right;
                if (topY <= 24f)
                {
                    left = 24f * (1f - topY / 24f);
                    right = 204f + 21f * topY / 24f;
                }
                else
                {
                    float t = (topY - 24f) / 15f;
                    left = 16f * t; right = 225f - 15f * t;
                }
                float edge = Mathf.Min(x + .5f - left, right - (x + .5f));
                byte opacity = (byte)Mathf.RoundToInt(Mathf.Clamp01(edge + .5f) * 255f);
                pixels[y * width + x] = new Color32(7, 17, 26, opacity);
            }
            texture.SetPixels32(pixels); texture.Apply(false, false);
            return texture;
        }
        private static void NeonLabel(Rect r, string text, GUIStyle style, float alpha)
        {
            if (alpha <= 0) return;
            GUI.color = new Color(.02f, .78f, 1f, .13f * alpha);
            foreach (var offset in GlowOuter)
                GUI.Label(new Rect(r.x + offset.x, r.y + offset.y, r.width, r.height), text, style);
            GUI.color = new Color(.08f, .86f, 1f, .32f * alpha);
            foreach (var offset in GlowInner)
                GUI.Label(new Rect(r.x + offset.x, r.y + offset.y, r.width, r.height), text, style);
            GUI.color = new Color(.78f, .98f, 1f, alpha);
            GUI.Label(r, text, style);
        }
        private void Draw(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, panel, ScaleMode.StretchToFill, true); }
    }
}
