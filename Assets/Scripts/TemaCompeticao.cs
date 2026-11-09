using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NovoTemaCompeticao",
    menuName = "Tournament Engine/Tema de Competição",
    order = 2)]
public sealed class TemaCompeticao : ScriptableObject
{
    [Header("Competição")]
    [Tooltip("Identificador de 1 a 80 caracteres: letras, números, hífen ou sublinhado. Também identifica a pasta de resultados.")]
    public string competicaoId = "champions";

    public string nomeCompeticao = "Champions League";

    [Header("Ano / edição visível")]
    [Tooltip("Ative para mostrar o ano no título, no placar e no chaveamento.")]
    public bool mostrarAno;
    public string ano = "2026";
    public string NomeExibido => mostrarAno && !string.IsNullOrWhiteSpace(ano)
        ? nomeCompeticao + " " + ano.Trim() : nomeCompeticao;

    [Header("Câmera e espaço na tela")]
    [Range(6f, 10f)] public float tamanhoCamera = 7.2f;
    [Range(0.65f, 1.1f)] public float escalaHUD = 0.85f;

    [Header("Fontes e cores: jogadores, placar e gol")]
    public TMP_FontAsset fonteJogadores;
    public TMP_FontAsset fontePlacar;
    public TMP_FontAsset fonteGol;
    public Color corNomeJogador = Color.white;
    public Color corTextoPlacar = Color.white;
    public Color corTextoGol = Color.white;
    public Color corTituloGol = new Color(1f, 0.83f, 0.48f, 1f);

    [Header("Movimento das mãos do goleiro (visual)")]
    public bool animarMaosGoleiro = true;
    [Range(0.2f, 3f)] public float velocidadeMaosGoleiro = 0.8f;
    [Range(0f, 0.12f)] public float amplitudeMaosGoleiro = 0.06f;
    [Range(0f, 0.12f)] public float reacaoMaosGoleiro = 0.07f;
    public Material materialMaosGoleiro;

    [Header("Paleta da interface")]
    public Color corPrimaria = new Color(0.035f, 0.075f, 0.24f, 1f);
    public Color corSecundaria = new Color(0.1f, 0.25f, 0.6f, 1f);
    public Color corDestaque = new Color(0.78f, 0.86f, 1f, 1f);
    public Color corTexto = Color.white;
    public Color corPainel = new Color(0.025f, 0.045f, 0.13f, 0.96f);

    [Header("Imagens da transmissão")]
    public Sprite fundoAbertura;
    public Sprite fundoChaveamento;
    public Sprite logoCompeticao;
    public Sprite logoCanal;

    [Header("Fontes opcionais")]
    public TMP_FontAsset fonteTitulo;
    public TMP_FontAsset fonteTexto;

    [Header("Tamanho das fontes da apresentação")]
    [Tooltip("1 = tamanho atual; 1.2 = 20% maior; 0.8 = 20% menor. O texto se ajusta para caber no espaço disponível.")]
    [Range(0.5f, 1.75f)]
    public float escalaFonteTitulo = 1f;

    [Tooltip("Controla os textos que usam Fonte Texto. 1 = tamanho atual; 1.2 = 20% maior; 0.8 = 20% menor.")]
    [Range(0.5f, 1.75f)]
    public float escalaFonteTexto = 1f;

    [Header("Som opcional")]
    public AudioClip somAbertura;

    [Header("Sprites opcionais da arena")]
    [Tooltip("Se vazio, preserva o sprite original. A nova arte é ajustada num filho visual, sem mudar colisores.")]
    public Sprite fundoArena;
    public Sprite campo;
    public Sprite gol;
    public Sprite helice2;
    public Sprite goleiro;
    [Tooltip("Posição do início do gramado, medida do topo ao rodapé da imagem. 0.835 = 83,5%.")]
    [Range(0.6f, 0.95f)] public float alturaGramadoNaImagem = 0.835f;

    [Header("Cores da arena")]
    public Color tintFundoArena = new Color(0.035f, 0.075f, 0.24f, 1f);
    public Color tintCampo = Color.white;
    public Color tintGol = Color.white;
    public Color tintHelice = Color.white;
    public Color tintGoleiro = Color.white;

    [Header("Materiais opcionais da arena")]
    public Material materialGol;
    public Material materialHelice;
    public Material materialGoleiro;

    public bool PossuiIdentificadorValido()
    {
        return !string.IsNullOrEmpty(competicaoId) &&
            Regex.IsMatch(competicaoId, @"\A[A-Za-z0-9_-]{1,80}\z");
    }

    private void OnValidate()
    {
        if (!PossuiIdentificadorValido())
        {
            Debug.LogWarning(
                "TemaCompeticao: o identificador deve conter de 1 a 80 " +
                "letras, números, hífens ou sublinhados.",
                this);
        }
    }
}
