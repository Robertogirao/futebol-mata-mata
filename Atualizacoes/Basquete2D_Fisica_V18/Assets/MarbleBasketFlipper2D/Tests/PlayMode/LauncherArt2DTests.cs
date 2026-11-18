using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class LauncherArt2DTests
    {
        [UnityTest] public IEnumerator ConveyorArtUsesTheExistingPaddleAndKeepsTheChuteOpen()
        {
            var root = new GameObject("Teste visual da esteira"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                root.SetActive(true);
                yield return null;
                var world = root.transform.Find("Arena de duas cestas gerada");
                Assert.That(world.Find("Carcaça fixa da máquina"), Is.Null);
                Assert.That(world.Find("Apoio da esteira · esquerda"), Is.Not.Null);
                var pivot = world.Find("Lançador móvel");
                Assert.That(pivot.localPosition.x, Is.EqualTo(-6f).Within(.001f));
                Assert.That(pivot.localPosition.y, Is.EqualTo(-3.08f).Within(.001f));
                var paddle = pivot.Find("Alavanca móvel");
                var collider = pivot.GetComponent<EdgeCollider2D>();
                Assert.That(paddle.localPosition.x, Is.EqualTo(.70f).Within(.001f));
                Assert.That(paddle.localScale.x, Is.EqualTo(1.4f).Within(.001f));
                Assert.That(collider.points.Length, Is.GreaterThan(2));
                Assert.That(collider.points[collider.points.Length - 1].y, Is.GreaterThan(collider.points[0].y));
                var image = paddle.Find("Pá ilustrada");
                var renderer = image.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sprite.texture.name, Is.EqualTo("EsteiraPlataforma"));
                float visualTop = image.localPosition.y + renderer.sprite.bounds.max.y * image.localScale.y;
                Assert.That(visualTop * paddle.localScale.y, Is.EqualTo(collider.points[0].y).Within(.002f));
                Assert.That(world.Find("Curva de descida · colisão").GetComponent<EdgeCollider2D>().pointCount, Is.GreaterThan(2));
                Assert.That(world.Find("Parede direita · colisão"), Is.Null);
                Assert.That(world.Find("Cesta de 2 · rede móvel/Corda esquerda").GetComponent<Collider2D>(), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
