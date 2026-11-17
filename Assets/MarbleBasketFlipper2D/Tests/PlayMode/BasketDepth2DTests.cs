using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class BasketDepth2DTests
    {
        [UnityTest] public IEnumerator TwoPointBallAppearsBehindTheNet() => CheckBasket(2, 3.65f, -.65f);
        [UnityTest] public IEnumerator ThreePointBallAppearsBehindTheNet() => CheckBasket(3, 5.05f, 1.45f);

        private static IEnumerator CheckBasket(int points, float x, float y)
        {
            var root = new GameObject("Teste visual da rede"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1");
                var body = ball.GetComponent<Rigidbody2D>();
                var renderer = ball.Find("Bolinha").GetComponent<SpriteRenderer>();
                var highlight = ball.Find("Reflexo").GetComponent<SpriteRenderer>();
                var net = world.Find("Cesta de " + points + " · rede móvel/Cesta de " + points + " · rede ilustrada").GetComponent<SpriteRenderer>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                body.position = new Vector2(x, y + .55f);
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = Vector2.down;
#else
                body.velocity = Vector2.down;
#endif
                bool behindNet = false, foregroundRestored = false;
                for (int i = 0; i < Mathf.CeilToInt(2f / Time.fixedDeltaTime); i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (arena.RedScore == points && renderer.sortingOrder < net.sortingOrder &&
                        highlight.sortingOrder < net.sortingOrder) behindNet = true;
                    if (behindNet && body.position.y < y - .79f && renderer.sortingOrder == 10 && highlight.sortingOrder == 11)
                    { foregroundRestored = true; break; }
                }
                Assert.That(arena.RedScore, Is.EqualTo(points));
                Assert.That(behindNet, Is.True);
                Assert.That(foregroundRestored, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
