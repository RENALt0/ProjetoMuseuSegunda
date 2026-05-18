using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de Pausa — cria toda a UI via código, sem precisar montar nada no Editor.
/// Basta adicionar este script a qualquer GameObject na cena (ex: um vazio chamado "MenuPausa").
/// - ESC (PC) abre/fecha o menu
/// - Botão ☰ no canto superior esquerdo também abre/fecha
/// </summary>
public class MenuPausa : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Nome exato da cena do menu principal para o botão Sair")]
    public string cenaMenuPrincipal = "MenuPrincipal";

    [Header("Visual")]
    [Tooltip("Arraste aqui a fonte da pasta Fontes (ex: VCR_OSD_MONO)")]
    public Font fontePrincipal;

    [Header("Áudio")]
    [Tooltip("AudioSource da música de fundo (pode deixar vazio por enquanto)")]
    public AudioSource musicaFundo;

    // ── Referências internas (criadas por código) ────────────────────────────
    private GameObject painelPausa;
    private bool pausado = false;
    private Slider sliderVolume;
    private Toggle toggleMute;
    private float volumeAntesDeMutar = 0.5f;

    // Cache: evita recalcular a cada frame ou chamada de método
    private Font _fonte;
    private bool _isMobile;
    private bool _ignorarCallbackVolume = false;

    // Vetor reutilizado em muitos RectTransforms — evita criar struct repetidamente
    private static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

    // Singleton leve
    public static MenuPausa Instancia { get; private set; }
    public static bool EstaPausado => Instancia != null && Instancia.pausado;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Instancia = this;

        // Cache da fonte resolvida uma única vez
        _fonte    = fontePrincipal != null
                    ? fontePrincipal
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Cache da plataforma — Application.isMobilePlatform não muda em runtime
        _isMobile = Application.isMobilePlatform;

        CriarUI();
    }

    void Start()
    {
        // Configura a música: loop ativado e volume inicial em 50%
        if (musicaFundo != null)
        {
            musicaFundo.loop   = true;
            musicaFundo.volume = 0.5f;
            if (!musicaFundo.isPlaying)
                musicaFundo.Play();
        }

        // Volume global também começa em 50%
        AudioListener.volume = 0.5f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            AlternarPausa();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CRIAÇÃO DA UI POR CÓDIGO
    // ════════════════════════════════════════════════════════════════════════

    void CriarUI()
    {
        // ── Canvas principal ──────────────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas_MenuPausa");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // na frente de tudo

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem (necessário para botões funcionarem) ──────────────
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject evSys = new GameObject("EventSystem");
            evSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Botão hamburguer ☰ (canto superior esquerdo) ──────────────────
        CriarBotaoHamburger(canvasGO);

        // ── Painel de pausa (começa desativado) ───────────────────────────
        painelPausa = CriarPainelPausa(canvasGO);
        painelPausa.SetActive(false);
    }

    // ── Botão ☰ ──────────────────────────────────────────────────────────────
    void CriarBotaoHamburger(GameObject canvas)
    {
        GameObject btnGO = new GameObject("BotaoHamburger");
        btnGO.transform.SetParent(canvas.transform, false);

        // Imagem de fundo semitransparente
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        // Posição: canto superior esquerdo
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(40, -40);
        rt.sizeDelta        = new Vector2(160, 120);

        // Texto ☰
        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(btnGO.transform, false);
        Text txt = textoGO.AddComponent<Text>();
        txt.text      = "☰";
        txt.font      = _fonte;
        txt.fontSize  = 72;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform rtTxt = textoGO.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;

        // Botão
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
        cb.pressedColor     = new Color(1f, 1f, 1f, 0.3f);
        btn.colors = cb;
        btn.onClick.AddListener(AlternarPausa);
    }

    // ── Painel central de pausa ───────────────────────────────────────────────
    GameObject CriarPainelPausa(GameObject canvas)
    {
        // Fundo escuro cobrindo a tela toda
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvas.transform, false);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform rtOv = overlay.GetComponent<RectTransform>();
        rtOv.anchorMin = Vector2.zero;
        rtOv.anchorMax = Vector2.one;
        rtOv.offsetMin = Vector2.zero;
        rtOv.offsetMax = Vector2.zero;

        // Caixa central — aumentada para caber os novos controles
        GameObject painel = new GameObject("PainelPausa");
        painel.transform.SetParent(overlay.transform, false);
        Image painelImg = painel.AddComponent<Image>();
        painelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.97f);
        RectTransform rtP = painel.GetComponent<RectTransform>();
        rtP.anchorMin = Centro;
        rtP.anchorMax = Centro;
        rtP.pivot     = Centro;
        rtP.anchoredPosition = Vector2.zero;
        rtP.sizeDelta        = new Vector2(720, 760);

        // Título
        CriarTexto(painel, "PAUSADO", 60, new Vector2(0, 310), new Vector2(600, 100), Color.white);

        // Linha separadora
        CriarLinha(painel, new Vector2(0, 244), new Vector2(600, 4));

        // Botão Continuar
        CriarBotaoMenu(painel, "Continuar", new Vector2(0, 160),
            new Color(0.85f, 0.85f, 0.85f, 1f), Color.black, Continuar);

        // Botão Sair
        CriarBotaoMenu(painel, "Sair para o Menu", new Vector2(0, 20),
            new Color(0.85f, 0.85f, 0.85f, 1f), Color.black, Sair);

        // ── Linha separadora de áudio ──────────────────────────────────────
        CriarLinha(painel, new Vector2(0, -90), new Vector2(600, 4));

        // Label "Volume da Música"
        CriarTexto(painel, "Volume da Música", 36, new Vector2(0, -136), new Vector2(560, 60), new Color(0.8f, 0.8f, 0.8f, 1f));

        // Slider de volume
        sliderVolume = CriarSliderVolume(painel, new Vector2(0, -210));

        // Toggle de mute
        toggleMute = CriarToggleMute(painel, new Vector2(0, -296));

        // Retorna o overlay (que engloba tudo) para ser ativado/desativado
        return overlay;
    }

    // ── Slider de Volume ──────────────────────────────────────────────────────
    Slider CriarSliderVolume(GameObject pai, Vector2 pos)
    {
        GameObject sliderGO = new GameObject("SliderVolume");
        sliderGO.transform.SetParent(pai.transform, false);

        RectTransform rt = sliderGO.AddComponent<RectTransform>();
        rt.anchorMin = Centro;
        rt.anchorMax = Centro;
        rt.pivot = Centro;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560, 60);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 0.5f;

        // Trilha (background)
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.25f);
        bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Área de preenchimento
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5, 0);
        fillAreaRt.offsetMax = new Vector2(-15, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f, 1f); // azul
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        // Handle (bolinha)
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(40, 40);

        // Conectar partes ao Slider
        slider.fillRect      = fillRt;
        slider.handleRect    = handleRt;
        slider.targetGraphic = handleImg;

        // Evento: muda volume ao arrastar
        slider.onValueChanged.AddListener(AlterarVolume);

        return slider;
    }

    // ── Toggle de Mute ────────────────────────────────────────────────────────
    Toggle CriarToggleMute(GameObject pai, Vector2 pos)
    {
        GameObject toggleGO = new GameObject("ToggleMute");
        toggleGO.transform.SetParent(pai.transform, false);

        RectTransform rt = toggleGO.AddComponent<RectTransform>();
        rt.anchorMin = Centro;
        rt.anchorMax = Centro;
        rt.pivot = Centro;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 56);

        Toggle toggle = toggleGO.AddComponent<Toggle>();

        // Caixa do toggle (background)
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(toggleGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.pivot = new Vector2(0, 0.5f);
        bgRt.anchoredPosition = new Vector2(0, 0);
        bgRt.sizeDelta = new Vector2(48, 48);

        // Checkmark (marcação)
        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(bgGO.transform, false);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform checkRt = checkGO.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.1f, 0.1f);
        checkRt.anchorMax = new Vector2(0.9f, 0.9f);
        checkRt.offsetMin = Vector2.zero;
        checkRt.offsetMax = Vector2.zero;

        // Label "Mudo"
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(toggleGO.transform, false);
        Text labelTxt = labelGO.AddComponent<Text>();
        labelTxt.text      = "Mudo";
        labelTxt.font      = _fonte;
        labelTxt.fontSize  = 36;
        labelTxt.color     = new Color(0.8f, 0.8f, 0.8f, 1f);
        labelTxt.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0.5f);
        labelRt.anchorMax = new Vector2(1, 0.5f);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.anchoredPosition = new Vector2(64, 0);
        labelRt.sizeDelta = new Vector2(-64, 56);

        // Conectar ao Toggle
        toggle.targetGraphic = bgImg;
        toggle.graphic       = checkImg;
        toggle.isOn          = false;

        // Evento
        toggle.onValueChanged.AddListener(AlterarMute);

        return toggle;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void CriarTexto(GameObject pai, string conteudo, int tamanho,
                    Vector2 pos, Vector2 tam, Color cor)
    {
        GameObject go = new GameObject("Texto_" + conteudo);
        go.transform.SetParent(pai.transform, false);
        Text txt = go.AddComponent<Text>();
        txt.text      = conteudo;
        txt.font      = _fonte;
        txt.fontSize  = tamanho;
        txt.color     = cor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Centro;
        rt.anchorMax = Centro;
        rt.pivot = Centro;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    void CriarLinha(GameObject pai, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject("Linha");
        go.transform.SetParent(pai.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Centro;
        rt.anchorMax = Centro;
        rt.pivot = Centro;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    void CriarBotaoMenu(GameObject pai, string label, Vector2 pos,
                        Color corFundo, Color corTexto, UnityEngine.Events.UnityAction acao)
    {
        GameObject btnGO = new GameObject("Btn_" + label);
        btnGO.transform.SetParent(pai.transform, false);

        Image img = btnGO.AddComponent<Image>();
        img.color = corFundo;

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = Centro;
        rt.anchorMax = Centro;
        rt.pivot = Centro;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560, 110);

        // Texto do botão
        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(btnGO.transform, false);
        Text txt = textoGO.AddComponent<Text>();
        txt.text      = label;
        txt.font      = _fonte;
        txt.fontSize  = 44;
        txt.color     = corTexto;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        RectTransform rtTxt = textoGO.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
        cb.pressedColor     = new Color(0f, 0f, 0f, 0.3f);
        btn.colors = cb;
        btn.onClick.AddListener(acao);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LÓGICA DE ÁUDIO
    // ════════════════════════════════════════════════════════════════════════

    void AlterarVolume(float valor)
    {
        // Ignora o callback se foi acionado programaticamente (evita double-apply)
        if (_ignorarCallbackVolume) return;

        // Se estiver mutado e o usuário mexer no slider, desmuta sem re-disparar callbacks
        if (toggleMute != null && toggleMute.isOn)
        {
            _ignorarCallbackVolume = true;
            toggleMute.isOn = false;
            _ignorarCallbackVolume = false;
        }

        volumeAntesDeMutar = valor;
        AplicarVolume(valor);
    }

    void AlterarMute(bool mutado)
    {
        if (_ignorarCallbackVolume) return;

        if (mutado)
        {
            // Salva o volume atual e muta
            if (sliderVolume != null)
                volumeAntesDeMutar = sliderVolume.value;
            AplicarVolume(0f);
        }
        else
        {
            // Restaura o volume anterior
            AplicarVolume(volumeAntesDeMutar);
            if (sliderVolume != null)
                sliderVolume.value = volumeAntesDeMutar;
        }
    }

    void AplicarVolume(float valor)
    {
        // Aplica no AudioListener global (afeta todos os sons do jogo)
        AudioListener.volume = valor;

        // Se tiver um AudioSource específico da música, aplica nele também
        if (musicaFundo != null)
            musicaFundo.volume = valor;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LÓGICA DE PAUSA
    // ════════════════════════════════════════════════════════════════════════

    public void AlternarPausa()
    {
        if (pausado) Continuar();
        else         Pausar();
    }

    public void Pausar()
    {
        pausado = true;
        painelPausa.SetActive(true);
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Continuar()
    {
        pausado = false;
        painelPausa.SetActive(false);
        Time.timeScale = 1f;

        // Usa o valor cacheado no Awake — não chama a property a cada vez
        if (!_isMobile && ControlesMobile.Instancia == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    public void Sair()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(cenaMenuPrincipal);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instancia == this) Instancia = null;
    }
}
