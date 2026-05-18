using UnityEngine;

/// <summary>
/// Coloque este script no mesmo GameObject que FuncoesPlayer (o Player).
/// Ao chamar AbrirInspecao(gameObject), cria uma cópia do objeto numa área
/// remota (50 000 unidades), renderiza via câmera dedicada → RenderTexture
/// e exibe com overlay escuro + controles de rotação e zoom.
///
/// IMPORTANTE: a layer 31 é usada automaticamente para a área de inspeção.
/// Se você já usa a layer 31 para outro fim, altere LAYER_INSPECAO abaixo.
/// </summary>
public class InspecaoObjeto : MonoBehaviour
{
    // ─── Singleton ─────────────────────────────────────────────────────────────
    public static InspecaoObjeto Instancia { get; private set; }
    public bool EstaInspecionando { get; private set; } = false;

    // ─── Configurações ─────────────────────────────────────────────────────────
    [Header("Câmera / Zoom")]
    public float distanciaInicial  = 2.5f;
    public float distanciaMin      = 0.5f;
    public float distanciaMax      = 6f;
    public float sensibilidadeRot  = 0.4f;
    public float sensibilidadeZoom = 8f;

    [Header("UI")]
    public Color corOverlay   = new Color(0f, 0f, 0f, 0.90f);
    public int   resolucaoRT  = 1024;

    // ─── Constantes internas ────────────────────────────────────────────────────
    private const int       LAYER_INSPECAO = 31;
    private static readonly Vector3 STAGING = new Vector3(50000f, 0f, 0f);

    // ─── Estado ────────────────────────────────────────────────────────────────
    private Camera        camInspecao;
    private RenderTexture renderTex;
    private GameObject    copiaObjeto;
    private GameObject    pivo;           // pivô no centro geométrico do objeto
    private float         distanciaAtual;
    private Quaternion    rotacaoAtual;
    private bool          arrastando;
    private Vector2       ultimaMouse;
    private FuncoesPlayer funcoesPlayer;

    // ─── GUI ───────────────────────────────────────────────────────────────────
    private GUIStyle  estiloFundo, estiloBotao, estiloDica;
    private Texture2D texFundo;
    private bool      estilosOk;

    // ───────────────────────────────────────────────────────────────────────────

    void Awake() { Instancia = this; }

    void Start()
    {
        funcoesPlayer = GetComponent<FuncoesPlayer>();

        // Ocultar a layer de inspeção da câmera principal do jogo
        Camera camPrincipal = ObterCamPrincipal();
        if (camPrincipal != null)
            camPrincipal.cullingMask &= ~(1 << LAYER_INSPECAO);

        CriarCameraDeInspecao();
    }

    Camera ObterCamPrincipal()
    {
        if (funcoesPlayer != null && funcoesPlayer.cameraPrimeiraPessoa != null)
            return funcoesPlayer.cameraPrimeiraPessoa.GetComponent<Camera>();
        return Camera.main;
    }

    void CriarCameraDeInspecao()
    {
        resolucaoRT = Mathf.Clamp(resolucaoRT, 256, 4096);

        var go = new GameObject("_CameraInspecao");
        go.transform.position = STAGING + new Vector3(0f, 0f, -distanciaInicial);
        go.transform.LookAt(STAGING);
        DontDestroyOnLoad(go);

        camInspecao = go.AddComponent<Camera>();
        camInspecao.clearFlags      = CameraClearFlags.SolidColor;
        camInspecao.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        camInspecao.cullingMask     = 1 << LAYER_INSPECAO;
        camInspecao.nearClipPlane   = 0.01f;
        camInspecao.farClipPlane    = 30f;
        camInspecao.fieldOfView     = 50f;
        camInspecao.depth           = -2f; // abaixo da câmera principal
        camInspecao.enabled         = false;

        renderTex               = new RenderTexture(resolucaoRT, resolucaoRT, 24);
        renderTex.antiAliasing  = 4;
        camInspecao.targetTexture = renderTex;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // API pública
    // ────────────────────────────────────────────────────────────────────────────

    public void AbrirInspecao(GameObject original)
    {
        if (EstaInspecionando) return;

        EstaInspecionando    = true;
        Cursor.lockState     = CursorLockMode.None;
        Cursor.visible       = true;

        // Pausar FuncoesPlayer para não mover / olhar enquanto inspeciona
        if (funcoesPlayer != null) funcoesPlayer.enabled = false;

        // Clonar objeto na staging area
        copiaObjeto = Instantiate(original, STAGING, Quaternion.identity);

        // Desativar scripts da cópia (física, IA etc.) mas manter renderers
        foreach (var m in copiaObjeto.GetComponentsInChildren<MonoBehaviour>())
            m.enabled = false;
        foreach (var r in copiaObjeto.GetComponentsInChildren<Renderer>())
            r.enabled = true;

        // ── Pivô: ponto de rotação exatamente no centro geométrico ──────────────
        Bounds b = CalcularBounds(copiaObjeto);

        pivo = new GameObject("_PivoInspecao");
        pivo.transform.position = STAGING;           // pivô fica no ponto-alvo da câmera

        copiaObjeto.transform.SetParent(pivo.transform);
        // Desloca o clone para que o centro dos bounds caia exatamente em STAGING
        copiaObjeto.transform.position += (STAGING - b.center);

        DefinirLayerRecursivo(pivo, LAYER_INSPECAO); // inclui clone e filhos
        // ───────────────────────────────────────────────────────────────────────

        // Distância inicial proporcional ao tamanho do objeto
        float tamanho = b.extents.magnitude;
        distanciaAtual = Mathf.Clamp(tamanho * 2.5f, distanciaMin, distanciaMax);

        rotacaoAtual = Quaternion.Euler(15f, 0f, 0f);

        AtualizarCamera();
        camInspecao.enabled = true;
    }

    public void FecharInspecao()
    {
        EstaInspecionando   = false;
        camInspecao.enabled = false;

        if (!Application.isMobilePlatform && ControlesMobile.Instancia == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        if (funcoesPlayer != null) funcoesPlayer.enabled = true;
        if (pivo != null) { Destroy(pivo); pivo = null; copiaObjeto = null; }
    }

    // ────────────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!EstaInspecionando) return;

        // --- Rotação: Touch (1 dedo) ou Mouse ---
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                rotacaoAtual = Quaternion.AngleAxis(t.deltaPosition.x * sensibilidadeRot * 0.2f, Vector3.up) * rotacaoAtual;
                rotacaoAtual = Quaternion.AngleAxis(-t.deltaPosition.y * sensibilidadeRot * 0.2f, Vector3.right) * rotacaoAtual;
            }
        }
        else if (Input.touchCount == 0) // Mouse fallback
        {
            if (Input.GetMouseButtonDown(0)) { arrastando = true; ultimaMouse = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0))   { arrastando = false; }

