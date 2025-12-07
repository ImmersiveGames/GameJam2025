using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Configs
{
    [CreateAssetMenu(fileName = "SceneSetup", menuName = "ImmersiveGames/Scene Setup", order = 0)]
    public class SceneSetup : ScriptableObject
    {
        [Header("Identificação (opcional)")]
        [Tooltip("Identificador lógico deste setup (ex: 'MenuPrincipal', 'GameplayPadrao').")]
        [SerializeField] private string id;

        [Header("Cenas de destino")]
        [Tooltip("Lista de cenas que devem estar carregadas ao final da transição.")]
        [SerializeField] private string[] scenes;

        [Tooltip("Cena que será marcada como ativa ao final da transição. Se vazio, usa a primeira da lista.")]
        [SerializeField] private string activeScene;

        [Header("Comportamento padrão")]
        [Tooltip("Se verdadeiro, solicita uso de fade durante a transição.")]
        [SerializeField] private bool useFade = true;

        public string Id => id;

        /// <summary>
        /// Cenas que serão carregadas. O array é exposto como IReadOnlyList para evitar modificações externas.
        /// </summary>
        public IReadOnlyList<string> Scenes => scenes;

        /// <summary>
        /// Cena ativa ao final da transição. Se não definido, retorna a primeira cena válida da lista.
        /// </summary>
        public string ActiveScene
        {
            get
            {
                if (!string.IsNullOrEmpty(activeScene))
                    return activeScene;

                if (scenes != null)
                {
                    for (int i = 0; i < scenes.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(scenes[i]))
                            return scenes[i];
                    }
                }

                return string.Empty;
            }
        }

        public bool UseFade => useFade;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Se não houver cena ativa definida, e existir ao menos uma cena válida,
            // mantemos o campo de activeScene sincronizado para facilitar o debug no Inspector.
            if (string.IsNullOrEmpty(activeScene) && scenes != null)
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(scenes[i]))
                    {
                        activeScene = scenes[i];
                        break;
                    }
                }
            }
        }
#endif
    }
}
