using UnityEngine;

public class RotatingKicker : MonoBehaviour
{
    [Header("Configuração da Hélice")]
    [Tooltip("Velocidade de rotação (valores positivos giram para um lado, negativos para o outro)")]
    public float velocidadeDeRotacao = 350f;

    private Rigidbody2D rb;

    // Usa os pontos reais: o retângulo do sprite muda de tamanho a cada giro.
    public float ObterRaioDeGiro()
    {
        float raio = 0.1f;
        foreach (Collider2D colisor in GetComponentsInChildren<Collider2D>())
        {
            if (!colisor.enabled || colisor.isTrigger) continue;
            PolygonCollider2D poligono = colisor as PolygonCollider2D;
            if (poligono != null)
            {
                for (int i = 0; i < poligono.pathCount; i++)
                    foreach (Vector2 ponto in poligono.GetPath(i))
                        raio = Mathf.Max(raio, Vector2.Distance(transform.position,
                            poligono.transform.TransformPoint(ponto + poligono.offset)));
            }
            else
                raio = Mathf.Max(raio, Vector2.Distance(transform.position, colisor.bounds.center) +
                    colisor.bounds.extents.magnitude);
        }
        return raio;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Gira o objeto usando a física para transferir um impacto real e violento para a bola!
        if (rb != null)
        {
            rb.MoveRotation(rb.rotation + velocidadeDeRotacao * Time.fixedDeltaTime);
        }
        else
        {
            transform.Rotate(0, 0, velocidadeDeRotacao * Time.deltaTime);
        }
    }
}
