using UnityEngine;

/// <summary>
/// Coloque este script em cada objeto que o jogador pode "ler".
/// O objeto também precisa ter a tag "Leitura" e um Collider.
///
/// A detecção do cursor é feita por PHYSICS (cursor do mouse sobre o Collider)
/// via OverlapSphere a partir da câmera, igual ao sistema InterecaoObjetos —
/// mas aqui usamos Raycast do centro da câmera, assim funciona com mira travada.
///
/// IMPORTÂNCIA: coloque o script LeituraUI no mesmo GameObject que FuncoesPlayer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BaseInformacoes : MonoBehaviour
{
    [Header("Conteúdo")]
    [TextArea(4, 20)]
    public string textoLeitura = "Texto de teste";
    [Tooltip("Cor que o texto terá ao ser lido na tela")]
    public Color corDoTextoLido = Color.white;

    [Header("Configurações")]
    public KeyCode teclaLeitura = KeyCode.E;
    public float   alcance      = 3f;

    [Header("UI de dica")]
    public Color corTexto    = Color.white;
    public int   tamanhoFont = 22;

    // ─── Internos ──────────────────────────────────────────────────────────────
    private Camera    camPrincipal;
    private bool      jogadorPerto   = false;
    private GUIStyle  estiloBotao;
    private bool      estilosOk = false;

    // ───────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Tenta pegar a câmera pelo FuncoesPlayer (igual ao InterecaoObjetos)
        var jogador = FindObjectOfType<FuncoesPlayer>();
        if (jogador != null && jogador.cameraPrimeiraPessoa != null)
            camPrincipal = jogador.cameraPrimeiraPessoa.GetComponent<Camera>();
        if (camPrincipal == null)
            camPrincipal = Camera.main;
    }

    void Update()
    {
        // Não detecta se a tela de leitura já estiver aberta
        if (LeituraUI.Instancia != null && LeituraUI.Instancia.EstaLendo)
        {
            jogadorPerto = false;
            return;
        }

        DetectarCursor();

        if (jogadorPerto && Input.GetKeyDown(teclaLeitura))
        {
            if (LeituraUI.Instancia != null)
                LeituraUI.Instancia.AbrirLeitura(textoLeitura, corDoTextoLido);
            else
                Debug.LogWarning("[BaseInformacoes] LeituraUI não encontrado no Player!");
        }
    }

    void DetectarCursor()
    {
        if (camPrincipal == null) { jogadorPerto = false; return; }

        Ray raio = camPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(raio, out RaycastHit hit, alcance))
        {
            // Verifica se o colisor atingido é deste objeto (ou filho)
            Transform t = hit.collider.transform;
            bool acertou = false;
            while (t != null)
            {
                if (t == this.transform) { acertou = true; break; }
                t = t.parent;
            }
            jogadorPerto = acertou;
        }
        else
        {
            jogadorPerto = false;
        }
    }

    void OnGUI()
    {
        if (!jogadorPerto) return;
        if (LeituraUI.Instancia != null && LeituraUI.Instancia.EstaLendo) return;

        if (!estilosOk) IniciarEstilos();

        float cx = Screen.width  / 2f;
        float cy = Screen.height / 2f + 50f;
        float w  = 260f, h = 44f;
        Rect  ret = new Rect(cx - w / 2f, cy, w, h);

        // Botão clicável — funciona igual ao pressionar [E]
        if (GUI.Button(ret, "Toque para ler", estiloBotao))
        {
            if (LeituraUI.Instancia != null)
                LeituraUI.Instancia.AbrirLeitura(textoLeitura, corDoTextoLido);
            else
                Debug.LogWarning("[BaseInformacoes] LeituraUI não encontrado no Player!");
        }
    }

    void IniciarEstilos()
    {
        estiloBotao = new GUIStyle(GUI.skin.button);
        estiloBotao.fontSize  = tamanhoFont;
        estiloBotao.fontStyle = FontStyle.Bold;
        estiloBotao.alignment = TextAnchor.MiddleCenter;

        estilosOk = true;
    }
}
