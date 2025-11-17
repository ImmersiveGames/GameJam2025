using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// HUD da bússola responsável por instanciar e manter ícones para alvos rastreáveis.
    /// </summary>
    public class CompassHUD : MonoBehaviour
    {
        [Header("Compass UI")]
        [Tooltip("Área da UI onde os ícones da bússola serão posicionados.")]
        public RectTransform compassRectTransform;

        [Tooltip("Configurações gerais da bússola.")]
        public CompassSettings settings;

        [Tooltip("Banco de dados com as configurações visuais por tipo de alvo.")]
        public CompassVisualDatabase visualDatabase;

        [Tooltip("Prefab do ícone utilizado para cada alvo rastreável.")]
        public CompassIcon iconPrefab;

        private Dictionary<ICompassTrackable, CompassIcon> _iconsByTarget;

        private void Awake()
        {
            _iconsByTarget = new Dictionary<ICompassTrackable, CompassIcon>();
        }

        private void Update()
        {
            if (CompassRuntimeService.PlayerTransform == null || compassRectTransform == null || iconPrefab == null)
            {
                return;
            }

            IReadOnlyList<ICompassTrackable> trackables = CompassRuntimeService.Trackables;
            SynchronizeIcons(trackables);

            // TODO: Atualizar posição e rotação dos ícones com base no ângulo relativo ao jogador.
        }

        private void SynchronizeIcons(IReadOnlyList<ICompassTrackable> trackables)
        {
            if (trackables == null)
            {
                return;
            }

            for (int i = 0; i < trackables.Count; i++)
            {
                ICompassTrackable target = trackables[i];
                if (target == null || !target.IsActive)
                {
                    continue;
                }

                if (_iconsByTarget.ContainsKey(target))
                {
                    continue;
                }

                CompassTargetVisualConfig visualConfig = visualDatabase != null
                    ? visualDatabase.GetConfig(target.TargetType)
                    : null;

                CompassIcon iconInstance = Instantiate(iconPrefab, compassRectTransform);
                iconInstance.Initialize(target, visualConfig);

                // Por enquanto, mantemos os ícones centralizados até que a lógica de posição seja implementada.
                if (iconInstance.rectTransform != null)
                {
                    iconInstance.rectTransform.anchoredPosition = Vector2.zero;
                }

                _iconsByTarget[target] = iconInstance;
            }

            if (_iconsByTarget.Count == 0)
            {
                return;
            }

            List<ICompassTrackable> toRemove = null;
            foreach (KeyValuePair<ICompassTrackable, CompassIcon> pair in _iconsByTarget)
            {
                ICompassTrackable target = pair.Key;
                if (target == null || !target.IsActive || !trackables.Contains(target))
                {
                    toRemove ??= new List<ICompassTrackable>();
                    toRemove.Add(target);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                ICompassTrackable target = toRemove[i];
                if (_iconsByTarget.TryGetValue(target, out CompassIcon icon))
                {
                    if (icon != null)
                    {
                        Destroy(icon.gameObject);
                    }

                    _iconsByTarget.Remove(target);
                }
            }
        }
    }
}
