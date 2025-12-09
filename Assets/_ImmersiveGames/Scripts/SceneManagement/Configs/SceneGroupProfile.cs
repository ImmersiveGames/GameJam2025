using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Configs
{
    /// <summary>
    /// Define um "grupo lógico" de cenas que representam um estado do jogo
    /// (ex.: Menu Principal, Gameplay padrão, BossFight_01, etc.).
    /// 
    /// Este ScriptableObject é a base da Fase 1 de modularização do sistema de cenas:
    /// - Torna explícito quais cenas compõem um "estado" do jogo.
    /// - Permite configurar a cena ativa alvo.
    /// - Permite associar um perfil de transição (fade, HUD, textos).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneGroupProfile",
        menuName = "ImmersiveGames/Scene Flow/Scene Group Profile",
        order = 0)]
    public class SceneGroupProfile : ScriptableObject
    {
        [Header("Identidade Lógica")]
        [Tooltip("Identificador lógico único (ex.: 'MenuPrincipal', 'Gameplay_Default', 'Boss_01').")]
        [SerializeField] private string id;

        [Tooltip("Nome amigável para debug/editor (opcional).")]
        [SerializeField] private string displayName;

        [Header("Cenas Alvo")]
        [Tooltip("Lista de cenas que devem estar carregadas ao final da transição.")]
        [SerializeField] private List<string> sceneNames = new List<string>();

        [Tooltip("Nome da cena que deve se tornar a 'ActiveScene' ao final da transição. " +
                 "Se vazio, o planner irá escolher automaticamente (primeira do grupo ou manter a atual).")]
        [SerializeField] private string activeSceneName;

        [Header("Transição")]
        [Tooltip("Perfil de transição (fade, HUD, textos). Se null, será usado o perfil padrão global.")]
        [SerializeField] private SceneTransitionProfile transitionProfile;

        [Tooltip("Se verdadeiro, força o uso de fade mesmo que o perfil esteja null.")]
        [SerializeField] private bool forceUseFade = true;

        /// <summary>
        /// Identificador lógico único deste grupo.
        /// Idealmente usado como chave em dicionários, enums ou lookups.
        /// </summary>
        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;

        /// <summary>
        /// Nome amigável para UI/editor.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;

        /// <summary>
        /// Lista somente leitura de nomes de cenas pertencentes a este grupo.
        /// </summary>
        public IReadOnlyList<string> SceneNames => sceneNames;

        /// <summary>
        /// Cena que deve se tornar ativa ao final da transição.
        /// Pode ser vazia; o planner decide o fallback.
        /// </summary>
        public string ActiveSceneName => activeSceneName;

        /// <summary>
        /// Perfil de transição associado a este grupo.
        /// Pode ser null; o planner/serviço decidirá o perfil default.
        /// </summary>
        public SceneTransitionProfile TransitionProfile => transitionProfile;

        /// <summary>
        /// Indica se este grupo deve usar fade por padrão.
        /// Se houver um SceneTransitionProfile, o campo UseFade dele pode sobrepor este valor.
        /// </summary>
        public bool ForceUseFade => forceUseFade;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Garante que a lista não é null.
            sceneNames ??= new List<string>();

            // Remove entradas nulas ou em branco para evitar lixo no inspector.
            for (int i = sceneNames.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(sceneNames[i]))
                    sceneNames.RemoveAt(i);
            }

            // Warning se não houver nenhuma cena configurada.
            if (sceneNames.Count == 0)
            {
                Debug.LogWarning(
                    $"[SceneGroupProfile] Grupo '{name}' não possui nenhuma cena configurada (sceneNames vazio).");
            }

            // Warning se a cena ativa alvo não estiver na lista de cenas do grupo.
            if (!string.IsNullOrWhiteSpace(activeSceneName) &&
                !sceneNames.Contains(activeSceneName))
            {
                Debug.LogWarning(
                    $"[SceneGroupProfile] Grupo '{name}' possui ActiveSceneName='{activeSceneName}' " +
                    "que não está presente em sceneNames. Verifique o nome da cena.");
            }
        }
#endif
    }
}
