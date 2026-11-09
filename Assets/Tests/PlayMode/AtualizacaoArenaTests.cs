using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Reflection evita mudar o Assembly-CSharp e as referências serializadas do projeto.
public class AtualizacaoArenaTests
{
    private readonly List<Object> criados = new List<Object>();
    private const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Type Tipo(string nome) => Type.GetType(nome + ", Assembly-CSharp", true);
    private static void Campo(object obj, string nome, object valor) => obj.GetType().GetField(nome, Todos).SetValue(obj, valor);
    private static object Ler(object obj, string nome) => obj.GetType().GetField(nome, Todos).GetValue(obj);
    private static object Chamar(object obj, string nome, params object[] args) => obj.GetType().GetMethod(nome, Todos).Invoke(obj, args);
    private GameObject Novo(string nome)
    {
        var obj = new GameObject(nome); criados.Add(obj); return obj;
    }
    private Component Bola(out Rigidbody2D corpo, out CircleCollider2D colisor)
    {
        var obj = Novo("BolaTeste"); obj.transform.position = new Vector3(100, 100, 0);
        corpo = obj.AddComponent<Rigidbody2D>(); corpo.gravityScale = 0;
        colisor = obj.AddComponent<CircleCollider2D>(); colisor.radius = 0.12f;
        obj.AddComponent<SpriteRenderer>();
        return obj.AddComponent(Tipo("BallController"));
    }
    private Component Interface()
    {
        Tipo("UniversalUI").GetField("Instance").SetValue(null, null);
        Tipo("AutoBallPipe").GetField("Instance").SetValue(null, null);
        Tipo("TournamentManager").GetField("Instance").SetValue(null, null);
        Tipo("GoalBannerUI").GetField("Instance").SetValue(null, null);
        var ui = Novo("UITeste").AddComponent(Tipo("UniversalUI"));
        Campo(ui, "partidaEmAndamento", true); return ui;
    }
    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        foreach (var obj in criados) if (obj != null) Object.Destroy(obj);
        criados.Clear(); yield return null;
    }

    [UnityTest]
    public IEnumerator GolSomeImediatamenteInclusiveEmPausa()
    {
        var bola = Bola(out var corpo, out var colisor);
        Time.timeScale = 0;
        Assert.IsTrue((bool)Chamar(bola, "ConsumirNoGol"));
        Assert.IsFalse(bola.gameObject.activeSelf);
        Assert.IsFalse(colisor.enabled);
        Assert.IsFalse(corpo.simulated);
        Assert.IsFalse(bola.GetComponent<SpriteRenderer>().enabled);
        Assert.IsFalse((bool)Chamar(bola, "ConsumirNoGol"));
        Time.timeScale = 1; yield return null;
    }

    [UnityTest]
    public IEnumerator DoisSensoresEColisorFilhoContamUmUnicoGol()
    {
        var ui = Interface(); var bola = Bola(out _, out _);
        var filho = Novo("ColisorFilho"); filho.transform.SetParent(bola.transform, false);
        var colisor = filho.AddComponent<CircleCollider2D>();
        var sensor1 = Novo("Sensor1").AddComponent(Tipo("GoalDetector"));
        var sensor2 = Novo("Sensor2").AddComponent(Tipo("GoalDetector"));
        Chamar(sensor1, "OnTriggerEnter2D", colisor);
        Chamar(sensor2, "OnTriggerEnter2D", colisor);
        Assert.AreEqual(1, (int)Ler(ui, "golsA"));
        Assert.IsFalse(bola.gameObject.activeSelf); yield return null;
    }

    [UnityTest]
    public IEnumerator PartidaEncerradaNaoConsomeNemPontua()
    {
        var ui = Interface(); Campo(ui, "partidaEmAndamento", false);
        var bola = Bola(out _, out var colisor);
        var sensor = Novo("Sensor").AddComponent(Tipo("GoalDetector"));
        Chamar(sensor, "OnTriggerEnter2D", colisor);
        Assert.AreEqual(0, (int)Ler(ui, "golsA"));
        Assert.IsTrue(bola.gameObject.activeSelf); yield return null;
    }

    [UnityTest]
    public IEnumerator GoldenGoalConsomeBolaAntesDaPausaFinal()
    {
        var ui = Interface(); Campo(ui, "emProrrogacao", true);
        var bola = Bola(out var corpo, out var colisor);
        var sensor = Novo("Sensor").AddComponent(Tipo("GoalDetector"));
        Chamar(sensor, "OnTriggerEnter2D", colisor);
        Assert.AreEqual(1, (int)Ler(ui, "golsA"));
        Assert.IsFalse(bola.gameObject.activeSelf);
        Assert.IsFalse(corpo.simulated);
        Assert.AreEqual(0, Time.timeScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator OrbitaSemContatoContinuoSaiDoGiroERestauraColisoes()
    {
        Time.timeScale = 1;
        var rotor = Novo("RotorTeste"); rotor.transform.position = new Vector3(100, 100, 0);
        var rb = rotor.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        var forma = rotor.AddComponent<PolygonCollider2D>();
        forma.points = new[] { new Vector2(-0.2f,-1.5f),new Vector2(0.2f,-1.5f),new Vector2(0.2f,1.5f),new Vector2(-0.2f,1.5f) };
        var helice = rotor.AddComponent(Tipo("RotatingKicker")); Campo(helice,"velocidadeDeRotacao",0f);
        var bola = Bola(out var corpo, out var colisor); corpo.position += Vector2.right * 0.6f;
        var protecao = bola.gameObject.AddComponent(Tipo("DesprenderBolaDaHelice"));
        Campo(protecao,"tempoMaximoDeContato",0.15f);
        // Um contato foi observado; depois não chega nenhum Stay. A bola ainda
        // está no raio varrido, reproduzindo o reset indevido da versão anterior.
        Campo(protecao,"observada",helice); Campo(protecao,"ultimoContato",Time.fixedTime);
        yield return new WaitForSeconds(0.23f);
        float raio = (float)Chamar(helice,"ObterRaioDeGiro");
        Assert.Greater(Vector2.Distance(corpo.position,rotor.transform.position),raio+colisor.radius);
        Assert.IsTrue(Physics2D.GetIgnoreCollision(colisor,forma));
        yield return new WaitForSeconds(0.35f);
        Assert.IsFalse(Physics2D.GetIgnoreCollision(colisor,forma));
    }

    [Test]
    public void ElencoGrandeSelecionaSoUmTitularERespeitaIndiceManual()
    {
        var time = ScriptableObject.CreateInstance(Tipo("TeamData")); criados.Add(time);
        var lista = (IList)Ler(time,"elenco");
        for (int i=0;i<3;i++)
        {
            var jogador=Activator.CreateInstance(Tipo("JogadorDados")); Campo(jogador,"nomeJogador","Teste "+i);lista.Add(jogador);
        }
        Campo(time,"usarSelecaoManual",true); Campo(time,"indicesTitulares",new[]{2,1,0});
        var titulares=(IList)Chamar(time,"ObterTitulares");
        Assert.AreEqual(1,titulares.Count); Assert.AreSame(lista[2],titulares[0]);
        Campo(time,"sortearTitularesAleatorios",true);
        for(int i=0;i<20;i++) Assert.AreEqual(1,((IList)Chamar(time,"ObterTitulares")).Count);
    }

    [Test]
    public void MovimentoDasMaosPreservaTroncoEPernas()
    {
        var peso = Tipo("AnimacaoMaosGoleiro").GetMethod("PesoBracos");
        for (int x = 0; x <= 10; x++)
            for (int y = 0; y <= 5; y++)
                Assert.AreEqual(0f, (float)peso.Invoke(null, new object[] { x / 10f, y / 10f }));
        Assert.AreEqual(1f, (float)peso.Invoke(null, new object[] { 0.5f, 1f }));
    }

    [UnityTest]
    public IEnumerator AnimacaoMexeLuvasSemMoverColisoresECongelaNaPausa()
    {
        var objeto = Novo("ArteAnimadaTeste");
        var colisor = objeto.AddComponent<BoxCollider2D>(); colisor.size = new Vector2(0.3f, 2f);
        var textura = new Texture2D(16, 96); criados.Add(textura);
        var sprite = Sprite.Create(textura,new Rect(0,0,16,96),new Vector2(0.5f,0.5f)); criados.Add(sprite);
        var visual = objeto.AddComponent<SpriteRenderer>(); visual.sprite = sprite;
        var tema = ScriptableObject.CreateInstance(Tipo("TemaCompeticao")); criados.Add(tema);
        var shader = Shader.Find("TournamentEngine/GoleiroAnimado"); Assert.IsNotNull(shader);
        var material = new Material(shader); criados.Add(material);
        Campo(tema,"materialMaosGoleiro",material);
        var animacao = objeto.AddComponent(Tipo("AnimacaoMaosGoleiro"));
        Chamar(animacao,"Configurar",visual,null,tema);
        var malha = objeto.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] antes = malha.vertices;
        Campo(animacao,"fase",0.8f); Chamar(animacao,"AtualizarMalha");
        Vector3[] depois = malha.vertices;
        Assert.AreEqual(antes[0],depois[0]);
        Assert.Greater(Vector3.Distance(antes[antes.Length-1],depois[depois.Length-1]),0.0001f);
        Assert.AreEqual(new Vector2(0.3f,2f),colisor.size);
        Assert.AreEqual(Vector3.zero,objeto.transform.position);
        Time.timeScale = 0;
        yield return null;
        Vector3[] pausado = malha.vertices;
        yield return new WaitForSecondsRealtime(0.08f);
        CollectionAssert.AreEqual(pausado,malha.vertices);
        Time.timeScale = 1;
    }
}
