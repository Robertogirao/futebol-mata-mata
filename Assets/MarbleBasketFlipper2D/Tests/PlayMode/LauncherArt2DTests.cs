using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class LauncherArt2DTests
    {
        [UnityTest] public IEnumerator MachineArtUsesTheExistingPaddleAndKeepsTheChuteOpen()
        {
            var root = new GameObject("Teste visual da máquina"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>(); arena.bolasPorTime = 1;
                root.SetActive(true);
                yield return null;
                var world = root.transform.Find("Arena de duas cestas gerada");
                var housing = world.Find("Carcaça fixa da máquina").GetComponent<SpriteRenderer>();
                Assert.That(housing.sprite.texture.name, Is.EqualTo("BaseMaquina"));
                var pivot = world.Find("Lançador móvel");
                Assert.That(pivot.localPosition.x, Is.EqualTo(-6f).Within(.001f));
                Assert.That(pivot.localPosition.y, Is.EqualTo(-3.08f).Within(.001f));
                var paddle = pivot.Find("Alavanca móvel");
                var collider = paddle.GetComponent<BoxCollider2D>();
                Assert.That(paddle.localPosition.x, Is.EqualTo(1.2f).Within(.001f));
                Assert.That(paddle.localScale.x, Is.EqualTo(2.4f).Within(.001f));
                Assert.That(collider.size.x, Is.EqualTo(.94f).Within(.001f));
                Assert.That(collider.size.y, Is.EqualTo(.80f).Within(.001f));
                Assert.That(paddle.Find("Pá ilustrada").GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("BracoMaquina"));
                Assert.That(world.Find("Parede direita · colisão"), Is.Null);
                Assert.That(world.Find("Cesta de 2 · rede móvel/Corda esquerda").GetComponent<Collider2D>(), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