            if (arrastando)
            {
                Vector2 delta = (Vector2)Input.mousePosition - ultimaMouse;
                ultimaMouse   = Input.mousePosition;
                rotacaoAtual = Quaternion.AngleAxis( delta.x * sensibilidadeRot, Vector3.up)    * rotacaoAtual;
                rotacaoAtual = Quaternion.AngleAxis(-delta.y * sensibilidadeRot, Vector3.right) * rotacaoAtual;
            }
        }

        // --- Zoom: Touch (Pinch/2 dedos) ou Mouse Scroll ---
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 posPrev0 = t0.position - t0.deltaPosition;
            Vector2 posPrev1 = t1.position - t1.deltaPosition;

            float prevMag = (posPrev0 - posPrev1).magnitude;
            float atualMag = (t0.position - t1.position).magnitude;
            float diferenca = atualMag - prevMag;

            distanciaAtual = Mathf.Clamp(distanciaAtual - diferenca * sensibilidadeZoom * 0.05f, distanciaMin, distanciaMax);
        }
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distanciaAtual = Mathf.Clamp(distanciaAtual - scroll * sensibilidadeZoom, distanciaMin, distanciaMax);
        }

        if (pivo != null)
            pivo.transform.rotation = rotacaoAtual;

        AtualizarCamera();
    }

    void AtualizarCamera()
    {
        if (camInspecao == null) return;
        camInspecao.transform.position = STAGING + new Vector3(0f, 0f, -distanciaAtual);
        camInspecao.transform.LookAt(STAGING);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // OnGUI — overlay + RenderTexture + botão
    // ────────────────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (!EstaInspecionando) return;
        if (!estilosOk) IniciarEstilos();

        float sw = Screen.width, sh = Screen.height;

        // 1. Overlay escuro cobrindo toda a tela
        GUI.Box(new Rect(0, 0, sw, sh), GUIContent.none, estiloFundo);

        // 2. RenderTexture centralizado (quadrado)
        float size = Mathf.Min(sw, sh) * 0.65f;
        var rectRT = new Rect((sw - size) * 0.5f, (sh - size) * 0.5f, size, size);
        if (renderTex != null)
            GUI.DrawTexture(rectRT, renderTex, ScaleMode.ScaleToFit, false);

        // 3. Botão "← Sair" no canto superior direito
        float bw = 130f, bh = 44f, m = 20f;
        if (GUI.Button(new Rect(sw - bw - m, m, bw, bh), "← Sair", estiloBotao))
            FecharInspecao();

        // 4. Dica de controles na parte de baixo
        GUI.Label(new Rect(0, sh - 38f, sw, 28f),
                  "🖱  Arraste para girar   |   Scroll para aproximar / afastar",
                  estiloDica);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────────

    void IniciarEstilos()
    {
        texFundo = MakeColorTex(corOverlay);

        estiloFundo = new GUIStyle(GUI.skin.box);
        estiloFundo.normal.background = texFundo;
        estiloFundo.border = new RectOffset(0, 0, 0, 0);

        estiloBotao = new GUIStyle(GUI.skin.button);
        estiloBotao.fontSize  = 18;
        estiloBotao.fontStyle = FontStyle.Bold;
        estiloBotao.normal.textColor = Color.white;
        estiloBotao.hover.textColor  = Color.yellow;
        estiloBotao.normal.background = MakeColorTex(new Color(0.15f, 0.15f, 0.15f, 0.95f));
        estiloBotao.hover.background  = MakeColorTex(new Color(0.28f, 0.28f, 0.28f, 0.95f));
        estiloBotao.active.background = MakeColorTex(new Color(0.08f, 0.08f, 0.08f, 0.95f));
        estiloBotao.border = new RectOffset(6, 6, 6, 6);

        estiloDica = new GUIStyle();
        estiloDica.fontSize  = 14;
        estiloDica.fontStyle = FontStyle.Italic;
        estiloDica.normal.textColor = new Color(1f, 1f, 1f, 0.50f);
        estiloDica.alignment = TextAnchor.MiddleCenter;

        estilosOk = true;
    }

    static Texture2D MakeColorTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static void DefinirLayerRecursivo(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform filho in go.transform)
            DefinirLayerRecursivo(filho.gameObject, layer);
    }

    static Bounds CalcularBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
