using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class BasketMotion2DTests
    {
        [UnityTest] public IEnumerator BackboardsNetsAndSupportsFollowBothHoops()
        {
            var root = new GameObject("Teste de cestas móveis"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1; arena.moverCestas = true;
                root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var low = world.Find("Cesta de 2 · estrutura móvel");
                var high = world.Find("Cesta de 3 · estrutura móvel");
                var net = world.Find("Cesta de 2 · rede móvel");
                var foot = world.Find("Cesta de 2 · base móvel");
                var support = foot.Find("Cesta de 2 · suporte telescópico");
                var backboard = low.Find("Cesta de 2 · tabela física · colisão").GetComponent<Collider2D>();
                Vector3 initialNet = net.localPosition;
                Assert.That(backboard.attachedRigidbody, Is.EqualTo(low.GetComponent<Rigidbody2D>()));
                for (int i = 0; i < Mathf.CeilToInt(1.5f / Time.fixedDeltaTime); i++)
                    yield return new WaitForFixedUpdate();
                Assert.That(low.localPosition.x, Is.GreaterThan(.10f));
                Assert.That(low.localPosition.y, Is.GreaterThan(.05f));
                Assert.That(high.localPosition.x, Is.LessThan(-.10f));
                Assert.That(high.localPosition.y, Is.LessThan(-.05f));
                Assert.That(net.localPosition.x - initialNet.x, Is.EqualTo(low.localPosition.x).Within(.02f));
                Assert.That(net.localPosition.y - initialNet.y, Is.EqualTo(low.localPosition.y).Within(.02f));
                Assert.That(foot.localPosition.x, Is.EqualTo(low.localPosition.x).Within(.02f));
                Assert.That(foot.localPosition.y, Is.EqualTo(0).Within(.001f));
                Assert.That(support.localScale.y, Is.EqualTo(1 + low.localPosition.y / 3.2f).Within(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest] public IEnumerator AShiftedHoopScoresWhereTheBallEnters()
        {
            var root = new GameObject("Teste de pontuação na cesta móvel"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1; arena.moverCestas = true;
                root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1").GetComponent<Rigidbody2D>();
                ball.simulated = false;
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                var hoop = world.Find("Cesta de 3 · estrutura móvel");
                for (int i = 0; i < Mathf.CeilToInt(2f / Time.fixedDeltaTime); i++)
                    yield return new WaitForFixedUpdate();
                // At the shifted left edge this position would miss the original, fixed hoop.
                Vector2 offset = hoop.localPosition;
                float x = FlipperRules2D.HighLeft + offset.x + .24f;
                Assert.That(x, Is.LessThan(FlipperRules2D.HighLeft + .17f));
                ball.position = new Vector2(x, FlipperRules2D.HighY + offset.y + .52f);
                ball.simulated = true;
#if UNITY_6000_0_OR_NEWER
                ball.linearVelocity = Vector2.down;
#else
                ball.velocity = Vector2.down;
#endif
                for (int i = 0; i < Mathf.CeilToInt(1.5f / Time.fixedDeltaTime) && arena.RedScore == 0; i++)
                    yield return new WaitForFixedUpdate();
                Assert.That(arena.RedScore, Is.EqualTo(3));
                var net = world.Find("Cesta de 3 · rede móvel");
                var netSide = net.Find("Corda esquerda").GetComponent<Collider2D>();
                Assert.That(Physics2D.GetIgnoreCollision(ball.GetComponent<CircleCollider2D>(), netSide), Is.False);
                var ballSprite = ball.transform.Find("Bolinha").GetComponent<SpriteRenderer>();
                var netSprite = net.Find("Cesta de 3 · rede ilustrada").GetComponent<SpriteRenderer>();
                Assert.That(ballSprite.sortingOrder, Is.LessThan(netSprite.sortingOrder));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
