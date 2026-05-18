using UnityEngine;

/// <summary>
/// Configura a iluminação ambiente da cena de forma leve para mobile.
/// Adicione este script a um GameObject vazio na cena (ex: "GerenciadorCena").
/// Ele cria uma Directional Light se não houver nenhuma e ajusta a luz ambiente.
/// </summary>
public class ConfiguracaoIluminacao : MonoBehaviour
{
    [Header("Luz Direcional (Sol)")]
    [Tooltip("Intensidade da luz direcional")]
    public float intensidadeSol    = 1.2f;
    [Tooltip("Cor da luz direcional — branco quente é mais bonito")]
    public Color corSol            = new Color(1f, 0.96f, 0.88f, 1f); // branco levemente quente
    [Tooltip("Rotação da luz (ângulo do sol)")]
    public Vector3 rotacaoSol      = new Vector3(50f, -30f, 0f);

    [Header("Luz Ambiente (preenche sombras)")]
    [Tooltip("Cor da luz ambiente — cinza médio evita sombras completamente pretas")]
    public Color corAmbiente       = new Color(0.35f, 0.35f, 0.40f, 1f);

    [Header("Neblina (opcional — dá profundidade)")]
    public bool  usarNeblina       = true;
    public Color corNeblina        = new Color(0.7f, 0.72f, 0.76f, 1f);
    [Range(0.001f, 0.05f)]
    public float densidadeNeblina  = 0.008f;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        ConfigurarAmbiente();
        GarantirLuzDirecional();
        ConfigurarNeblina();
    }

    void ConfigurarAmbiente()
    {
        // Modo cor sólida: muito mais leve que Skybox procedural em mobile
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = corAmbiente;

        // Desativa reflexões em tempo real (custosas em mobile)
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity   = 0f;
    }

    void GarantirLuzDirecional()
    {
        // Verifica se já existe uma Directional Light na cena
        Light[] luzes = FindObjectsOfType<Light>();
        foreach (Light l in luzes)
            if (l.type == LightType.Directional) return; // já existe, não cria outra

        // Cria uma nova Directional Light leve
        GameObject sol = new GameObject("Sol_Direcional");
        sol.transform.rotation = Quaternion.Euler(rotacaoSol);

        Light luz = sol.AddComponent<Light>();
        luz.type      = LightType.Directional;
        luz.color     = corSol;
        luz.intensity = intensidadeSol;

        // Sombras suaves mas com resolução baixa — bom equilíbrio para mobile
        luz.shadows           = LightShadows.Soft;
        luz.shadowResolution  = UnityEngine.Rendering.LightShadowResolution.Low;
        QualitySettings.shadowDistance = 40f;   // sombras só até 40m
        luz.shadowBias        = 0.05f;

        // Sem GI em tempo real
        luz.renderMode        = LightRenderMode.Auto;
        luz.bounceIntensity   = 0f;

        Debug.Log("[ConfiguracaoIluminacao] Directional Light criada automaticamente.");
    }

    void ConfigurarNeblina()
    {
        RenderSettings.fog          = usarNeblina;
        RenderSettings.fogMode      = FogMode.ExponentialSquared; // suave e bonita
        RenderSettings.fogColor     = corNeblina;
        RenderSettings.fogDensity   = densidadeNeblina;
    }

#if UNITY_EDITOR
    // Permite visualizar as mudanças diretamente no Editor ao alterar os valores
    void OnValidate()
    {
        ConfigurarAmbiente();
        ConfigurarNeblina();
    }
#endif
}
