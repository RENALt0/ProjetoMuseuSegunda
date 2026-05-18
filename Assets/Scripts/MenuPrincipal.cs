using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void BotaoIniciar()
    {
        SceneManager.LoadScene("JogoPrincipal");
    }
}
