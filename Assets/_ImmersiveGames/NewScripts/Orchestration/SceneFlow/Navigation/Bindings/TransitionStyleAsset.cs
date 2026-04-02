using System;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Bindings;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Asset canonico de estilo de transicao.
    /// Owner estrutural: profileRef + useFade.
    /// Nomes expostos sao apenas descritivos/observabilidade.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TransitionStyleAsset",
        menuName = "ImmersiveGames/NewScripts/Orchestration/SceneFlow/Navigation/Bindings/TransitionStyleAsset",
        order = 29)]
    public sealed class TransitionStyleAsset : ScriptableObject
    {
        [Tooltip("Referencia direta obrigatoria ao SceneTransitionProfile usado em runtime.")]
        [SerializeField] private SceneTransitionProfile profileRef;

        [Tooltip("Quando true, aplica fade (se o SceneFlow suportar).")]
        [SerializeField] private bool useFade = true;

        public SceneTransitionProfile Profile => profileRef;
        public bool UseFade => useFade;
        public string StyleLabel => string.IsNullOrWhiteSpace(name) ? "<unnamed-style>" : name.Trim();
        public string ProfileLabel => profileRef != null && !string.IsNullOrWhiteSpace(profileRef.name) ? profileRef.name.Trim() : string.Empty;

        public TransitionStyleDefinition ToDefinitionOrFail(string owner, string context)
        {
            if (profileRef == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config] TransitionStyleAsset sem profileRef obrigatorio. owner='{owner}', context='{context}', asset='{name}', style='{StyleLabel}'.");
            }

            return new TransitionStyleDefinition(profileRef, useFade, StyleLabel, ProfileLabel);
        }
    }
}
