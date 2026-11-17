using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class ArenaTheme2DTests
    {
        [UnityTest] public IEnumerator FireUsesTheFireBackgroundAndEffect() => CheckTheme(TemaArena2D.Fogo, "ArenaFogo", null);
        [UnityTest] public IEnumerator WaterUsesTheWaterBackgroundAndEffect() => CheckTheme(TemaArena2D.Agua, "ArenaAgua", "Água · gota 1");
        [UnityTest] public IEnumerator ElectricityUsesTheElectricBackgroundAndEffect() => CheckTheme(TemaArena2D.Eletricidade, "ArenaEletricidade", "Eletricidade · clarão");
        [UnityTest] public IEnumerator JungleUsesTheJungleBackgroundAndLeaves() => CheckTheme(TemaArena2D.Selva, "ArenaSelva", "Selva · folha 1");
        [UnityTest] public IEnumerator WindUsesTheWindBackgroundAndSwirls() => CheckTheme(TemaArena2D.Vento, "ArenaVento", "Vento · redemoinho 1");
        [UnityTest] public IEnumerator IceUsesTheIceBackgroundAndCrystals() => CheckTheme(TemaArena2D.Gelo, "ArenaGelo", "Gelo · cristal 1");
        [UnityTest] public IEnumerator CaveUsesTheCaveBackgroundAndFragments() => CheckTheme(TemaArena2D.Caverna, "ArenaCaverna", "Caverna · fragmento 1");
        [UnityTest] public IEnumerator TempleUsesTheTempleBackgroundAndStars() => CheckTheme(TemaArena2D.TemploCelestial, "ArenaTemploCelestial", "Templo · estrela 1");
        [UnityTest] public IEnumerator RuinsUseTheRuinsBackgroundAndStones() => CheckTheme(TemaArena2D.RuinasAntigas, "ArenaRuinasAntigas", "Ruínas · pedra 1");
        [UnityTest] public IEnumerator SwampUsesTheSwampBackgroundAndBubbles() => CheckTheme(TemaArena2D.PantanoVenenoso, "ArenaPantanoVenenoso", "Pântano · bolha 1");
        [UnityTest] public IEnumerator DesertUsesTheDesertBackgroundAndSand() => CheckTheme(TemaArena2D.Deserto, "ArenaDeserto", "Deserto · areia 1");

        [Test] public void PreviouslySavedThemesKeepTheirNumericValues()
        {
            Assert.That((int)TemaArena2D.Fogo, Is.EqualTo(0));
            Assert.That((int)TemaArena2D.Agua, Is.EqualTo(1));
            Assert.That((int)TemaArena2D.Eletricidade, Is.EqualTo(2));
            Assert.That((int)TemaArena2D.Selva, Is.EqualTo(3));
            Assert.That((int)TemaArena2D.Vento, Is.EqualTo(4));
            Assert.That((int)TemaArena2D.Gelo, Is.EqualTo(5));
        }

        private static IEnumerator CheckTheme(TemaArena2D theme, string textureName, string particleName)
        {
            var root = new GameObject("Teste do tema da arena"); root.SetActive(false);
            try
            {
                var arena = root.AddComponent<FlipperArena2D>();
                arena.bolasPorTime = 1; arena.moverCestas = false; arena.temaDaArena = theme;
                arena.regraDaLava = LavaRule2D.VoltaAoFunil;
                root.SetActive(true);
                var world = root.transform.Find("Arena de duas cestas gerada");
                var background = world.Find("Arena · fundo").GetComponent<SpriteRenderer>();
                Assert.That(background.sprite.texture.name, Is.EqualTo(textureName));
                Assert.That(world.Find("Lançador móvel/Alavanca móvel"), Is.Not.Null);
                Assert.That(world.Find("Cesta de 2 · rede móvel/Corda esquerda").GetComponent<Collider2D>(), Is.Not.Null);

                var ball = world.Find("Vermelho 1");
                var body = ball.GetComponent<Rigidbody2D>();
                var collider = ball.GetComponent<CircleCollider2D>();
                var fire = ball.Find("Chama externa").GetComponent<SpriteRenderer>();
                var effect = ball.Find("Efeito do elemento");
                world.Find("Azul 1").GetComponent<Rigidbody2D>().simulated = false;
                body.position = new Vector2(-2.5f, -3.20f);
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = Vector2.down;
#else
                body.velocity = Vector2.down;
#endif
                for (int i = 0; i < 12 && collider.enabled; i++) yield return new WaitForFixedUpdate();
                Assert.That(collider.enabled, Is.False, "O contato com o elemento não foi detectado.");
                Assert.That(fire.enabled, Is.EqualTo(theme == TemaArena2D.Fogo));
                var parts = effect.GetComponentsInChildren<SpriteRenderer>(true);
                if (theme == TemaArena2D.Fogo) Assert.That(parts.Length, Is.Zero);
                else
                {
                    Assert.That(parts.Length, Is.GreaterThan(0));
                    Assert.That(parts[0].enabled, Is.True);
                    Assert.That(effect.Find(particleName).GetComponent<SpriteRenderer>().enabled, Is.True);
                }
                for (int i = 0; i < Mathf.CeilToInt(2f / Time.fixedDeltaTime) && !collider.enabled; i++)
                    yield return new WaitForFixedUpdate();
                Assert.That(collider.enabled, Is.True, "A bolinha deveria retornar ao funil após o efeito.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
