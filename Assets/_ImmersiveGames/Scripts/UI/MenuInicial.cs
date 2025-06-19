using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.UI
{
    public class MenuInicial : MonoBehaviour
    {
        public void PlayGame()
        {
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        
    }
}