using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gera um chão de madeira procedural com tábuas não simétricas.
/// Coloque este script em um GameObject vazio. 
/// Arraste o Material de madeira para o campo "Material Madeira" no Inspector.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GerarChaoMadeira : MonoBehaviour
{
    [Header("Tamanho do Chão")]
    public float larguraTotal  = 10f;
    public float comprimentoTotal = 10f;

    [Header("Tábuas")]
    [Tooltip("Largura mínima de cada tábua (metros)")]
    public float larguraMinTabua = 0.12f;
    [Tooltip("Largura máxima de cada tábua (metros)")]
    public float larguraMaxTabua = 0.20f;
    [Tooltip("Comprimento mínimo de um segmento de tábua")]
    public float comprMinSegmento = 0.8f;
    [Tooltip("Comprimento máximo de um segmento de tábua")]
    public float comprMaxSegmento = 1.8f;
    [Tooltip("Offset aleatório de início por linha (simula juntas desencontradas)")]
    public float offsetMaximoLinha = 0.5f;
    [Tooltip("Gap visual entre tábuas (metros)")]
    public float gap = 0.004f;

    [Header("Textura")]
    [Tooltip("Quantos metros do mundo = 1 tile completo da textura")]
    public float escalaUV = 0.25f;  // 1 tile a cada 0.25m → textura densa, não esticada

    [Header("Material")]
    public Material materialMadeira;

    void Start()
    {
        GerarMalha();
    }

    void GerarMalha()
    {
        List<Vector3> vertices  = new List<Vector3>();
        List<int>     triangles = new List<int>();
        List<Vector2> uvs       = new List<Vector2>();

        float x = 0f;

        while (x < larguraTotal)
        {
            // Largura aleatória desta fila de tábuas
            float largTabua = Random.Range(larguraMinTabua, larguraMaxTabua);
            largTabua = Mathf.Min(largTabua, larguraTotal - x);

            // Offset de inicio no eixo Z para esta fila
            float inicioZ = -Random.Range(0f, offsetMaximoLinha);

            float z = inicioZ;

            while (z < comprimentoTotal)
            {
                float comprSeg = Random.Range(comprMinSegmento, comprMaxSegmento);

                float x0 = x + gap;
                float x1 = x + largTabua - gap;
                float z0 = z + gap;
                float z1 = Mathf.Min(z + comprSeg - gap, comprimentoTotal);

                if (x1 <= x0 || z1 <= z0) { z += comprSeg; continue; }

                int baseIdx = vertices.Count;

                // 4 vértices do quad (plano XZ, Y = 0)
                vertices.Add(new Vector3(x0, 0, z0));
                vertices.Add(new Vector3(x1, 0, z0));
                vertices.Add(new Vector3(x0, 0, z1));
                vertices.Add(new Vector3(x1, 0, z1));

                // UV em espaço-mundo: divide posição XZ pela escala
                // → textura tila corretamente independente do tamanho da tábua
                uvs.Add(new Vector2(x0 * escalaUV, z0 * escalaUV));
                uvs.Add(new Vector2(x1 * escalaUV, z0 * escalaUV));
                uvs.Add(new Vector2(x0 * escalaUV, z1 * escalaUV));
                uvs.Add(new Vector2(x1 * escalaUV, z1 * escalaUV));

                // Dois triângulos formando o quad
                triangles.Add(baseIdx + 0);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 1);

                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 3);

                z += comprSeg;
            }

            x += largTabua;
        }

        // --- Monta a Mesh ---
        Mesh mesh = new Mesh();
        mesh.name = "ChaoMadeira";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // suporta muitos vértices

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        // Aplica material
        if (materialMadeira != null)
            GetComponent<MeshRenderer>().material = materialMadeira;

        // Gera collider para o chão
        MeshCollider col = gameObject.GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        Debug.Log($"[ChaoMadeira] Gerado com {vertices.Count} vértices e {triangles.Count / 3} triângulos.");
    }

#if UNITY_EDITOR
    // Botão no Inspector para regenerar em tempo de edição
    [ContextMenu("Regenerar Chão")]
    void RegenerarEditor()
    {
        Random.InitState(System.Environment.TickCount);
        GerarMalha();
    }
#endif
}
