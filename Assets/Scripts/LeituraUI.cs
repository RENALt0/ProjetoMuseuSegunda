using UnityEngine;

/// <summary>
/// Gerencia a tela de leitura que aparece ao interagir com objetos BaseInformacoes.
/// Coloque este script no mesmo GameObject que tem o FuncoesPlayer (o Player).
/// </summary>
public class LeituraUI : MonoBehaviour
{
    // ─── Singleton simples para outros scripts acessarem ───────────────────────
    public static LeituraUI Instancia { get; private set; }

    // ─── Estado ────────────────────────────────────────────────────────────────
    public bool EstaLendo { get; private set; } = false;

    // ─── Estilo visual ─────────────────────────────────────────────────────────
    [Header("Aparência")]
    public Texture2D imagemFundo = null;          // Arraste uma foto aqui no Inspector
    public Color corFundo       = new Color(0f, 0f, 0f, 1f); // totalmente opaco
    public Color corTexto       = Color.white;
    public Color corBotao       = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color corBotaoHover  = Color.white;
    public int   tamanhoTexto   = 22;
    public int   tamanhoTitulo  = 28;

    // ─── Internos GUI ──────────────────────────────────────────────────────────
    private string    textoAtual    = "";
    private Color     corAtualTexto = Color.white;
    private GUIStyle  estiloFundo;
    private GUIStyle  estiloTexto;
    private GUIStyle  estiloBotao;
    private bool      estilosIniciados = false;
    private Texture2D texFundo;

    // ─── Referências ───────────────────────────────────────────────────────────
    private FuncoesPlayer funcoesPlayer;

    // ───────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        funcoesPlayer = GetComponent<FuncoesPlayer>();
    }

    // ── Chamado por BaseInformacoes ao apertar [E] ─────────────────────────────
    public void AbrirLeitura(string texto, Color? corEspecifica = null)
    {
        textoAtual    = texto;
        corAtualTexto = corEspecifica ?? corTexto;
        EstaLendo     = true;

        if (funcoesPlayer != null) funcoesPlayer.enabled = false; // para câmera/movimento
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Fecha a tela de leitura ────────────────────────────────────────────────
    public void FecharLeitura()
    {
        EstaLendo = false;

        if (funcoesPlayer != null) funcoesPlayer.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ───────────────────────────────────────────────────────────────────────────

    void IniciarEstilos()
    {
        // Fundo semi-transparente
        texFundo = new Texture2D(1, 1);
        texFundo.SetPixel(0, 0, corFundo);
        texFundo.Apply();

        estiloFundo = new GUIStyle(GUI.skin.box);
        estiloFundo.normal.background = texFundo;
        estiloFundo.border            = new RectOffset(0, 0, 0, 0);

        // Texto principal
        estiloTexto                        = new GUIStyle(GUI.skin.label);
        estiloTexto.normal.textColor       = corTexto;
        estiloTexto.fontSize               = tamanhoTexto;
        estiloTexto.wordWrap               = true;
        estiloTexto.alignment              = TextAnchor.UpperLeft;
        estiloTexto.richText               = true;

        // Botão de voltar
        estiloBotao                        = new GUIStyle(GUI.skin.button);
        estiloBotao.fontSize               = 18;
        estiloBotao.fontStyle              = FontStyle.Bold;

        estilosIniciados = true;
    }

    void OnGUI()
    {
        if (!EstaLendo) return;

        if (!estilosIniciados) IniciarEstilos();

        float sw = Screen.width;
        float sh = Screen.height;

        // ── Fundo: foto (se definida) ou cor sólida ──────────────────────────
        if (imagemFundo != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), imagemFundo, ScaleMode.ScaleAndCrop);
            GUI.color = Color.white;
        }
        else
        {
            GUI.Box(new Rect(0, 0, sw, sh), GUIContent.none, estiloFundo);
        }

        // ── Botão "← Voltar" no canto superior direito ────────────────────────
        float btnW = 120f, btnH = 40f, margem = 20f;
        Rect rectBtn = new Rect(sw - btnW - margem, margem, btnW, btnH);
        if (GUI.Button(rectBtn, "← Voltar", estiloBotao))
        {
            FecharLeitura();
            return;
        }

        // ── Área de texto centralizada ────────────────────────────────────────
        float painelW = Mathf.Min(sw * 0.7f, 800f);
        float painelX = (sw - painelW) / 2f;
        float painelY = sh * 0.15f;
        float painelH = sh * 0.70f;

        estiloTexto.normal.textColor = corAtualTexto;
        GUI.Label(new Rect(painelX, painelY, painelW, painelH), textoAtual, estiloTexto);
    }
}
