using UnityEngine;

namespace MarbleBasketFlipper2D
{
    public sealed class FlipperHud2D : MonoBehaviour
    {
        public FlipperArena2D arena;
        public Texture2D panel;
        private GUIStyle team, number, timer, badge;
        private void InitStyles()
        {
            if (team != null) return;
            team = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 31, clipping = TextClipping.Clip };
            number = new GUIStyle(team) { fontSize = 58 };
            timer = new GUIStyle(team) { fontSize = 27 };
            badge = new GUIStyle(team) { fontSize = 24 };
            team.normal.textColor = number.normal.textColor = timer.normal.textColor = badge.normal.textColor = Color.white;
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
                Badge(new Rect(1115 + highOffset.x * 100, 170 - highOffset.y * 100, 176, 66), "3 PONTOS");
                Badge(new Rect(970 + lowOffset.x * 100, 405 - lowOffset.y * 100, 176, 66), "2 PONTOS");
                if (arena.Finished) { GUI.color = Color.white; GUI.Label(new Rect(610, 110, 380, 48), "FIM DE JOGO", team); }
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }
        private void Badge(Rect r, string text)
        {
            Draw(r, new Color(.15f, .70f, 1f));
            Draw(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), new Color(.02f, .045f, .11f));
            GUI.color = Color.white; GUI.Label(r, text, badge);
        }
        private void Draw(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, panel, ScaleMode.StretchToFill, true); }
    }
}
