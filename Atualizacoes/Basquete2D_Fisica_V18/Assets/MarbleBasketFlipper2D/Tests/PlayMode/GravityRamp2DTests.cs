using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class GravityRamp2DTests
    {
        [UnityTest] public IEnumerator FallingBallAcceleratesBeforeReachingTheRamp()
        {
            var root = new GameObject("Teste de gravidade"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                root.SetActive(true);
                yield return null;
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1").GetComponent<Rigidbody2D>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                ball.position = new Vector2(-5.8f, .8f);
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
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
