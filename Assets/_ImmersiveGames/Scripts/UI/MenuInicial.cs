using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.UI
{
    public class MenuInicial : MonoBehaviour
    {

        [SerializeField] private string startGameScene = "Game";
        public void PlayGame()
        {
            var loadOperation = SceneManager.LoadSceneAsync(startGameScene);
        }
        
    }
}