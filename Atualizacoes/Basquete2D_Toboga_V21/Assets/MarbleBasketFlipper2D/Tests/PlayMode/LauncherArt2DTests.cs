using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class LauncherArt2DTests
    {
        [UnityTest] public IEnumerator SlideMeetsTheHingedLipAndKeepsTheChuteOpen()
        {
            var root = new GameObject("Teste da emenda do tobogã");
            root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1;
                root.SetActive(true);
                yield return null;

                var world = root.transform.Find("Arena de duas cestas gerada");
                var descent = world.Find("Tobogã · descida fixa").GetComponent<EdgeCollider2D>();
                var lip = world.Find("Tobogã · ponta articulada").GetComponent<EdgeCollider2D>();
                var fixedPoints = descent.points;
                var lipPoints = lip.points;
                Vector3 end = descent.transform.TransformPoint(fixedPoints[fixedPoints.Length - 1]);
                Vector3 start = lip.transform.TransformPoint(lipPoints[0]);

                Assert.That(Vector3.Distance(end, start), Is.LessThan(.001f));
                Vector3 exit = lip.transform.TransformPoint(lipPoints[lipPoints.Length - 1]);
                Assert.That(exit.y, Is.GreaterThan(start.y + .30f));
                Assert.That(exit.x, Is.LessThan(-1.20f), "A curva compacta deve terminar no lado esquerdo.");
                foreach (Vector2 point in fixedPoints)
                {
                    if (point.y < end.y + .05f)
                        Assert.That(point.x, Is.GreaterThan(end.x - .50f), "O fundo não deve ter um trecho longo e plano.");
                }
                Assert.That(fixedPoints[0].y, Is.GreaterThan(4f), "A entrada deve dar altura para a queda.");
                Assert.That(world.Find("Parede direita · colisão"), Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
