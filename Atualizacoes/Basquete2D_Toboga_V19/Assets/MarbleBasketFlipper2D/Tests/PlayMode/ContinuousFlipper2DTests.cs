using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class ContinuousFlipper2DTests
    {
        [UnityTest] public IEnumerator OnlyTheLipChangesItsInclinationAroundTheFixedHinge()
        {
            var root = new GameObject("Teste da ponta móvel");
            root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1;
                root.SetActive(true);

                var world = root.transform.Find("Arena de duas cestas gerada");
                var tip = world.Find("Tobogã · ponta articulada").GetComponent<Rigidbody2D>();
                var hinge = tip.position;
                float mostUp = 0, mostDown = 0;
                float cycle = 1f / arena.velocidadeDaRampa;
                for (float t = 0; t <= cycle; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    mostUp = Mathf.Max(tip.rotation, mostUp);
                    mostDown = Mathf.Min(tip.rotation, mostDown);
                    Assert.That(Vector2.Distance(tip.position, hinge), Is.LessThan(.001f));
                }

                Assert.That(mostUp, Is.InRange(5.5f, 8.1f));
                Assert.That(mostDown, Is.InRange(-8.1f, -5.5f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
