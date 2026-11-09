using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class DesprenderBolaDaHelice : MonoBehaviour
{
    [Header("Detecção de contato e órbita")]
    [Min(0.1f)] public float tempoMaximoDeContato = 1.2f;
    [Tooltip("Pequenas separações entre a bola e a chuteira não zeram a proteção.")]
    [Min(0.05f)] public float toleranciaSemContato = 0.35f;
    [Header("Desprendimento")]
    [Min(0.1f)] public float velocidadeDeSaida = 4f;
    [Min(0.1f)] public float intervaloEntreTentativas = 1f;
    [Min(0.05f)] public float tempoSemRecaptura = 0.25f;
    [Min(0.01f)] public float folgaDeSaida = 0.15f;

    private Rigidbody2D corpo;
    private BallController bola;
    private Collider2D[] meusColisores;
    private RotatingKicker observada;
    private float tempoPerto;
    private float ultimoContato;
    private float proximaTentativa;
    private float restaurarEm;
    private struct ParIgnorado { public Collider2D bola, helice; }
    private readonly List<ParIgnorado> ignorados = new List<ParIgnorado>();

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        bola = GetComponent<BallController>();
        meusColisores = GetComponentsInChildren<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D colisao) => Registrar(colisao);
    private void OnCollisionStay2D(Collision2D colisao) => Registrar(colisao);

    private void Registrar(Collision2D colisao)
    {
        var helice = colisao.collider.GetComponentInParent<RotatingKicker>();
        if (helice == null || (bola != null && bola.jaMarcouGol)) return;
        if (observada != helice) { observada = helice; tempoPerto = 0f; }
        ultimoContato = Time.fixedTime;
    }

    private void FixedUpdate()
    {
        if (ignorados.Count > 0 && Time.fixedTime >= restaurarEm) RestaurarColisoes();
        if (corpo == null || !corpo.simulated || corpo.bodyType != RigidbodyType2D.Dynamic ||
            (bola != null && bola.jaMarcouGol)) return;
        if (observada == null || !observada.isActiveAndEnabled) { tempoPerto = 0f; return; }

        // Também detecta a bola que dá voltas junto da chuteira, mesmo quando os
        // callbacks de contato se interrompem a cada passo da rotação.
        float raio = RaioDaBola();
        float alcance = observada.ObterRaioDeGiro() + raio + folgaDeSaida;
        bool naOrbita = Vector2.Distance(corpo.position, observada.transform.position) <= alcance;
        bool contatoRecente = Time.fixedTime - ultimoContato <= toleranciaSemContato;
        if (!naOrbita && !contatoRecente)
        {
            observada = null; tempoPerto = 0f; return;
        }
        tempoPerto += Time.fixedDeltaTime;
        if (tempoPerto < tempoMaximoDeContato || Time.fixedTime < proximaTentativa) return;
        if (Desprender(observada, raio, alcance)) { observada = null; tempoPerto = 0f; }
        proximaTentativa = Time.fixedTime + Mathf.Max(0.1f, intervaloEntreTentativas);
    }

    private float RaioDaBola()
    {
        float raio = 0.05f;
        foreach (var colisor in meusColisores)
            if (colisor != null && colisor.enabled && !colisor.isTrigger)
                raio = Mathf.Max(raio, colisor.bounds.extents.magnitude);
        return raio;
    }

    private bool Desprender(RotatingKicker helice, float raio, float alcance)
    {
        Vector2 centro = helice.transform.position;
        Vector2 radial = corpo.position - centro;
        if (radial.sqrMagnitude < 0.0001f) radial = Vector2.up;
        radial.Normalize();
        // Procura espaço fora de TODO o giro. Não coloca a bola numa parede,
        // no goleiro ou num sensor de gol. Não altera as colisões normais.
        for (int i = 0; i < 16; i++)
        {
            float angulo = i == 0 ? 0f : ((i + 1) / 2) * 22.5f * (i % 2 == 0 ? -1f : 1f);
            Vector2 direcao = Quaternion.Euler(0f, 0f, angulo) * radial;
            Vector2 destino = centro + direcao * (alcance + raio);
            if (!EspacoLivre(destino, raio) || !CaminhoLivre(destino, raio, helice)) continue;
            RestaurarColisoes();
            foreach (var meu in meusColisores)
                foreach (var outro in helice.GetComponentsInChildren<Collider2D>())
                    if (meu != null && outro != null && !Physics2D.GetIgnoreCollision(meu, outro))
                    {
                        Physics2D.IgnoreCollision(meu, outro, true);
                        ignorados.Add(new ParIgnorado { bola = meu, helice = outro });
                    }
            restaurarEm = Time.fixedTime + tempoSemRecaptura;
            corpo.position = destino;
            corpo.linearVelocity = direcao * Mathf.Max(velocidadeDeSaida, 0.1f);
            corpo.angularVelocity = 0f;
            corpo.WakeUp();
            foreach (var rastro in GetComponentsInChildren<TrailRenderer>()) rastro.Clear();
            return true;
        }
        return false;
    }

    private bool EspacoLivre(Vector2 ponto, float raio)
    {
        foreach (var colisor in Physics2D.OverlapCircleAll(ponto, raio))
        {
            if (colisor == null || colisor.attachedRigidbody == corpo) continue;
            if (!colisor.isTrigger || colisor.GetComponentInParent<GoalDetector>() != null) return false;
        }
        return true;
    }

    private bool CaminhoLivre(Vector2 destino, float raio, RotatingKicker helice)
    {
        Vector2 caminho = destino - corpo.position;
        foreach (var impacto in Physics2D.CircleCastAll(corpo.position, raio, caminho.normalized, caminho.magnitude))
        {
            Collider2D colisor = impacto.collider;
            if (colisor == null || colisor.attachedRigidbody == corpo ||
                colisor.GetComponentInParent<RotatingKicker>() == helice) continue;
            if (!colisor.isTrigger || colisor.GetComponentInParent<GoalDetector>() != null) return false;
        }
        return true;
    }

    private void RestaurarColisoes()
    {
        foreach (var par in ignorados)
            if (par.bola != null && par.helice != null)
                Physics2D.IgnoreCollision(par.bola, par.helice, false);
        ignorados.Clear();
    }

    private void OnDisable()
    {
        RestaurarColisoes(); observada = null; tempoPerto = 0f; proximaTentativa = 0f;
    }
}
