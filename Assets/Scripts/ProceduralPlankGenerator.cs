using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor customizado para PlankManager.
/// Aparece no Inspector quando o componente PlankManager está selecionado.
/// Pasta: Assets/Editor/ProceduralPlankGenerator.cs
/// </summary>
[CustomEditor(typeof(PlankManager))]
public class ProceduralPlankGenerator : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha todos os campos padrão do PlankManager
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Gerador ──", EditorStyles.boldLabel);

        PlankManager mgr = (PlankManager)target;

        // Botão principal
        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
        if (GUILayout.Button("🪵  Gerar Superfície", GUILayout.Height(36)))
        {
            GerarSuperficie(mgr);
        }

        GUI.backgroundColor = new Color(0.4f, 0.6f, 1.0f);
        if (GUILayout.Button("🎨  Aplicar Textura nas Tábuas", GUILayout.Height(28)))
        {
            AplicarTexturaExistentes(mgr);
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑  Limpar Geração", GUILayout.Height(28)))
        {
            LimparGeracao(mgr);
        }

        GUI.backgroundColor = Color.white;
    }

    // -------------------------------------------------------
    // Gera a superfície de tábuas
    // -------------------------------------------------------
    void GerarSuperficie(PlankManager mgr)
    {
        // Registra o undo para poder desfazer no Editor
        Undo.RegisterFullObjectHierarchyUndo(mgr.gameObject, "Gerar Superfície Madeira");

        LimparGeracao(mgr);  // remove geração anterior

        // --- Cria o GameObject pai ---
        GameObject pai = new GameObject("Tabuas_Pai");
        pai.transform.SetParent(mgr.transform);
        pai.transform.localPosition = Vector3.zero;
        mgr.PlankParent = pai;
        Undo.RegisterCreatedObjectUndo(pai, "Criar Pai Tábuas");

        // --- Material / Textura ---
        Material mat = ObterOuCriarMaterial(mgr);

        // --- Geração das tábuas ---
        System.Random rng = new System.Random(42); // seed fixa → resultado reproduzível

        float x = 0f;

        while (x < mgr.AreaWidth)
        {
            float largTabua = mgr.PlankBaseWidth * (1f + Lerp(rng, -mgr.ScaleVarX, mgr.ScaleVarX));
            largTabua = Mathf.Clamp(largTabua, 0.05f, mgr.AreaWidth - x);

            // Offset de início em Z para juntas desencontradas
            float offsetZ = -(float)(rng.NextDouble() * mgr.PlankBaseLength * 0.6f);
            float z = offsetZ;

            while (z < mgr.AreaLength)
            {
                float comprTabua = mgr.PlankBaseLength * (1f + Lerp(rng, -mgr.ScaleVarZ, mgr.ScaleVarZ));
                comprTabua = Mathf.Max(comprTabua, 0.1f);

                float espessura = mgr.PlankHeight * (1f + Lerp(rng, -mgr.ScaleVarY, mgr.ScaleVarY));
                espessura = Mathf.Max(espessura, 0.005f);

                // Posição central da tábua
                float cx = x + largTabua / 2f;
                float cz = Mathf.Clamp(z + comprTabua / 2f, 0f, mgr.AreaLength);

                // Ignora segmentos fora da área
                if (z + comprTabua > 0f && z < mgr.AreaLength)
                {
                    GameObject tabua = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tabua.name = $"Tabua_{x:F2}_{z:F2}";
                    tabua.transform.SetParent(pai.transform);

                    // Escala = dimensões reais descontando o gap
                    float escX = (largTabua - mgr.Gap);
                    float escZ = Mathf.Min(comprTabua - mgr.Gap, mgr.AreaLength - z);
                    escZ = Mathf.Clamp(escZ, 0.02f, comprTabua);

                    tabua.transform.localScale    = new Vector3(escX, espessura, escZ);
                    tabua.transform.localPosition = new Vector3(cx, espessura / 2f, cz);

                    // Leve rotação no Y para efeito de empeno rústico
                    float rotY = Lerp(rng, -mgr.RotationVarY, mgr.RotationVarY);
                    tabua.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

                    // Aplica material
                    MeshRenderer mr = tabua.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;

                    // UV tiling proporcional ao tamanho físico
                    // (via MaterialPropertyBlock para não criar instâncias)
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    mr.GetPropertyBlock(mpb);
                    mr.SetPropertyBlock(mpb);

                    Undo.RegisterCreatedObjectUndo(tabua, "Criar Tábua");
                }

                z += comprTabua;
            }

            x += largTabua;
        }

        // --- Salva como Prefab ---
        SalvarPrefab(pai, mgr);

        Debug.Log($"[ProceduralPlankGenerator] Superfície gerada com {pai.transform.childCount} tábuas.");
        EditorUtility.DisplayDialog("✅ Concluído",
            $"Superfície gerada com {pai.transform.childCount} tábuas!\nPrefab salvo em Assets/Prefabs/Generated/", "OK");
    }

    // -------------------------------------------------------
    // Aplica textura em tábuas já geradas (sem precisar regenerar)
    // -------------------------------------------------------
    void AplicarTexturaExistentes(PlankManager mgr)
    {
        Material mat = ObterOuCriarMaterial(mgr);

        // Encontra o pai das tábuas
        GameObject pai = mgr.PlankParent;
        if (pai == null)
        {
            // Tenta achar por nome na hierarquia
            Transform t = mgr.transform.Find("Tabuas_Pai");
            if (t != null) pai = t.gameObject;
        }

        if (pai == null)
        {
            EditorUtility.DisplayDialog("Atenção", "Nenhuma geração encontrada. Clique em 'Gerar Superfície' primeiro.", "OK");
            return;
        }

        int count = 0;
        foreach (Transform filho in pai.transform)
        {
            MeshRenderer mr = filho.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.RecordObject(mr, "Aplicar Textura Tábua");
                mr.sharedMaterial = mat;
                count++;
            }
        }

        EditorUtility.SetDirty(pai);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ProceduralPlankGenerator] Textura aplicada em {count} tábuas.");
        EditorUtility.DisplayDialog("✅ Textura Aplicada!", $"Material com textura aplicado em {count} tábuas.", "OK");
    }

    // -------------------------------------------------------
    // Remove a geração anterior
    // -------------------------------------------------------
    void LimparGeracao(PlankManager mgr)
    {
        if (mgr.PlankParent != null)
        {
            Undo.DestroyObjectImmediate(mgr.PlankParent);
            mgr.PlankParent = null;
        }

        // Segurança: remove qualquer "Tabuas_Pai" residual filho do manager
        foreach (Transform filho in mgr.transform)
        {
            if (filho != null && filho.name == "Tabuas_Pai")
            {
                Undo.DestroyObjectImmediate(filho.gameObject);
                break;
            }
        }
    }

    // -------------------------------------------------------
    // Cria ou reutiliza o material WoodPlankMaterial
    // -------------------------------------------------------
    Material ObterOuCriarMaterial(PlankManager mgr = null)
    {
        const string matPath = "Assets/Materials/WoodPlankMaterial.mat";

        // Tenta carregar já existente
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            GarantirPasta("Assets/Materials");
            mat = new Material(Shader.Find("Standard"));
            mat.name = "WoodPlankMaterial";
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Prioridade 1: textura escolhida no Inspector do PlankManager
        Texture2D tex = (mgr != null) ? mgr.TexturaEscolhida : null;

        // Prioridade 2: fallback para arquivo fixo (compatibilidade anterior)
        if (tex == null)
        {
            const string texPath = "Assets/Textures/Wood_Diffuse.png";
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        if (tex != null)
        {
            mat.mainTexture      = tex;
            mat.mainTextureScale = new Vector2(2f, 2f);
            EditorUtility.SetDirty(mat);
        }
        else
        {
            // Cor âmbar de madeira quando não há textura nenhuma
            mat.color = new Color(0.72f, 0.52f, 0.30f);
        }

        AssetDatabase.SaveAssets();
        return mat;
    }

    // -------------------------------------------------------
    // Salva o conjunto de tábuas como Prefab
    // -------------------------------------------------------
    void SalvarPrefab(GameObject pai, PlankManager mgr)
    {
        const string pastaBase   = "Assets/Prefabs/Generated";
        GarantirPasta("Assets/Prefabs");
        GarantirPasta(pastaBase);

        string prefabPath = $"{pastaBase}/WoodSurface.prefab";

        // Se já existir, sobrescreve
        bool sucesso;
        PrefabUtility.SaveAsPrefabAssetAndConnect(pai, prefabPath, InteractionMode.AutomatedAction, out sucesso);

        if (sucesso)
            AssetDatabase.Refresh();
        else
            Debug.LogWarning("[ProceduralPlankGenerator] Não foi possível salvar o Prefab.");
    }

    // -------------------------------------------------------
    // Garante criação de pasta no projeto sem erro
    // -------------------------------------------------------
    void GarantirPasta(string caminho)
    {
        if (!AssetDatabase.IsValidFolder(caminho))
        {
            string[] partes = caminho.Split('/');
            string atual = partes[0];
            for (int i = 1; i < partes.Length; i++)
            {
                string proximo = atual + "/" + partes[i];
                if (!AssetDatabase.IsValidFolder(proximo))
                    AssetDatabase.CreateFolder(atual, partes[i]);
                atual = proximo;
            }
        }
    }

    // Lerp auxiliar com System.Random (não usa UnityEngine.Random)
    float Lerp(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
