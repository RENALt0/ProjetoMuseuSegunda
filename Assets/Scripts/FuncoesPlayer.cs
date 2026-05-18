using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FuncoesPlayer : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 5f;
    public float velocidadeCorrida = 10f;
    public float forcaPulo = 1.5f;
    public float gravidade = -9.81f;

    [Header("Câmera / Mouse")]
    public Transform cameraPrimeiraPessoa;
    public float sensibilidadeX = 2f;   // horizontal
    public float sensibilidadeY = 2f;   // vertical  ← reduza aqui se ainda puxar
    public float limiteVertical = 80f;

    [Header("Mira (Crosshair)")]
    public Color corMira = Color.white;
    public float tamanhoMira = 20f;     // raio do círculo em pixels
    public float espessuraMira = 2f;
    public float tamanhoPonto = 4f;

    // --- Privados ---
    private CharacterController controller;
    private Vector3 velocidadeVertical;
    private float rotacaoX = 0f;
    private Texture2D texturaMira;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Desbloquear o mouse no Editor se estivermos usando os controles mobile, 
        // para podermos simular os toques livremente.
        if (Application.isMobilePlatform || ControlesMobile.Instancia != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        texturaMira = GerarTexturaMira(64);
    }

    void Update()
    {
        MoverPlayer();
        OlharComMouse();
    }

    // -------------------------------------------------------
    // Movimentação WASD + Shift (corrida) + Espaço (pulo)
    // -------------------------------------------------------
    void MoverPlayer()
    {
        bool noChao = controller.isGrounded;

        if (noChao && velocidadeVertical.y < 0f)
            velocidadeVertical.y = -2f;

        float eixoX = Input.GetAxis("Horizontal");
        float eixoZ = Input.GetAxis("Vertical");

        // --- Adição Mobile ---
        if (ControlesMobile.Instancia != null && ControlesMobile.Instancia.Movimento != Vector2.zero)
        {
            eixoX = ControlesMobile.Instancia.Movimento.x;
            eixoZ = ControlesMobile.Instancia.Movimento.y;
        }

        Vector3 direcao = transform.right * eixoX + transform.forward * eixoZ;

        // No PC: Shift para correr. No mobile: verifica o botão CORRER.
        float velocidadeAtual = Input.GetKey(KeyCode.LeftShift) ? velocidadeCorrida : velocidade;
        
        if (ControlesMobile.Instancia != null)
        {
            if (ControlesMobile.Instancia.CorridaPressionada)
                velocidadeAtual = velocidadeCorrida;
        }

        controller.Move(direcao * velocidadeAtual * Time.deltaTime);

        bool tentarPular = Input.GetButtonDown("Jump") || (ControlesMobile.Instancia != null && ControlesMobile.Instancia.PuloPressionadoDown);

        if (tentarPular && noChao)
            velocidadeVertical.y = Mathf.Sqrt(forcaPulo * -2f * gravidade);

        velocidadeVertical.y += gravidade * Time.deltaTime;
        controller.Move(velocidadeVertical * Time.deltaTime);
    }

    // -------------------------------------------------------
    // Olhar com o mouse
    // -------------------------------------------------------
    void OlharComMouse()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        // Se o controle mobile estiver ativo e houver toques/cliques (ou for build mobile)
        if (ControlesMobile.Instancia != null && (Input.touchCount > 0 || Input.GetMouseButton(0) || Application.isMobilePlatform))
        {
            mouseX = ControlesMobile.Instancia.CameraDelta.x * sensibilidadeX * 0.1f;
            mouseY = ControlesMobile.Instancia.CameraDelta.y * sensibilidadeY * 0.1f;
        }
        else
        {
            // Comportamento padrão de PC
            mouseX = Input.GetAxis("Mouse X") * sensibilidadeX;
            mouseY = Input.GetAxis("Mouse Y") * sensibilidadeY;
        }

        rotacaoX -= mouseY;
        rotacaoX = Mathf.Clamp(rotacaoX, -limiteVertical, limiteVertical);
        cameraPrimeiraPessoa.localRotation = Quaternion.Euler(rotacaoX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // -------------------------------------------------------
    // Mira — círculo com ponto central
    // -------------------------------------------------------
    void OnGUI()
    {
        if (texturaMira == null) return;

        float cx = Screen.width  / 2f;
        float cy = Screen.height / 2f;
        float size = tamanhoMira * 2f;

        GUI.color = corMira;
        GUI.DrawTexture(new Rect(cx - tamanhoMira, cy - tamanhoMira, size, size), texturaMira);
        GUI.color = Color.white;
    }

    // Gera a textura da mira (círculo + ponto) proceduralmente
    Texture2D GerarTexturaMira(int resolucao)
    {
        Texture2D tex = new Texture2D(resolucao, resolucao, TextureFormat.RGBA32, false);
        Color transparente = new Color(0, 0, 0, 0);

        // Preenche tudo com transparente
        for (int y = 0; y < resolucao; y++)
            for (int x = 0; x < resolucao; x++)
                tex.SetPixel(x, y, transparente);

        float centro = resolucao / 2f;
        float raioExterno = resolucao / 2f - 1f;
        float raioInterno = raioExterno - espessuraMira;
        float raioPonto  = tamanhoPonto;

        for (int y = 0; y < resolucao; y++)
        {
            for (int x = 0; x < resolucao; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));

                // Anel do círculo
                bool noAnel = dist <= raioExterno && dist >= raioInterno;
                // Ponto central
                bool noPonto = dist <= raioPonto;

                if (noAnel || noPonto)
                    tex.SetPixel(x, y, corMira);
            }
        }

        tex.Apply();
        return tex;
    }
}
