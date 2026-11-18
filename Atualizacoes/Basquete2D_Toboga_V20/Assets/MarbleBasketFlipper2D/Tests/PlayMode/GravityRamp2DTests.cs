using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class GravityRamp2DTests
    {
        [UnityTest] public IEnumerator BallAcceleratesAndLeavesAFixedLipGoingUp()
        {
            var root = new GameObject("Teste de gravidade"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1;
                arena.inclinacaoDaRampa = 0;
                root.SetActive(true);
                yield return null;
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1").GetComponent<Rigidbody2D>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                ball.position = new Vector2(-5.8f, 2f);
                Stop(ball);
                int steps = Mathf.Max(1, Mathf.CeilToInt(.12f / Time.fixedDeltaTime));
                for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
                float earlySpeed = VerticalSpeed(ball);
                for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
                float lateSpeed = VerticalSpeed(ball);
                Assert.That(earlySpeed, Is.LessThan(-.2f));
                Assert.That(lateSpeed, Is.LessThan(earlySpeed - .3f));
                for (float t = 0; t < 3f; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    if (ball.position.x <= -3.0f) continue;
                    float upwardSpeed = VerticalSpeed(ball);
                    Assert.That(upwardSpeed, Is.GreaterThan(1f));
                    Assert.That(ball.position.y, Is.GreaterThan(-2.5f));
                    yield break;
                }
                Assert.Fail("A bola não saiu pela ponta do tobogã.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [UnityTest] public IEnumerator AStalledBallClearsTheTurnAndReturnsToTheFunnel()
        {
            var root = new GameObject("Teste de bola parada na curva");
            root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1;
                arena.inclinacaoDaRampa = 0;
                root.SetActive(true);
                yield return null;

                var world = root.transform.Find("Arena de duas cestas gerada");
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                var ball = world.Find("Vermelho 1").GetComponent<Rigidbody2D>();
                var hinge = world.Find("Tobogã · ponta articulada").GetComponent<Rigidbody2D>();
                ball.position = hinge.position + Vector2.up * .18f;
                Stop(ball);

                for (float t = 0; t < 1.5f; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    if (ball.position.y > 1.8f) yield break;
                }
                Assert.Fail("A bola parada deveria ter voltado ao funil.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Stop(Rigidbody2D ball)
        {
#if UNITY_6000_0_OR_NEWER
            ball.linearVelocity = Vector2.zero;
#else
            ball.velocity = Vector2.zero;
#endif
            ball.angularVelocity = 0;
        }

        private static float VerticalSpeed(Rigidbody2D ball)
        {
#if UNITY_6000_0_OR_NEWER
            return ball.linearVelocity.y;
#else
            return ball.velocity.y;
#endif
        }
    }
}
