using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aplica somente a aparência da arena. O controlador escolhe o tema e chama
/// Aplicar antes da abertura, sem alterar componentes ou parâmetros de jogo.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArenaTema : MonoBehaviour
{
    [Header("Renderizadores da arena")]
    public SpriteRenderer fundoArena;
    public SpriteRenderer campo;
    public SpriteRenderer gol;
    public SpriteRenderer helice2;
    public SpriteRenderer goleiro;

    [Tooltip("Procura referências vazias pelos nomes originais, somente nesta cena.")]
    public bool localizarAutomaticamente = true;
    private float temaAlturaGramado = 0.835f;
    private TemaCompeticao temaAplicado;

    private struct AparenciaOriginal
    {
        public Sprite sprite;
        public Color cor;
        public Material material;
        public bool habilitado;

        public AparenciaOriginal(SpriteRenderer renderizador)
        {
            sprite = renderizador.sprite;
            cor = renderizador.color;
            material = renderizador.sharedMaterial;
            habilitado = renderizador.enabled;
        }
    }

    private readonly Dictionary<SpriteRenderer, AparenciaOriginal> originais =
        new Dictionary<SpriteRenderer, AparenciaOriginal>();
    private readonly Dictionary<SpriteRenderer, SpriteRenderer> visuais =
        new Dictionary<SpriteRenderer, SpriteRenderer>();

    public void Aplicar(TemaCompeticao tema)
    {
        if (tema == null)
            return;

        LocalizarReferencias();
        temaAlturaGramado = tema.alturaGramadoNaImagem;
        temaAplicado = tema;

        Camera camera = Camera.main;
        if (camera != null && camera.orthographic)
        {
            camera.orthographicSize = Mathf.Clamp(tema.tamanhoCamera, 6f, 10f);
            camera.backgroundColor = tema.tintFundoArena;
        }

        AplicarAparencia(fundoArena, tema.fundoArena, tema.tintFundoArena);
        AplicarAparencia(campo, tema.campo, tema.tintCampo);
        AplicarAparencia(gol, tema.gol, tema.tintGol, tema.materialGol);
        AplicarAparencia(helice2, tema.helice2, tema.tintHelice, tema.materialHelice);
        AplicarAparencia(goleiro, tema.goleiro, tema.tintGoleiro, tema.materialGoleiro);
    }

    /// <summary>
    /// Restaura os valores capturados na primeira aplicação a cada renderizador.
    /// Desabilitar este componente não desfaz a aparência da competição.
    /// </summary>
    public void Restaurar()
    {
        foreach (KeyValuePair<SpriteRenderer, AparenciaOriginal> item in originais)
        {
            if (item.Key == null)
                continue;

            item.Key.sprite = item.Value.sprite;
            item.Key.color = item.Value.cor;
            item.Key.sharedMaterial = item.Value.material;
            item.Key.enabled = item.Value.habilitado;
            if (visuais.TryGetValue(item.Key, out SpriteRenderer visual) && visual != null)
                visual.gameObject.SetActive(false);
        }
    }

    private void AplicarAparencia(
        SpriteRenderer renderizador,
        Sprite sprite,
        Color cor,
        Material material = null)
    {
        if (renderizador == null)
            return;

        AparenciaOriginal original;
        if (!originais.TryGetValue(renderizador, out original))
        {
            original = new AparenciaOriginal(renderizador);
            originais.Add(renderizador, original);
        }

        // A arte fica num filho. Ajustar esse filho preserva integralmente os
        // transforms, pivôs e polígonos responsáveis pela jogabilidade.
        if (sprite != null && original.sprite != null)
        {
            if (!visuais.TryGetValue(renderizador, out SpriteRenderer visual) || visual == null)
            {
                var filho = new GameObject("ArteDoTema");
                filho.transform.SetParent(renderizador.transform, false);
                visual = filho.AddComponent<SpriteRenderer>();
                visuais[renderizador] = visual;
            }
            Bounds destino = original.sprite.bounds;
            if (renderizador == campo && Camera.main != null)
            {
                Camera camera = Camera.main;
                float proporcao = sprite.bounds.size.x / Mathf.Max(0.001f, sprite.bounds.size.y);
                float altura = camera.orthographicSize * 2f * 1.06f;
                altura = Mathf.Max(altura, camera.orthographicSize * 2f * camera.aspect / proporcao);
                float largura = altura * proporcao;
                float piso = -4.715f;
                foreach (var raiz in gameObject.scene.GetRootGameObjects())
                    foreach (var colisor in raiz.GetComponentsInChildren<Collider2D>(true))
                        if (colisor.name == "Chao") piso = colisor.bounds.max.y;
                Vector3 centro = renderizador.transform.InverseTransformPoint(
                    new Vector3(0, piso - (0.5f - temaAlturaGramado) * altura, renderizador.transform.position.z));
                Vector3 escalaMundo = renderizador.transform.lossyScale;
                destino = new Bounds(centro, new Vector3(largura / Mathf.Abs(escalaMundo.x),
                    altura / Mathf.Abs(escalaMundo.y), 0));
            }
            // O PNG antigo do goleiro tem grande margem vazia. A nova figura
            // acompanha a área que realmente colide, sem ampliar o personagem.
            if (renderizador == goleiro)
            {
                var poligono = renderizador.GetComponent<PolygonCollider2D>();
                if (poligono != null && poligono.pathCount > 0)
                {
                    bool primeiro = true;
                    for (int i = 0; i < poligono.pathCount; i++)
                        foreach (Vector2 ponto in poligono.GetPath(i))
                        {
                            Vector3 local = ponto + poligono.offset;
                            if (primeiro) { destino = new Bounds(local, Vector3.zero); primeiro = false; }
                            else destino.Encapsulate(local);
                        }
                }
            }
            Vector3 escala = new Vector3(destino.size.x / Mathf.Max(0.001f, sprite.bounds.size.x),
                destino.size.y / Mathf.Max(0.001f, sprite.bounds.size.y), 1f);
            visual.transform.localScale = escala;
            visual.transform.localPosition = destino.center - Vector3.Scale(sprite.bounds.center, escala);
            visual.sprite = sprite;
            visual.color = cor;
            visual.sharedMaterial = material != null ? material : original.material;
            visual.sortingLayerID = renderizador.sortingLayerID;
            visual.sortingOrder = renderizador.sortingOrder;
            visual.flipX = renderizador.flipX;
            visual.flipY = renderizador.flipY;
            visual.gameObject.SetActive(original.habilitado);
            renderizador.enabled = false;
            if (renderizador == goleiro)
            {
                var animacao = visual.GetComponent<AnimacaoMaosGoleiro>();
                if (animacao == null) animacao = visual.gameObject.AddComponent<AnimacaoMaosGoleiro>();
                animacao.Configurar(visual, renderizador.GetComponent<GoalkeeperController>(), temaAplicado);
            }
            return;
        }

        if (visuais.TryGetValue(renderizador, out SpriteRenderer anterior) && anterior != null)
            anterior.gameObject.SetActive(false);

        renderizador.enabled = original.habilitado;
        renderizador.sprite = sprite != null ? sprite : original.sprite;
        renderizador.color = cor;
        renderizador.sharedMaterial = material != null ? material : original.material;
    }

    private void LocalizarReferencias()
    {
        if (!localizarAutomaticamente ||
            (fundoArena != null && campo != null && gol != null &&
             helice2 != null && goleiro != null))
        {
            return;
        }

        // Inclui objetos inativos sem procurar em outras cenas ou em assets.
        GameObject[] raizes = gameObject.scene.GetRootGameObjects();
        List<SpriteRenderer> renderizadores = new List<SpriteRenderer>();

        foreach (GameObject raiz in raizes)
            renderizadores.AddRange(raiz.GetComponentsInChildren<SpriteRenderer>(true));

        if (fundoArena == null)
            fundoArena = Procurar(renderizadores, "Fundo_Gramado");
        if (campo == null)
            campo = Procurar(renderizadores, "CAMPO_0");
        if (gol == null)
            gol = Procurar(renderizadores, "GOL_00000_0");
        if (helice2 == null)
            helice2 = Procurar(renderizadores, "HELICE2_00000_0");
        if (goleiro == null)
        {
            goleiro = Procurar(renderizadores, "Goleiro_0");
            if (goleiro == null)
                goleiro = Procurar(renderizadores, "Goleiro_2D");
        }
    }

    private static SpriteRenderer Procurar(
        List<SpriteRenderer> renderizadores,
        string nome)
    {
        SpriteRenderer inativo = null;

        foreach (SpriteRenderer renderizador in renderizadores)
        {
            if (renderizador.gameObject.name != nome)
                continue;

            if (renderizador.gameObject.activeInHierarchy)
                return renderizador;

            if (inativo == null)
                inativo = renderizador;
        }

        return inativo;
    }
}
