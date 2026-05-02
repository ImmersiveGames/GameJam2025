using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation
{
    /// <summary>
    /// OWNER: contrato Base 1.1 de policy/capability da rota.
    /// NAO E OWNER: execucao operacional de SceneFlow ou pipeline.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteProfileAsset",
        menuName = "ImmersiveGames/NewScripts/Orchestration/SceneFlow/Navigation/Profiles/SceneRouteProfileAsset",
        order = 31)]
    public sealed class SceneRouteProfileAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private SceneRouteProfileId profileId;

        [Header("Policy")]
        [SerializeField] private SceneRouteProfileClass routeClass = SceneRouteProfileClass.Unspecified;
        [SerializeField] private SceneRouteProfileGameplayParticipation gameplayParticipation;
        [SerializeField] private SceneRouteProfileWorldResetRequirement worldResetRequirement;
        [SerializeField] private SceneTransitionGameplayEntryKind gameplayEntryKind;
        [SerializeField] private SceneRouteProfileInputModeBehavior inputModeBehavior;
        [SerializeField] private SceneRouteProfileActorSetBehavior actorSetBehavior;
        [SerializeField] private SceneRouteProfileSaveBehavior saveBehavior;
        [SerializeField] private SceneRouteProfileLoadingBehavior loadingBehavior;

        public SceneRouteProfileId ProfileId => profileId;
        public SceneRouteProfileClass RouteClass => routeClass;
        public SceneRouteProfileGameplayParticipation GameplayParticipation => gameplayParticipation;
        public SceneRouteProfileWorldResetRequirement WorldResetRequirement => worldResetRequirement;
        public SceneTransitionGameplayEntryKind GameplayEntryKind => gameplayEntryKind;
        public SceneRouteProfileInputModeBehavior InputModeBehavior => inputModeBehavior;
        public SceneRouteProfileActorSetBehavior ActorSetBehavior => actorSetBehavior;
        public SceneRouteProfileSaveBehavior SaveBehavior => saveBehavior;
        public SceneRouteProfileLoadingBehavior LoadingBehavior => loadingBehavior;

        public SceneRouteProfile ToProfile()
        {
            EnsureValidProfile();
            return new SceneRouteProfile(
                profileId,
                routeClass,
                gameplayParticipation,
                worldResetRequirement,
                gameplayEntryKind,
                inputModeBehavior,
                actorSetBehavior,
                saveBehavior,
                loadingBehavior);
        }

        public void ValidateProfileOrFailFast()
        {
            EnsureValidProfile();
        }

        public string GetValidationError()
        {
            return GetProfileValidationError();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            string validationError = GetProfileValidationError();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                DebugUtility.LogWarning(typeof(SceneRouteProfileAsset),
                    $"[Config][Editor] profileId='{profileId}' invalido. detail='{validationError}'");
            }
        }
