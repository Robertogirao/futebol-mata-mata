using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class ArenaInteractions2DTests
    {
        private static void SetVelocity(Rigidbody2D body, Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = value;
#else
            body.velocity = value;
#endif
        }
        private static Vector2 GetVelocity(Rigidbody2D body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        [UnityTest] public IEnumerator BarrierMovesAndBallBouncesOnFloorBeforeReturning()
        {
            var root = new GameObject("Teste de chão e barreira"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                arena.regraDaLava = LavaRule2D.VoltaAoFunil; root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var barrier = world.Find("Barreira móvel").GetComponent<Rigidbody2D>();
                var ball = world.Find("Vermelho 1").GetComponent<Rigidbody2D>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                Assert.That(barrier.GetComponentInChildren<BoxCollider2D>(), Is.Not.Null);
                float initialY = barrier.position.y;
                ball.position = new Vector2(6.5f, -2); SetVelocity(ball, Vector2.zero);
                bool bounced = false, returned = false;
                for (int i = 0; i < Mathf.CeilToInt(4.2f / Time.fixedDeltaTime); i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (ball.position.y < -3 && GetVelocity(ball).y > .2f) bounced = true;
                    if (bounced && ball.position.y > 1.7f) { returned = true; break; }
                    if (i == Mathf.CeilToInt(.3f / Time.fixedDeltaTime))
                        Assert.That(barrier.position.y, Is.GreaterThan(initialY + .5f));
                }
                Assert.That(bounced, Is.True); Assert.That(returned, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest] public IEnumerator LavaBurnsAndRespectsTheEliminationSetting()
        {
            var root = new GameObject("Teste de lava"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                arena.regraDaLava = LavaRule2D.EliminadaNaPartida; root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1"); var body = ball.GetComponent<Rigidbody2D>();
                var collider = ball.GetComponent<CircleCollider2D>();
                var fire = ball.Find("Chama externa").GetComponent<SpriteRenderer>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                body.position = new Vector2(1.65f, -2); SetVelocity(body, Vector2.zero);
                bool sawFire = false;
                for (int i = 0; i < Mathf.CeilToInt(2.5f / Time.fixedDeltaTime); i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (fire.enabled && !collider.enabled) sawFire = true;
                }
                Assert.That(sawFire, Is.True);
                Assert.That(collider.enabled, Is.False);
                Assert.That(ball.Find("Bolinha").GetComponent<SpriteRenderer>().enabled, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest] public IEnumerator ScoredBallReturnsEvenIfItFallsIntoLava()
        {
            var root = new GameObject("Teste de cesta e lava"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                arena.regraDaLava = LavaRule2D.EliminadaNaPartida; root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1"); var body = ball.GetComponent<Rigidbody2D>();
                var fire = ball.Find("Chama externa").GetComponent<SpriteRenderer>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                body.position = new Vector2(3.65f, .25f); SetVelocity(body, Vector2.down);
                bool sawFire = false, returned = false;
                for (int i = 0; i < Mathf.CeilToInt(3f / Time.fixedDeltaTime); i++)
                {
                    yield return new WaitForFixedUpdate();
                    sawFire |= fire.enabled;
                    if (sawFire && body.position.y > 1.7f && ball.GetComponent<CircleCollider2D>().enabled)
                    { returned = true; break; }
                }
                Assert.That(arena.RedScore, Is.EqualTo(2));
                Assert.That(sawFire && returned, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
