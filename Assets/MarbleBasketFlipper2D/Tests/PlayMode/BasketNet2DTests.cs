using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class BasketNet2DTests
    {
        [UnityTest] public IEnumerator TwoPointNetCatchesAndSways() => CheckBasket(2, 3.65f, -.65f);
        [UnityTest] public IEnumerator ThreePointNetCatchesAndSways() => CheckBasket(3, 5.05f, 1.45f);

        private static IEnumerator CheckBasket(int points, float x, float y)
        {
            var root = new GameObject("Teste de contato com a rede"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1; arena.moverCestas = false;
                root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var ball = world.Find("Vermelho 1");
                var body = ball.GetComponent<Rigidbody2D>();
                var ballCollider = ball.GetComponent<CircleCollider2D>();
                var net = world.Find("Cesta de " + points + " · rede móvel");
                var netBody = net.GetComponent<Rigidbody2D>();
                var left = net.Find("Corda esquerda").GetComponent<Collider2D>();
                var right = net.Find("Corda direita").GetComponent<Collider2D>();
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                Assert.That(Physics2D.GetIgnoreCollision(ballCollider, left), Is.True);
                Assert.That(Physics2D.GetIgnoreCollision(ballCollider, right), Is.True);
                body.position = new Vector2(x - .35f, y + .45f);
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = new Vector2(4f, -3f);
#else
                body.velocity = new Vector2(4f, -3f);
#endif
                bool active = false, moved = false, exited = false;
                for (int i = 0; i < Mathf.CeilToInt(3f / Time.fixedDeltaTime); i++)
                {
                    yield return new WaitForFixedUpdate();
                    if (arena.RedScore == points && !Physics2D.GetIgnoreCollision(ballCollider, left) &&
                        !Physics2D.GetIgnoreCollision(ballCollider, right)) active = true;
                    if (active && Mathf.Abs(netBody.rotation) > .1f) moved = true;
                    Vector3 local = net.InverseTransformPoint(body.position);
                    if (active && local.y < -.12f && local.y > -.57f)
                        Assert.That(Mathf.Abs(local.x), Is.LessThan(.60f), "A bola atravessou a lateral da rede.");
                    if (active && body.position.y < y - .85f && Physics2D.GetIgnoreCollision(ballCollider, left))
                    { exited = true; break; }
                }
                Assert.That(arena.RedScore, Is.EqualTo(points));
                Assert.That(active, Is.True); Assert.That(moved, Is.True); Assert.That(exited, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
