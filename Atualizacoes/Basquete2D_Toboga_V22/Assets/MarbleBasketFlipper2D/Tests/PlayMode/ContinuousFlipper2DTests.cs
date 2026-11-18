using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class ContinuousFlipper2DTests
    {
        [UnityTest] public IEnumerator LipReachesItsNewLimitsAndCanPauseAtTheFixedHinge()
        {
            var root = new GameObject("Teste da ponta móvel");
            root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1;
                arena.inclinacaoDaRampa = 20f;
                arena.velocidadeDaRampa = 1.5f;
                root.SetActive(true);

                var world = root.transform.Find("Arena de duas cestas gerada");
                var tip = world.Find("Tobogã · ponta articulada").GetComponent<Rigidbody2D>();
                var hinge = tip.position;
                float mostUp = 0, mostDown = 0;
                const float cycle = 1f / 1.5f;
                for (float t = 0; t <= cycle; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    float angle = Mathf.DeltaAngle(0f, tip.rotation);
                    mostUp = Mathf.Max(angle, mostUp);
                    mostDown = Mathf.Min(angle, mostDown);
                    Assert.That(Vector2.Distance(tip.position, hinge), Is.LessThan(.001f));
                }

                Assert.That(mostUp, Is.InRange(19f, 20.1f));
                Assert.That(mostDown, Is.InRange(-20.1f, -19f));

                arena.velocidadeDaRampa = 0f;
                yield return new WaitForFixedUpdate();
                float paused = tip.rotation;
                yield return new WaitForFixedUpdate();
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(paused, tip.rotation)), Is.LessThan(.1f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
