using UnityEngine;

/// <summary>
/// Componente de dados para o gerador procedural de tábuas.
/// Adicione este script a um GameObject vazio na cena.
/// Use o botão "Gerar Superfície" que aparecerá no Inspector.
/// </summary>
public class PlankManager : MonoBehaviour
{
    [Header("Dimensões da Área")]
    public float AreaWidth  = 6f;
    public float AreaLength = 6f;

    [Header("Tábua Base")]
    public float PlankBaseWidth  = 0.15f;
    public float PlankBaseLength = 1.2f;
    public float PlankHeight     = 0.02f;   // espessura (Y) da tábua

    [Header("Gap entre Tábuas")]
    [Range(0f, 0.05f)]
    public float Gap = 0.005f;

    [Header("Variação de Escala (aleatoriedade)")]
    [Range(0f, 0.3f)] public float ScaleVarX = 0.08f;   // variação na largura
    [Range(0f, 0.3f)] public float ScaleVarY = 0.5f;    // variação na espessura
    [Range(0f, 0.3f)] public float ScaleVarZ = 0.25f;   // variação no comprimento

    [Header("Variação de Rotação Y (empenamento) °")]
    [Range(0f, 5f)]
    public float RotationVarY = 1.5f;

    [Header("Textura das Tábuas")]
    [Tooltip("Arraste aqui qualquer textura do projeto para aplicar nas tábuas")]
    public Texture2D TexturaEscolhida;

    [Header("Referência interna (não editar)")]
    public GameObject PlankParent;   // referência ao PAI gerado
}
