using UnityEngine;

public class MenuSair : MonoBehaviour
{
    // Chame esse método no OnClick do botão "Sair"
    public void Sair()
    {
        Debug.Log("Saindo do Jogo");
        Application.Quit();
    }
}