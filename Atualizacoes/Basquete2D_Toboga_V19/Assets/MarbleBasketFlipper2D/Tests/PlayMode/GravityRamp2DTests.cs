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
#if UNITY_6000_0_OR_NEWER
                ball.linearVelocity = Vector2.zero;
#else
                ball.velocity = Vector2.zero;
#endif
                int steps = Mathf.Max(1, Mathf.CeilToInt(.12f / Time.fixedDeltaTime));
                for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
#if UNITY_6000_0_OR_NEWER
                float earlySpeed = ball.linearVelocity.y;
#else
                float earlySpeed = ball.velocity.y;
#endif
                for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
#if UNITY_6000_0_OR_NEWER
                float lateSpeed = ball.linearVelocity.y;
#else
                float lateSpeed = ball.velocity.y;
#endif
                Assert.That(earlySpeed, Is.LessThan(-.2f));
                Assert.That(lateSpeed, Is.LessThan(earlySpeed - .3f));
                for (float t = 0; t < 3f; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    if (ball.position.x <= 1.35f) continue;
#if UNITY_6000_0_OR_NEWER
                    float upwardSpeed = ball.linearVelocity.y;
#else
                    float upwardSpeed = ball.velocity.y;
#endif
                    Assert.That(upwardSpeed, Is.GreaterThan(1f));
                    Assert.That(ball.position.y, Is.GreaterThan(-1.75f));
                    yield break;
                }
                Assert.Fail("A bola não saiu pela ponta do tobogã.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
