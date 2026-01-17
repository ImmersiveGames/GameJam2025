using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Controla painéis locais do Frontend dentro do MenuScene (sem SceneFlow / sem GameLoop).
    ///
    /// Uso:
    /// - Configure uma lista de "Panels": cada item tem um PanelId e um Root (GameObject).
    /// - Chame Show(panelId) para alternar qual root fica ativo (um por vez).
    /// - Opcional: define "selectOnShow" por painel (para navegação por teclado/controle).
    ///
    /// Regras:
    /// - Não registra listeners de UI; é acionado por binders via Inspector.
    /// - Não faz transição de cena; apenas SetActive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrontendPanelsController : MonoBehaviour
    {
        [Serializable]
        public sealed class PanelEntry
        {
            [Tooltip("Id lógico do painel (ex.: 'main', 'options', 'howto'). Case-insensitive.")]
            public string panelId;

            [Tooltip("Root GameObject do painel (vai ser SetActive true/false).")]
            public GameObject root;

            [Tooltip("Opcional: GameObject para selecionar quando o painel for exibido (EventSystem).")]
            public GameObject selectOnShow;
        }

        [Header("Panels")]
        [SerializeField] private List<PanelEntry> panels = new List<PanelEntry>();

        [Header("Defaults")]
        [Tooltip("Painel inicial (id). Se vazio, tenta detectar pelo root ativo; senão usa o primeiro da lista.")]
        [SerializeField] private string initialPanelId = "main";

        [Header("Behavior")]
        [Tooltip("Se true, no Awake força mostrar o painel initialPanelId.")]
        [SerializeField] private bool forceInitialOnAwake = true;

        [Tooltip("Se true, ao trocar painel tenta setar seleção no EventSystem (se existir).")]
        [SerializeField] private bool setEventSystemSelectionOnShow = true;

        public string CurrentPanelId { get; private set; }

        private readonly Dictionary<string, PanelEntry> _map =
            new Dictionary<string, PanelEntry>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            RebuildMap();

            if (forceInitialOnAwake)
            {
                string id = string.IsNullOrWhiteSpace(initialPanelId) ? DetectActiveOrFallback() : initialPanelId;
                Show(id, "Awake/Initial");
                return;
            }

            CurrentPanelId = DetectActiveOrFallback();
            ApplyState(CurrentPanelId, "Awake/Detect");
        }

        /// <summary>
        /// Exibe o painel por id (case-insensitive). Desativa todos os outros.
        /// </summary>
        public void Show(string panelId, string reason = "UI/ShowPanel")
        {
            if (string.IsNullOrWhiteSpace(panelId))
            {
                DebugUtility.LogWarning<FrontendPanelsController>(
                    "[FrontendPanels] Show ignorado: panelId vazio.");
                return;
            }

            if (_map.Count == 0)
            {
                RebuildMap();
            }

            if (!_map.TryGetValue(panelId, out var target) || target.root == null)
            {
                DebugUtility.LogWarning<FrontendPanelsController>(
                    $"[FrontendPanels] PanelId '{panelId}' não encontrado ou root nulo. reason='{reason}'.",
                    this);
                return;
            }

            ApplyState(panelId, reason);
        }

        /// <summary>
        /// Rebuild do mapa interno. Útil se você alterar a lista em runtime (raro).
        /// </summary>
        public void RebuildMap()
        {
            _map.Clear();

            for (int i = 0; i < panels.Count; i++)
            {
                var p = panels[i];
                if (p == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(p.panelId))
                {
                    DebugUtility.LogWarning<FrontendPanelsController>(
                        $"[FrontendPanels] PanelEntry #{i} com panelId vazio. Ignorado.");
                    continue;
                }

                if (_map.ContainsKey(p.panelId))
                {
                    DebugUtility.LogWarning<FrontendPanelsController>(
                        $"[FrontendPanels] PanelId duplicado '{p.panelId}'. O último vence.");
                }

                _map[p.panelId] = p;
            }
        }

        private void ApplyState(string panelId, string reason)
        {
            // Importante: iterar pela LISTA (fonte de verdade) garante que:
            // - roots duplicados (panelId repetido) também sejam desligados/ligados corretamente
            // - entries fora do _map (por erro de dados) não fiquem "sobrando" ativos
            foreach (var entry in panels)
            {
                if (entry?.root == null)
                {
                    continue;
                }

                bool isTarget = string.Equals(entry.panelId, panelId, StringComparison.OrdinalIgnoreCase);
                entry.root.SetActive(isTarget);
            }

            CurrentPanelId = panelId;

            DebugUtility.LogVerbose<FrontendPanelsController>(
                $"[FrontendPanels] Panel='{panelId}' (reason='{reason}').",
                DebugUtility.Colors.Info);

            if (setEventSystemSelectionOnShow)
            {
                TrySetSelection(panelId);
            }
        }

        private void TrySetSelection(string panelId)
        {
            var es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            // Escolha determinística: primeira entrada da LISTA que bater com o panelId e tiver selectOnShow.
            foreach (var target in from entry in panels where entry != null where string.Equals(entry.panelId, panelId, StringComparison.OrdinalIgnoreCase) select entry.selectOnShow into target where target != null select target)
            {
                es.SetSelectedGameObject(target);

                DebugUtility.LogVerbose<FrontendPanelsController>(
                    $"[FrontendPanels] EventSystem selected='{target.name}' (panel='{panelId}').",
                    DebugUtility.Colors.Info);

                return;
            }
        }

        private string DetectActiveOrFallback()
        {
            // 1) se initialPanelId existe no mapa, usa ele
            if (!string.IsNullOrWhiteSpace(initialPanelId) && _map.ContainsKey(initialPanelId))
            {
                return initialPanelId;
            }

            // 2) detecta o primeiro painel (pela LISTA) cujo root esteja ativo
            foreach (var entry in panels.Where(entry => entry?.root != null && entry.root.activeSelf && !string.IsNullOrWhiteSpace(entry.panelId)))
            {
                return entry.panelId;
            }

            // 3) fallback: primeiro item válido da lista
            foreach (var entry in panels.Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.panelId)))
            {
                return entry.panelId;
            }

            return "main";
        }
    }
}
