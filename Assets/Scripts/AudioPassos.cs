using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPassos : MonoBehaviour
{
    [Header("Áudio de Passos")]
    [Tooltip("Arraste aqui o arquivo de áudio da pasta Audios")]
    public AudioClip clipPassos;

    [Tooltip("Volume dos passos (0 = mudo, 1 = máximo)")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Velocidade mínima do personagem para tocar o som (evita tocar parado)")]
    public float velocidadeMinima = 0.1f;

    [Header("Pitch (Velocidade do Áudio)")]
    [Tooltip("Velocidade do personagem andando (deve bater com 'velocidade' do FuncoesPlayer)")]
    public float velocidadeAndar = 5f;

    [Tooltip("Velocidade do personagem correndo (deve bater com 'velocidadeCorrida' do FuncoesPlayer)")]
    public float velocidadeCorrida = 10f;

    [Tooltip("Pitch do áudio ao andar (1 = normal)")]
    [Range(0.5f, 2f)]
    public float pitchAndar = 1f;

    [Tooltip("Pitch do áudio ao correr (maior = mais rápido)")]
    [Range(0.5f, 2f)]
    public float pitchCorrida = 1.5f;

    [Header("Debug")]
    [Tooltip("Ativa logs no Console para diagnóstico")]
    public bool debugAtivado = false;

    // --- Privados ---
    private AudioSource audioSource;
    private CharacterController controller;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        controller  = GetComponent<CharacterController>();

        if (clipPassos == null)
            Debug.LogError("[AudioPassos] Nenhum clip de áudio atribuído! Arraste o Passos.mp3 no campo 'Clip Passos'.", this);

        if (controller == null)
            Debug.LogError("[AudioPassos] CharacterController não encontrado neste GameObject!", this);

        audioSource.clip        = clipPassos;
        audioSource.loop        = true;
        audioSource.volume      = volume;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (controller == null || clipPassos == null) return;

        bool noChao = controller.isGrounded;

        Vector3 velHorizontal = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float speed = velHorizontal.magnitude;

        // Verifica input direto apenas quando a velocidade física não for suficiente
        // (evita chamar GetAxis desnecessário quando já sabemos que está movendo)
        bool movendo = speed > velocidadeMinima;
        if (!movendo)
        {
            float inputH = Input.GetAxis("Horizontal");
            float inputV = Input.GetAxis("Vertical");
            movendo = Mathf.Abs(inputH) > 0.01f || Mathf.Abs(inputV) > 0.01f;
        }

        if (noChao && movendo)
        {
            // Interpola o pitch entre andar e correr com base na velocidade atual
            float t = Mathf.InverseLerp(velocidadeAndar, velocidadeCorrida, speed);
            audioSource.pitch = Mathf.Lerp(pitchAndar, pitchCorrida, t);

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            audioSource.pitch = pitchAndar; // reseta pitch ao parar
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        if (debugAtivado)
            Debug.Log($"[AudioPassos] speed={speed:F2} | pitch={audioSource.pitch:F2} | tocando={audioSource.isPlaying}");
    }
}


