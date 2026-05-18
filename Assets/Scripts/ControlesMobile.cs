using UnityEngine;

/// <summary>
/// Gerencia os controles de toque na tela para movimentação (Joystick virtual),
/// visão da câmera e botões de ação na UI.
/// </summary>
public class ControlesMobile : MonoBehaviour
{
    public static ControlesMobile Instancia { get; private set; }

    public Vector2 Movimento { get; private set; }
    public Vector2 CameraDelta { get; private set; }
    
    public bool PuloPressionadoDown { get; private set; }
    private bool puloAtual;
    private bool puloAnterior;

    public bool CorridaPressionada { get; private set; }

    [Header("Configurações do Joystick")]
    public float tamanhoJoystick = 150f;
    public float raioMovimento = 50f;

    private int dedoJoystick = -1;
    private int dedoCamera = -1;
    private Vector2 origemJoystick;
    private Vector2 posicaoJoystick;
    private Vector2 ultimaPosCamera;

    private Texture2D texFundoJoystic;
    private Texture2D texBolinhaJoystick;
    private GUIStyle estiloPulo;

    // Cache de Rects dos botões — só recalcula quando a tela muda de tamanho
    private Rect  _rectPulo, _rectCorrer;
    private float _lastScreenW, _lastScreenH;
    private float _btnTamanhoCache;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        texFundoJoystic = CriarTexturaCircular(new Color(1f, 1f, 1f, 0.2f));
        texBolinhaJoystick = CriarTexturaCircular(new Color(1f, 1f, 1f, 0.6f));
    }

    void Update()
    {
        Movimento = Vector2.zero;
        CameraDelta = Vector2.zero;

        // Lógica de detecção do Jump (GetButtonDown)
        PuloPressionadoDown = puloAtual && !puloAnterior;
        puloAnterior = puloAtual;

        foreach (Touch toque in Input.touches)
        {
            ProcessarToque(toque.fingerId, toque.position, toque.phase);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // --- Simulação de Touch via Mouse no Editor/PC ---
        if (Input.GetMouseButtonDown(0))
        {
            ProcessarToque(99, Input.mousePosition, TouchPhase.Began);
        }
        else if (Input.GetMouseButton(0))
        {
            ProcessarToque(99, Input.mousePosition, TouchPhase.Moved);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ProcessarToque(99, Input.mousePosition, TouchPhase.Ended);
        }
#endif
    }

    private void ProcessarToque(int fingerId, Vector2 position, TouchPhase phase)
    {
        // Evitar conflitos se o toque estiver no lado direito mas for nos botões
        // Reutiliza os Rects já cacheados no OnGUI (coordenadas GUI: Y=0 no topo)
        // Para Input.touches Y=0 é na base, por isso converte antes de comparar
        bool toqueNosBotoes = false;
        if (_btnTamanhoCache > 0f)
        {
            // Converte position (Y base) para coordenada GUI (Y topo)
            Vector2 posGui = new Vector2(position.x, Screen.height - position.y);
            toqueNosBotoes = _rectPulo.Contains(posGui) || _rectCorrer.Contains(posGui);
        }

        if (phase == TouchPhase.Began)
        {
            if (position.x < Screen.width / 2f && dedoJoystick == -1)
            {
                dedoJoystick = fingerId;
                origemJoystick = position;
                posicaoJoystick = position;
            }
            else if (position.x >= Screen.width / 2f && dedoCamera == -1 && !toqueNosBotoes)
            {
                dedoCamera = fingerId;
                ultimaPosCamera = position;
            }
        }
        else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
        {
            if (fingerId == dedoJoystick)
            {
                posicaoJoystick = position;
                Vector2 direcao = posicaoJoystick - origemJoystick;
                float distancia = Mathf.Min(direcao.magnitude, raioMovimento);
                posicaoJoystick = origemJoystick + direcao.normalized * distancia;
                
                Movimento = (posicaoJoystick - origemJoystick) / raioMovimento;
            }
            else if (fingerId == dedoCamera)
            {
                CameraDelta = position - ultimaPosCamera;
                ultimaPosCamera = position;
            }
        }
        else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
        {
            if (fingerId == dedoJoystick)
            {
                dedoJoystick = -1;
                Movimento = Vector2.zero;
            }
            else if (fingerId == dedoCamera)
            {
                dedoCamera = -1;
                CameraDelta = Vector2.zero;
            }
        }
    }

    void OnGUI()
    {
        // Ocultar em telas de interface
        if (InspecaoObjeto.Instancia != null && InspecaoObjeto.Instancia.EstaInspecionando) return;
        if (LeituraUI.Instancia != null && LeituraUI.Instancia.EstaLendo) return;

        // 1. Desenhar Joystick Virtual
        if (dedoJoystick != -1)
        {
            float tf = tamanhoJoystick;
            float tb = tamanhoJoystick * 0.4f;
            
            // GUI desenha com Y = 0 no TOPO, Input.mousePosition Y = 0 na BASE
            float guiY_origem = Screen.height - origemJoystick.y;
            float guiY_posicao = Screen.height - posicaoJoystick.y;

            GUI.DrawTexture(new Rect(origemJoystick.x - tf/2, guiY_origem - tf/2, tf, tf), texFundoJoystic);
            GUI.DrawTexture(new Rect(posicaoJoystick.x - tb/2, guiY_posicao - tb/2, tb, tb), texBolinhaJoystick);
        }

        // 2. Desenhar botões (Pular e Correr)
        // Recalcula Rects apenas se a resolução mudou
        if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
        {
            _lastScreenW     = Screen.width;
            _lastScreenH     = Screen.height;
            _btnTamanhoCache = Screen.height * 0.15f;

            float guiY_Correr = Screen.height - _btnTamanhoCache - 40;
            float guiY_Pular  = guiY_Correr   - _btnTamanhoCache - 20;
            float btnX        = Screen.width   - _btnTamanhoCache - 40;

            _rectPulo   = new Rect(btnX, guiY_Pular,  _btnTamanhoCache, _btnTamanhoCache);
            _rectCorrer = new Rect(btnX, guiY_Correr, _btnTamanhoCache, _btnTamanhoCache);
        }
        
        if (estiloPulo == null)
        {
            estiloPulo = new GUIStyle(GUI.skin.button);
            estiloPulo.fontSize = 20;
            estiloPulo.fontStyle = FontStyle.Bold;
        }

        puloAtual          = GUI.RepeatButton(_rectPulo,   "PULAR",  estiloPulo);
        CorridaPressionada = GUI.RepeatButton(_rectCorrer, "CORRER", estiloPulo);
    }

    private Texture2D CriarTexturaCircular(Color cor)
    {
        int tam = 128;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        Color clara = new Color(0,0,0,0);
        float centro = tam / 2f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) <= centro)
                    tex.SetPixel(x, y, cor);
                else
                    tex.SetPixel(x, y, clara);
            }
        }
        tex.Apply();
        return tex;
    }
}