#endif

        private void EnsureValidProfile()
        {
            string validationError = GetProfileValidationError();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                FailFast(validationError);
            }
        }

        private string GetProfileValidationError()
        {
            if (!profileId.IsValid)
            {
                return "profileId vazio ou invalido.";
            }

            if (routeClass == SceneRouteProfileClass.Unspecified)
            {
                return $"profileId='{profileId}' com RouteClass='{SceneRouteProfileClass.Unspecified}' e invalido.";
            }

            if (routeClass == SceneRouteProfileClass.Gameplay)
            {
                if (gameplayParticipation != SceneRouteProfileGameplayParticipation.Gameplay)
                {
                    return $"profileId='{profileId}' Gameplay exige gameplayParticipation=Gameplay.";
                }

                if (worldResetRequirement != SceneRouteProfileWorldResetRequirement.RequiresWorldReset)
                {
                    return $"profileId='{profileId}' Gameplay exige worldResetRequirement=RequiresWorldReset.";
                }

                if (gameplayEntryKind == SceneTransitionGameplayEntryKind.None)
                {
                    return $"profileId='{profileId}' Gameplay exige gameplayEntryKind explicito.";
                }

                if (inputModeBehavior != SceneRouteProfileInputModeBehavior.GameplayDeferred)
                {
                    return $"profileId='{profileId}' Gameplay exige inputModeBehavior=GameplayDeferred.";
                }

                if (actorSetBehavior != SceneRouteProfileActorSetBehavior.GameplayRouteActorSet)
                {
                    return $"profileId='{profileId}' Gameplay exige actorSetBehavior=GameplayRouteActorSet.";
                }

                if (saveBehavior != SceneRouteProfileSaveBehavior.GameplayTransitionEligible)
                {
                    return $"profileId='{profileId}' Gameplay exige saveBehavior=GameplayTransitionEligible.";
                }

                if (loadingBehavior != SceneRouteProfileLoadingBehavior.Gameplay)
                {
                    return $"profileId='{profileId}' Gameplay exige loadingBehavior=Gameplay.";
                }
            }
            else if (routeClass == SceneRouteProfileClass.Frontend)
            {
                if (gameplayParticipation != SceneRouteProfileGameplayParticipation.NonGameplay)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayParticipation=NonGameplay.";
                }

                if (worldResetRequirement != SceneRouteProfileWorldResetRequirement.NoReset)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige worldResetRequirement=NoReset.";
                }

                if (gameplayEntryKind != SceneTransitionGameplayEntryKind.None)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayEntryKind=None.";
                }

                if (inputModeBehavior != SceneRouteProfileInputModeBehavior.FrontendMenu)
                {
                    return $"profileId='{profileId}' Frontend exige inputModeBehavior=FrontendMenu.";
                }

                if (actorSetBehavior != SceneRouteProfileActorSetBehavior.Clear)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige actorSetBehavior=Clear.";
                }

                if (saveBehavior != SceneRouteProfileSaveBehavior.NoSave)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige saveBehavior=NoSave.";
                }

                if (loadingBehavior != SceneRouteProfileLoadingBehavior.Frontend)
                {
                    return $"profileId='{profileId}' Frontend exige loadingBehavior=Frontend.";
                }
            }
            else if (routeClass == SceneRouteProfileClass.Overlay)
            {
                if (gameplayParticipation != SceneRouteProfileGameplayParticipation.NonGameplay)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayParticipation=NonGameplay.";
                }

                if (worldResetRequirement != SceneRouteProfileWorldResetRequirement.NoReset)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige worldResetRequirement=NoReset.";
                }

                if (gameplayEntryKind != SceneTransitionGameplayEntryKind.None)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayEntryKind=None.";
                }

                if (inputModeBehavior == SceneRouteProfileInputModeBehavior.FrontendMenu)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' nao pode usar inputModeBehavior=FrontendMenu.";
                }

                if (actorSetBehavior != SceneRouteProfileActorSetBehavior.Clear)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige actorSetBehavior=Clear.";
                }

                if (saveBehavior != SceneRouteProfileSaveBehavior.NoSave)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige saveBehavior=NoSave.";
                }

                if (loadingBehavior == SceneRouteProfileLoadingBehavior.Frontend)
                {
                    return $"profileId='{profileId}' Overlay nao pode usar loadingBehavior=Frontend.";
                }
            }
            else if (routeClass == SceneRouteProfileClass.Sandbox)
            {
                if (gameplayParticipation != SceneRouteProfileGameplayParticipation.NonGameplay)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayParticipation=NonGameplay.";
                }

                if (worldResetRequirement != SceneRouteProfileWorldResetRequirement.NoReset)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige worldResetRequirement=NoReset.";
                }

                if (gameplayEntryKind != SceneTransitionGameplayEntryKind.None)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige gameplayEntryKind=None.";
                }

                if (inputModeBehavior == SceneRouteProfileInputModeBehavior.FrontendMenu)
                {
                    return $"profileId='{profileId}' Sandbox nao pode usar inputModeBehavior=FrontendMenu.";
                }

                if (actorSetBehavior != SceneRouteProfileActorSetBehavior.Clear)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige actorSetBehavior=Clear.";
                }

                if (saveBehavior != SceneRouteProfileSaveBehavior.NoSave)
                {
                    return $"profileId='{profileId}' RouteClass='{routeClass}' exige saveBehavior=NoSave.";
                }

                if (loadingBehavior != SceneRouteProfileLoadingBehavior.Sandbox)
                {
                    return $"profileId='{profileId}' Sandbox exige loadingBehavior=Sandbox.";
                }
            }
            else
            {
                return $"profileId='{profileId}' RouteClass='{routeClass}' e invalido.";
            }

            return string.Empty;
        }

        private static void FailFast(string message)
        {
            HardFailFastH1.Trigger(typeof(SceneRouteProfileAsset), $"[Config] {message}");
        }
    }
}
