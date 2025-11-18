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
            UpdateCompass();
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

        /// <summary>
        /// Atualiza a bússola calculando ângulo, posição e distância para cada alvo rastreável.
        /// </summary>
        private void UpdateCompass()
        {
            if (compassRectTransform == null || iconPrefab == null)
            {
                return;
            }

            Transform playerTransform = CompassRuntimeService.PlayerTransform;
            IReadOnlyList<ICompassTrackable> trackables = CompassRuntimeService.Trackables;

            SynchronizeIcons(trackables);

            if (playerTransform == null || _iconsByTarget.Count == 0)
            {
                return;
            }

            Vector3 playerForward = playerTransform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.0001f)
            {
                playerForward = Vector3.forward;
            }

            float halfAngle = settings != null ? Mathf.Abs(settings.compassHalfAngleDegrees) : 180f;
            float width = compassRectTransform.rect.width;
            float halfWidth = width * 0.5f;
            bool clampIcons = settings != null && settings.clampIconsAtEdges;

            List<ICompassTrackable> toRemove = null;

            foreach (KeyValuePair<ICompassTrackable, CompassIcon> pair in _iconsByTarget.ToList())
            {
                ICompassTrackable target = pair.Key;
                CompassIcon icon = pair.Value;

                if (target == null || icon == null || target.Transform == null)
                {
                    toRemove ??= new List<ICompassTrackable>();
                    toRemove.Add(target);
                    continue;
                }

                Vector3 toTarget = target.Transform.position - playerTransform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                if (toTarget.sqrMagnitude < 0.0001f)
                {
                    icon.gameObject.SetActive(true);
                    SetIconPosition(icon, 0f, halfAngle, halfWidth);
                    icon.UpdateDistance(distance);
                    continue;
                }

                float angle = Vector3.SignedAngle(playerForward, toTarget, Vector3.up);

                if (halfAngle <= 0f)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                if (Mathf.Abs(angle) > halfAngle)
                {
                    if (!clampIcons)
                    {
                        icon.gameObject.SetActive(false);
                        continue;
                    }

                    angle = Mathf.Clamp(angle, -halfAngle, halfAngle);
                }

                icon.gameObject.SetActive(true);
                SetIconPosition(icon, angle, halfAngle, halfWidth);
                icon.UpdateDistance(distance);
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

        private static void SetIconPosition(CompassIcon icon, float angle, float halfAngle, float halfWidth)
        {
            if (icon.rectTransform == null)
            {
                return;
            }

            float normalized = Mathf.Approximately(halfAngle, 0f) ? 0f : angle / halfAngle;
            float x = normalized * halfWidth;
            Vector2 anchoredPos = icon.rectTransform.anchoredPosition;
            anchoredPos.x = x;
            icon.rectTransform.anchoredPosition = anchoredPos;
        }
    }
}
