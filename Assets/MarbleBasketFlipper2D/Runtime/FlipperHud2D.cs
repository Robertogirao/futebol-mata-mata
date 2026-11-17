using UnityEngine;

namespace MarbleBasketFlipper2D
{
    public sealed class FlipperHud2D : MonoBehaviour
    {
        public FlipperArena2D arena;
        public Texture2D panel;
        private GUIStyle team, number, timer, points, callout;
        private static readonly string[] Callouts = { "SUBSCRIBE!", "LIKE", "SHARE" };
        private const float CalloutSeconds = 3.5f;
        private float calloutStartedAt;

        private void OnEnable() { calloutStartedAt = Time.unscaledTime; }
        private void InitStyles()
        {
            if (team != null) return;
            team = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 31, clipping = TextClipping.Clip };
            number = new GUIStyle(team) { fontSize = 58 };
            timer = new GUIStyle(team) { fontSize = 27 };
            points = new GUIStyle(team) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            callout = new GUIStyle(team) { fontSize = 25 };
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
            // Small text with a subtle shadow, positioned beside the moving backboard.
            GUI.color = new Color(0, 0, 0, .78f);
            GUI.Label(new Rect(r.x + 1, r.y + 2, r.width, r.height), text, points);
            GUI.color = new Color(.90f, .96f, 1f, .95f);
            GUI.Label(r, text, points);
        }
        private void SocialPanel(float seconds)
        {
            float phase = seconds % CalloutSeconds;
            int index = Mathf.FloorToInt(seconds / CalloutSeconds) % Callouts.Length;
            float alpha = Mathf.Clamp01(Mathf.Min(phase / .25f, (CalloutSeconds - phase) / .25f));
            // The panel covers the small striped panel at the bottom centre of the arena.
            Draw(new Rect(702, 837, 196, 49), new Color(.15f, .49f, .65f, .95f));
            Draw(new Rect(705, 840, 190, 43), new Color(.018f, .032f, .055f, .97f));
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(708, 840, 184, 43), Callouts[index], callout);
        }
        private void Draw(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, panel, ScaleMode.StretchToFill, true); }
    }
}
