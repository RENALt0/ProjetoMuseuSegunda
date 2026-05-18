using UnityEngine;

/// <summary>
/// Coloque este script no mesmo GameObject que tem o FuncoesPlayer (o Player).
/// Todos os objetos com a tag "Interagivel" são detectados automaticamente.
/// </summary>
public class InterecaoObjetos : MonoBehaviour
{
    [Header("Configurações de Interação")]
    public float alcanceInteracao = 3f;
    public KeyCode teclaInteracao = KeyCode.E;

    [Header("UI de Interação")]
    public Color corTexto = Color.white;
    public int tamanhoFonte = 22;

    private Camera camPrincipal;
    private GameObject objetoAtual    = null;
    private GameObject objetoAnterior = null;
    private GUIStyle estiloUI;
    private GUIStyle estiloBotao;      // cache — não recriar por frame
    private bool      isMobile;        // cache — não chamar SystemInfo por frame
    private string    textoPrompt;     // cache — não recriar string por frame
    private Vector2   tamanhoTextoCache;

    void Start()
    {
        var fp = GetComponent<FuncoesPlayer>();
        if (fp != null && fp.cameraPrimeiraPessoa != null)
            camPrincipal = fp.cameraPrimeiraPessoa.GetComponent<Camera>();
        if (camPrincipal == null)
            camPrincipal = Camera.main;

        estiloUI = new GUIStyle();
        estiloUI.alignment  = TextAnchor.MiddleCenter;
        estiloUI.fontSize   = tamanhoFonte;
        estiloUI.fontStyle  = FontStyle.Bold;

        // Cache imutáveis — calculados uma única vez
        isMobile   = SystemInfo.deviceType == DeviceType.Handheld;
        textoPrompt = isMobile ? "TOCAR PARA INTERAGIR" : "Aperte [E] ou Clique para interagir";
    }

    void Update()
    {
        // Bloquear detecção enquanto o modo inspeção OU leitura estiver aberto
        bool bloqueado = (InspecaoObjeto.Instancia != null && InspecaoObjeto.Instancia.EstaInspecionando)
                      || (LeituraUI.Instancia      != null && LeituraUI.Instancia.EstaLendo);

        if (bloqueado)
        {
            objetoAnterior = null;
            objetoAtual    = null;
            return;
        }

        DetectarObjeto();

        if (objetoAtual != null && Input.GetKeyDown(teclaInteracao))
        {
            ExecutarInteracao();
        }
    }

    void ExecutarInteracao()
    {
        if (objetoAtual == null) return;

        if (InspecaoObjeto.Instancia != null)
            InspecaoObjeto.Instancia.AbrirInspecao(objetoAtual);
        else
        {
            objetoAtual.SendMessage("AoInteragir", SendMessageOptions.DontRequireReceiver);
            Debug.Log($"[Interação] Interagindo com: {objetoAtual.name}");
        }
    }

    void DetectarObjeto()
    {
        Ray raio = camPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        GameObject novoObjeto = null;

        if (Physics.Raycast(raio, out hit, alcanceInteracao))
        {
            Transform t = hit.collider.transform;
            while (t != null)
            {
                if (t.CompareTag("Interagivel")) { novoObjeto = t.gameObject; break; }
                t = t.parent;
            }
        }

        objetoAnterior = novoObjeto;
        objetoAtual    = novoObjeto;
    }

    void OnGUI()
    {
        if (objetoAtual == null) return;
        if (InspecaoObjeto.Instancia != null && InspecaoObjeto.Instancia.EstaInspecionando) return;
        if (LeituraUI.Instancia      != null && LeituraUI.Instancia.EstaLendo)              return;

        // Cria o estilo cacheado na primeira vez que o OnGUI rodar
        // (GUI.skin só é válido dentro de OnGUI, por isso não pode ir no Start)
        if (estiloBotao == null)
        {
            estiloBotao           = new GUIStyle(GUI.skin.button);
            estiloBotao.fontSize  = tamanhoFonte;
            estiloBotao.fontStyle = FontStyle.Bold;
            estiloBotao.alignment = TextAnchor.MiddleCenter;
            // Calcula tamanho uma única vez (textoPrompt é constante)
            tamanhoTextoCache = estiloBotao.CalcSize(new GUIContent(textoPrompt));
        }

        float w  = tamanhoTextoCache.x + 30f;
        float h  = Mathf.Max(45f, tamanhoTextoCache.y + 15f);
        float cx = Screen.width  / 2f;
        float cy = Screen.height / 2f + 50f;
        Rect ret = new Rect(cx - w / 2f, cy, w, h);

        if (GUI.Button(ret, textoPrompt, estiloBotao))
        {
            ExecutarInteracao();
        }
    }
}
