#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MarbleBasketFlipper2D.Editor
{
    public static class FlipperSceneEditor2D
    {
        [MenuItem("Tools/Basquete 2D/Abrir funil de duas cestas")]
        public static void Open()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/MarbleBasketFlipper2D/Scenes/MarbleBasketFunil2D.unity");
            Selection.activeGameObject = GameObject.Find("MarbleBasketFunil2D");
        }
    }
    [CustomEditor(typeof(FlipperArena2D))]
    public sealed class FlipperArenaInspector2D : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Entrada na rede V7: ao entrar na cesta, a bolinha aparece por trás do aro e dos fios da rede.", MessageType.Info);
        }
    }
}
#endif
