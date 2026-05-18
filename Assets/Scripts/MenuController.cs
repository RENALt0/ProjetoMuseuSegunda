using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject painelCreditos;

    // Chame no OnClick do botão "Créditos"
    public void AbrirCreditos()
    {
        painelCreditos.SetActive(true);
    }

    // Chame no OnClick do botão "Fechar" dentro do painel
    public void FecharCreditos()
    {
        painelCreditos.SetActive(false);
    }
}
