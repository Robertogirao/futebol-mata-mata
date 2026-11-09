using UnityEngine;

/// <summary>
/// Deforma apenas a arte dos braços/luvas. O objeto do goleiro, sua IA e seus
/// colisores continuam intactos. Não altera o asset Sprite compartilhado.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimacaoMaosGoleiro : MonoBehaviour
{
    private const int Colunas = 12;
    private const int Linhas = 48;
    private SpriteRenderer origem;
    private GoalkeeperController controlador;
    private TemaCompeticao tema;
    private Mesh malha;
    private MeshRenderer desenho;
    private Vector3[] repouso;
    private Vector3[] vertices;
    private Bounds limites;
    private float fase;
    private float impacto;
    private Vector2 acompanhamento;
    private int ultimaDefesa;
    private bool habilitacaoOriginal;
    private bool preparado;

    public void Configurar(SpriteRenderer visual, GoalkeeperController goleiro, TemaCompeticao aparencia)
    {
        if (visual == null || visual.sprite == null) return;
        if (!preparado || origem != visual) habilitacaoOriginal = visual.enabled;
        origem = visual;
        controlador = goleiro;
        tema = aparencia;
        ultimaDefesa = controlador != null ? controlador.NumeroDeDefesasVisuais : 0;
        acompanhamento = Vector2.zero;
        impacto = 0f;
        if (tema == null || !tema.animarMaosGoleiro || tema.materialMaosGoleiro == null)
        {
            RestaurarVisual();
            return;
        }
        MontarMalha();
        preparado = true;
        AplicarVisibilidade(true);
        AtualizarMalha();
    }

    private void MontarMalha()
    {
        if (malha != null) Destroy(malha);
        malha = new Mesh { name = "Goleiro_BracosELuvas" };
        malha.MarkDynamic();
        var filtro = GetComponent<MeshFilter>();
        if (filtro == null) filtro = gameObject.AddComponent<MeshFilter>();
        desenho = GetComponent<MeshRenderer>();
        if (desenho == null) desenho = gameObject.AddComponent<MeshRenderer>();
        desenho.sharedMaterial = tema.materialMaosGoleiro;
        desenho.sortingLayerID = origem.sortingLayerID;
        desenho.sortingOrder = origem.sortingOrder;
        desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        desenho.receiveShadows = false;
        var propriedades = new MaterialPropertyBlock();
        propriedades.SetTexture("_MainTex", origem.sprite.texture);
        propriedades.SetColor("_Color", origem.color);
        desenho.SetPropertyBlock(propriedades);

        limites = origem.sprite.bounds;
        Rect recorte = origem.sprite.textureRect;
        float larguraTextura = origem.sprite.texture.width;
        float alturaTextura = origem.sprite.texture.height;
        int quantidade = (Colunas + 1) * (Linhas + 1);
        repouso = new Vector3[quantidade];
        vertices = new Vector3[quantidade];
        var uv = new Vector2[quantidade];
        var cores = new Color[quantidade];
        var triangulos = new int[Colunas * Linhas * 6];
        for (int y = 0; y <= Linhas; y++)
            for (int x = 0; x <= Colunas; x++)
            {
                int i = y * (Colunas + 1) + x;
                float u = (float)x / Colunas, v = (float)y / Linhas;
                repouso[i] = new Vector3(Mathf.Lerp(limites.min.x, limites.max.x, u),
                    Mathf.Lerp(limites.min.y, limites.max.y, v), 0f);
                float tu = origem.flipX ? 1f - u : u;
                float tv = origem.flipY ? 1f - v : v;
                uv[i] = new Vector2((recorte.x + tu * recorte.width) / larguraTextura,
                    (recorte.y + tv * recorte.height) / alturaTextura);
                cores[i] = Color.white;
                if (x == Colunas || y == Linhas) continue;
                int t = (y * Colunas + x) * 6;
                triangulos[t] = i; triangulos[t + 1] = i + Colunas + 1; triangulos[t + 2] = i + 1;
                triangulos[t + 3] = i + 1; triangulos[t + 4] = i + Colunas + 1; triangulos[t + 5] = i + Colunas + 2;
            }
        malha.vertices = repouso;
        malha.uv = uv;
        malha.colors = cores;
        malha.triangles = triangulos;
        malha.RecalculateBounds();
        filtro.sharedMesh = malha;
    }

    private void LateUpdate()
    {
        if (!preparado || origem == null) return;
        bool animar = tema != null && tema.animarMaosGoleiro && tema.materialMaosGoleiro != null;
        AplicarVisibilidade(animar);
        if (!animar || Time.deltaTime <= 0f) return;

        fase += Time.deltaTime * Mathf.Clamp(tema.velocidadeMaosGoleiro, 0.2f, 3f) * Mathf.PI * 2f;
        Vector2 alvo = Vector2.zero;
        if (controlador != null)
        {
            BallController bola = controlador.BolaMaisProxima;
            if (bola != null && !bola.jaMarcouGol)
            {
                Vector2 distancia = bola.transform.position - controlador.transform.position;
                float proximidade = Mathf.Clamp01(1f - distancia.magnitude / 7f);
                alvo = distancia.normalized * proximidade;
            }
            if (controlador.NumeroDeDefesasVisuais != ultimaDefesa)
            {
                ultimaDefesa = controlador.NumeroDeDefesasVisuais;
                impacto = 1f;
            }
        }
        acompanhamento = Vector2.Lerp(acompanhamento, alvo, 1f - Mathf.Exp(-Time.deltaTime * 7f));
        impacto = Mathf.MoveTowards(impacto, 0f, Time.deltaTime / 0.25f);
        AtualizarMalha();
    }

    // Coordenadas normalizadas da arte atual. Ombros/cabeça permanecem estáveis;
    // a transição suave acompanha o braço que sobe pelo lado direito da figura.
    public static float PesoBracos(float x, float y)
    {
        float alto = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.80f, 0.86f, y));
        float ladoBraco = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.72f, x));
        float alturaBraco = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.80f, y));
        return Mathf.Max(alto, ladoBraco * alturaBraco);
    }

    private void AtualizarMalha()
    {
        if (malha == null || tema == null) return;
        float amplitude = Mathf.Clamp(tema.amplitudeMaosGoleiro, 0f, 0.12f);
        float reacao = Mathf.Clamp(tema.reacaoMaosGoleiro, 0f, 0.12f);
        float balanco = Mathf.Sin(fase) * amplitude;
        float pulso = Mathf.Sin(impacto * Mathf.PI) * 0.07f;
        for (int i = 0; i < repouso.Length; i++)
        {
            float u = (float)(i % (Colunas + 1)) / Colunas;
            float v = (float)(i / (Colunas + 1)) / Linhas;
            // O peso segue a imagem caso o SpriteRenderer esteja espelhado.
            float xImagem = origem.flipX ? 1f - u : u;
            float yImagem = origem.flipY ? 1f - v : v;
            float braco = PesoBracos(xImagem, yImagem);
            float luva = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 0.96f, yImagem));
            Vector3 ponto = repouso[i];
            ponto.x += limites.size.x * braco * (balanco + acompanhamento.x * reacao - pulso);
            ponto.x += limites.size.x * luva * (xImagem - 0.5f) * Mathf.Sin(fase * 0.8f) * amplitude * 0.35f;
            ponto.y += limites.size.y * braco * (acompanhamento.y * reacao * 0.12f + pulso * 0.08f);
            vertices[i] = ponto;
        }
        malha.vertices = vertices;
        malha.RecalculateBounds();
    }

    private void AplicarVisibilidade(bool animar)
    {
        if (origem != null) origem.enabled = habilitacaoOriginal && !animar;
        if (desenho != null) desenho.enabled = habilitacaoOriginal && animar;
    }
    private void RestaurarVisual() => AplicarVisibilidade(false);
    private void OnDisable() => RestaurarVisual();
    private void OnDestroy() { RestaurarVisual(); if (malha != null) Destroy(malha); }
}
