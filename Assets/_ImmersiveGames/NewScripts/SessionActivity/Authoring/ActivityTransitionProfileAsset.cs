using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(fileName = "ActivityTransitionProfile", menuName = "ImmersiveGames/SessionActivity/Activity Transition Profile")]
    public sealed class ActivityTransitionProfileAsset : ScriptableObject
    {
        [SerializeField] private ActivityTransitionMode transitionMode = ActivityTransitionMode.None;
        [SerializeField] private SceneTransitionProfile fadeProfileOverride;
        [SerializeField] private RuntimeLoadingProfileAsset loadingProfileOverride;

        public ActivityTransitionMode TransitionMode => transitionMode;
        public SceneTransitionProfile FadeProfileOverride => fadeProfileOverride;
        public RuntimeLoadingProfileAsset LoadingProfileOverride => loadingProfileOverride;

        public void ValidateOrThrow(string owner)
        {
            if (transitionMode == ActivityTransitionMode.CutWithCurtain &&
                fadeProfileOverride != null &&
                !fadeProfileOverride.TryValidate(out string fadeError))
            {
                throw new InvalidOperationException($"ActivityTransitionProfileAsset '{name}' invalid fadeProfileOverride. owner='{owner}' detail='{fadeError}'.");
            }

            if (loadingProfileOverride != null && !loadingProfileOverride.TryValidate(out string loadingError))
            {
                throw new InvalidOperationException($"ActivityTransitionProfileAsset '{name}' invalid loadingProfileOverride. owner='{owner}' detail='{loadingError}'.");
            }
        }
    }
}
