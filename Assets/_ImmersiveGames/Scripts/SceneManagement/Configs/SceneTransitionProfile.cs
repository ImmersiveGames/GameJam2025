﻿using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Configs
{
    /// <summary>
    /// Perfil de transição de cena.
    /// Centraliza valores de:
    /// - uso de fade;
    /// - tempos de fade;
    /// - curvas de easing do fade;
    /// - tempo mínimo de HUD;
    /// - textos padrão para HUD.
    /// 
    /// Integra-se com:
    /// - FadeController (durations/curves);
    /// - SceneTransitionService (tempo mínimo de HUD);
    /// - SceneLoadingHudController (textos).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneTransitionProfile",
        menuName = "ImmersiveGames/Scene Flow/Scene Transition Profile",
        order = 1)]
    public class SceneTransitionProfile : ScriptableObject
    {
        [Header("Fade Global")]
        [Tooltip("Se falso, transições usando este perfil não utilizarão fade.")]
        [SerializeField] private bool useFade = true;

        [Header("Fade Durations (Tempo não escalonado)")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Fade Curves")]
        [Tooltip("Curva de easing usada no FadeIn (0->1). Se nula ou vazia, será usado lerp linear.")]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Curva de easing usada no FadeOut (1->0). Se nula ou vazia, será usado lerp linear.")]
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("HUD de Loading")]
        [Tooltip("Tempo mínimo (em segundos, tempo não escalonado) que o HUD deve permanecer visível.")]
        [SerializeField] private float minHudVisibleSeconds = 0.5f;

        [Tooltip("Título padrão do HUD de loading.")]
        [SerializeField] private string loadingTitle = "Carregando";

        [Tooltip("Template da descrição ao iniciar. Use {Scenes} para listar cenas.")]
        [SerializeField] private string loadingDescriptionTemplate = "Carregando: {Scenes}";

        [Tooltip("Título exibido ao finalizar o carregamento.")]
        [SerializeField] private string finishingTitle = "";

        [Tooltip("Descrição ao marcar cenas como prontas.")]
        [SerializeField] private string finishingDescription = "Finalizando carregamento...";

        /// <summary>
        /// Indica se esta transição deve usar fade.
        /// </summary>
        public bool UseFade => useFade;

        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;

        public AnimationCurve FadeInCurve => fadeInCurve;
        public AnimationCurve FadeOutCurve => fadeOutCurve;

        public float MinHudVisibleSeconds => minHudVisibleSeconds;

        public string LoadingTitle => loadingTitle;
        public string LoadingDescriptionTemplate => loadingDescriptionTemplate;
        public string FinishingTitle => finishingTitle;
        public string FinishingDescription => finishingDescription;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fadeInDuration < 0f) fadeInDuration = 0f;
            if (fadeOutDuration < 0f) fadeOutDuration = 0f;
            if (minHudVisibleSeconds < 0f) minHudVisibleSeconds = 0f;
        }
#endif
    }
}
