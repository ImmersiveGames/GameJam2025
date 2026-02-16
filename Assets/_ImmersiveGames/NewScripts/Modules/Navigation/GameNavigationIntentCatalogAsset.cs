using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo canônico de ids de intents de navegação.
    /// Define slots explícitos para intents core do domínio.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationIntentCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/Navigation/Catalogs/GameNavigationIntentCatalogAsset",
        order = 31)]
    public sealed class GameNavigationIntentCatalogAsset : ScriptableObject
    {
        [Header("Core Intent Ids")]
        [SerializeField] private NavigationIntentId menu = NavigationIntentId.None;
        [SerializeField] private NavigationIntentId gameplay = NavigationIntentId.None;
        [SerializeField] private NavigationIntentId gameOver = NavigationIntentId.None;
        [SerializeField] private NavigationIntentId victory = NavigationIntentId.None;
        [SerializeField] private NavigationIntentId restart = NavigationIntentId.None;
        [SerializeField] private NavigationIntentId exitToMenu = NavigationIntentId.None;

        public NavigationIntentId Menu => menu;
        public NavigationIntentId Gameplay => gameplay;
        public NavigationIntentId GameOver => gameOver;
        public NavigationIntentId Victory => victory;
        public NavigationIntentId Restart => restart;
        public NavigationIntentId ExitToMenu => exitToMenu;

        /// <summary>
        /// Retorna intents críticos mínimos para validação de boot/fluxo principal.
        /// </summary>
        public IEnumerable<NavigationIntentId> EnumerateCriticalIntents()
        {
            yield return menu;
            yield return gameplay;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeIds();
            ValidateOrFail();
        }
#endif

        private void NormalizeIds()
        {
            menu = NavigationIntentId.FromName(menu.Value);
            gameplay = NavigationIntentId.FromName(gameplay.Value);
            gameOver = NavigationIntentId.FromName(gameOver.Value);
            victory = NavigationIntentId.FromName(victory.Value);
            restart = NavigationIntentId.FromName(restart.Value);
            exitToMenu = NavigationIntentId.FromName(exitToMenu.Value);
        }

        private void ValidateOrFail()
        {
            var seen = new HashSet<NavigationIntentId>();
            ValidateSingleOrFail(nameof(menu), menu, seen);
            ValidateSingleOrFail(nameof(gameplay), gameplay, seen);
            ValidateSingleOrFail(nameof(gameOver), gameOver, seen);
            ValidateSingleOrFail(nameof(victory), victory, seen);
            ValidateSingleOrFail(nameof(restart), restart, seen);
            ValidateSingleOrFail(nameof(exitToMenu), exitToMenu, seen);
        }

        private void ValidateSingleOrFail(string slotName, NavigationIntentId id, ISet<NavigationIntentId> seen)
        {
            if (!id.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: slot vazio. asset='{name}', slot='{slotName}'.");
            }

            if (!seen.Add(id))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: intent duplicado. asset='{name}', slot='{slotName}', intentId='{id}'.");
            }
        }

        private static void FailFastConfig(string message)
        {
            DebugUtility.LogError(typeof(GameNavigationIntentCatalogAsset), message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            throw new InvalidOperationException(message);
        }
    }
}
