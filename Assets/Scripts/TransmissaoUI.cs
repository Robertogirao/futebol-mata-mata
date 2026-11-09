using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD em 1920x1080, com um cartão por equipe. Não controla a física.</summary>
[DisallowMultipleComponent]
public sealed class TransmissaoUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform desenho;
    private TemaCompeticao tema;
    private UniversalUI ui;
    private Image fotoA, fotoB, escudoA, escudoB, logo;
    private TextMeshProUGUI nomeA, nomeB, golsA, golsB, relogio;
    private TextMeshProUGUI prorrogacao;
    private static Sprite painelSprite;
    private int larguraAnterior, alturaAnterior;

    public void Configurar(UniversalUI controle, TemaCompeticao aparencia,
        TeamData timeA, TeamData timeB, List<JogadorDados> titularesA, List<JogadorDados> titularesB)
    {
        ui = controle;
        if (canvas == null || tema != aparencia)
        {
            tema = aparencia;
            Criar();
        }
        DefinirEscudo(escudoA, timeA != null ? timeA.escudoOuBandeira : null);
        DefinirEscudo(escudoB, timeB != null ? timeB.escudoOuBandeira : null);
        DefinirEscudo(logo, tema != null ? tema.logoCompeticao : null);
        ui.placarTimeA = golsA; ui.placarTimeB = golsB; ui.textoCronometro = relogio;
        ui.escudoTimeA = escudoA; ui.escudoTimeB = escudoB; ui.logoCampeonato = logo;
        ui.fotosTitularesA = new[] { fotoA }; ui.fotosTitularesB = new[] { fotoB };
        ui.nomesTitularesA = new[] { nomeA }; ui.nomesTitularesB = new[] { nomeB };
        ui.numerosTitularesA = new TextMeshProUGUI[0];
        ui.numerosTitularesB = new TextMeshProUGUI[0];
        AtualizarEquipe("A", timeA, titularesA);
        AtualizarEquipe("B", timeB, titularesB);
        Ajustar();
    }

    private void Criar()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        // Os objetos antigos continuam ativos para não interromper corrotinas,
        // mas os gráficos do Canvas legado deixam de aparecer por baixo do HUD.
        foreach (var grafico in ui.GetComponentsInChildren<Graphic>(true)) grafico.enabled = false;
        var objeto = new GameObject("Transmissao_MataMata", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = objeto.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        desenho = Caixa("Quadro16x9", objeto.transform, Vector2.zero, new Vector2(1920, 1080));
        Color painel = tema != null ? tema.corPainel : new Color(0.025f, 0.045f, 0.09f, 0.97f);
        Color texto = tema != null ? tema.corTextoPlacar : Color.white;
        Color acento = tema != null ? tema.corDestaque : new Color(0.7f, 0.85f, 1f);
        float escala = tema != null ? tema.escalaHUD : 0.85f;
        Texto("Competicao", desenho, new Vector2(0, 508), new Vector2(1000, 26),
            tema != null ? tema.NomeExibido.ToUpperInvariant() : "MATA-MATA", 20, acento, FonteTitulo());
        var placar = Painel("Placar", desenho, new Vector2(0, 448), new Vector2(1000, 100), painel);
        placar.localScale = Vector3.one * escala;
        Painel("LinhaSuperior", placar, new Vector2(0, 49), new Vector2(960, 2), acento);
        escudoA = Imagem("EscudoA", placar, new Vector2(-430, 0), new Vector2(60, 60));
        escudoB = Imagem("EscudoB", placar, new Vector2(430, 0), new Vector2(60, 60));
        Texto("SiglaA", placar, new Vector2(-305, 0), new Vector2(145, 62), "", 32, texto, FonteTitulo());
        Texto("SiglaB", placar, new Vector2(305, 0), new Vector2(145, 62), "", 32, texto, FonteTitulo());
        golsA = Texto("GolsA", placar, new Vector2(-128, 0), new Vector2(90, 80), "0", 58, texto, FontePlacar());
        golsB = Texto("GolsB", placar, new Vector2(128, 0), new Vector2(90, 80), "0", 58, texto, FontePlacar());
        logo = Imagem("Logo", placar, Vector2.zero, new Vector2(55, 65));
        var tempo = Painel("Tempo", desenho, new Vector2(0, 377), new Vector2(132, 38), painel);
        tempo.localScale = Vector3.one * escala;
        relogio = Texto("Relogio", tempo, Vector2.zero, new Vector2(124, 36), "00:00", 24, texto, FontePlacar());
        prorrogacao = Texto("GolDeOuro", desenho, new Vector2(0, 343), new Vector2(280, 24),
            "GOL DE OURO", 16, acento, FonteTexto());
        prorrogacao.gameObject.SetActive(false);
        CriarCartao(true, painel, acento, escala);
        CriarCartao(false, painel, acento, escala);
        var gol = GoalBannerUI.Instance;
        if (gol != null)
        {
            if (gol.painelBanner != null) gol.painelBanner.SetActive(false);
            var banner = Painel("GolConfirmado", desenho, new Vector2(0, -270), new Vector2(630, 118), painel);
            banner.localScale = Vector3.one * escala;
            Painel("AcentoGol", banner, new Vector2(-313, 0), new Vector2(4, 92), acento);
            gol.fotoJogador = Imagem("FotoAutorGol", banner, new Vector2(-245, 0), new Vector2(96, 105));
            gol.textoTitulo = Texto("TituloGol", banner, new Vector2(45, 26), new Vector2(450, 36),
                "GOL CONFIRMADO", 27, tema != null ? tema.corTituloGol : acento, FonteGol());
            gol.textoDados = Texto("AutorGol", banner, new Vector2(45, -20), new Vector2(450, 46),
                "", 30, tema != null ? tema.corTextoGol : Color.white, FonteGol());
            gol.faixaCorDoTime = Imagem("CorDoTime", banner, new Vector2(45, -48), new Vector2(420, 3));
            gol.tema = tema;
            gol.painelBanner = banner.gameObject;
            ui.bannerGol = banner.gameObject;
            banner.gameObject.SetActive(false);
        }
    }

    private void CriarCartao(bool a, Color painel, Color acento, float escala)
    {
        string lado = a ? "A" : "B";
        var cartao = Painel("Titular" + lado, desenho, new Vector2(a ? -650 : 650, -434), new Vector2(465, 124), painel);
        cartao.localScale = Vector3.one * escala;
        Painel("CorEquipe", cartao, new Vector2(-230, 0), new Vector2(4, 98), acento);
        Imagem("Foto", cartao, new Vector2(-169, 2), new Vector2(105, 116));
        Texto("Equipe", cartao, new Vector2(42, 31), new Vector2(290, 24), "", 17, acento, FonteTexto());
        var nome = Texto("Nome", cartao, new Vector2(42, -5), new Vector2(290, 46), "", 29,
            tema != null ? tema.corNomeJogador : Color.white, FonteJogadores());
        Texto("Legenda", cartao, new Vector2(42, -40), new Vector2(290, 20), "TITULAR", 12, acento, FonteTexto());
        var foto = cartao.Find("Foto").GetComponent<Image>();
        if (a) { fotoA = foto; nomeA = nome; } else { fotoB = foto; nomeB = nome; }
    }

    private void AtualizarEquipe(string lado, TeamData time, List<JogadorDados> titulares)
    {
        var cartao = desenho.Find("Titular" + lado);
        cartao.Find("Equipe").GetComponent<TextMeshProUGUI>().text = time != null ? time.nomeEquipe.ToUpperInvariant() : "";
        desenho.Find("Placar/Sigla" + lado).GetComponent<TextMeshProUGUI>().text = time != null ? time.sigla : "";
        Color cor = time != null ? time.corPrincipal : Color.white;
        cartao.Find("CorEquipe").GetComponent<Image>().color = cor;
        var imagem = lado == "A" ? fotoA : fotoB;
        var nome = lado == "A" ? nomeA : nomeB;
        var jogador = titulares != null && titulares.Count > 0 ? titulares[0] : null;
        DefinirEscudo(imagem, jogador != null ? jogador.foto512x512 : null);
        nome.text = jogador != null ? jogador.nomeJogador : "";
    }

    private void LateUpdate()
    {
        if (Screen.width != larguraAnterior || Screen.height != alturaAnterior) Ajustar();
        if (prorrogacao != null && ui != null) prorrogacao.gameObject.SetActive(ui.EmProrrogacao);
    }
    private void Ajustar()
    {
        if (canvas == null) return;
        Canvas.ForceUpdateCanvases();
        Vector2 tamanho = ((RectTransform)canvas.transform).rect.size;
        desenho.localScale = Vector3.one * Mathf.Min(tamanho.x / 1920f, tamanho.y / 1080f);
        larguraAnterior = Screen.width; alturaAnterior = Screen.height;
    }
    private TMP_FontAsset FonteTitulo() => tema != null && tema.fonteTitulo != null ? tema.fonteTitulo : TMP_Settings.defaultFontAsset;
    private TMP_FontAsset FonteTexto() => tema != null && tema.fonteTexto != null ? tema.fonteTexto : TMP_Settings.defaultFontAsset;
    private TMP_FontAsset FontePlacar() => tema != null && tema.fontePlacar != null ? tema.fontePlacar : FonteTitulo();
    private TMP_FontAsset FonteJogadores() => tema != null && tema.fonteJogadores != null ? tema.fonteJogadores : FonteTexto();
    private TMP_FontAsset FonteGol() => tema != null && tema.fonteGol != null ? tema.fonteGol : FonteTexto();
    private static void DefinirEscudo(Image imagem, Sprite sprite) { imagem.sprite = sprite; imagem.enabled = sprite != null; }
    private static RectTransform Caixa(string nome, Transform pai, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(pai, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = tamanho; return rect;
    }
    private static Image Imagem(string nome, Transform pai, Vector2 pos, Vector2 tamanho)
    {
        var imagem = Caixa(nome, pai, pos, tamanho).gameObject.AddComponent<Image>();
        imagem.raycastTarget = false; imagem.preserveAspect = true; return imagem;
    }
    private static RectTransform Painel(string nome, Transform pai, Vector2 pos, Vector2 tamanho, Color cor)
    {
        Image imagem = Imagem(nome, pai, pos, tamanho);
        imagem.sprite = ObterPainel(); imagem.type = Image.Type.Sliced;
        imagem.preserveAspect = false; imagem.color = cor; return imagem.rectTransform;
    }
    private static TextMeshProUGUI Texto(string nome, Transform pai, Vector2 pos, Vector2 tamanho,
        string valor, float tamanhoFonte, Color cor, TMP_FontAsset fonte)
    {
        var texto = Caixa(nome, pai, pos, tamanho).gameObject.AddComponent<TextMeshProUGUI>();
        texto.font = fonte; texto.text = valor; texto.color = cor; texto.fontSize = tamanhoFonte;
        texto.enableAutoSizing = true; texto.fontSizeMax = tamanhoFonte; texto.fontSizeMin = tamanhoFonte * 0.6f;
        texto.alignment = TextAlignmentOptions.Center; texto.richText = false;
        texto.enableWordWrapping = false; texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.raycastTarget = false; return texto;
    }
    private static Sprite ObterPainel()
    {
        if (painelSprite != null) return painelSprite;
        const int lado = 48, raio = 10;
        var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.name = "PainelArredondado"; textura.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < lado; y++) for (int x = 0; x < lado; x++)
        {
            float dx = Mathf.Max(raio - x, x - (lado - 1 - raio));
            float dy = Mathf.Max(raio - y, y - (lado - 1 - raio));
            float distancia = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
            textura.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(raio + 0.5f - distancia)));
        }
        textura.Apply();
        painelSprite = Sprite.Create(textura, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(raio, raio, raio, raio));
        return painelSprite;
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
