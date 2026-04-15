#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    [AddComponentMenu("ImmersiveGames/NewScripts/IntroStage/IntroStage Presenter Content")]
    // Presenter scene-local da IntroStage. O contrato semantico vem da phase; este componente apenas executa a projeção visual.
    public sealed class IntroStagePresenter : MonoBehaviour, IIntroStagePresenter
    {
        [Header("Layout")]
        [SerializeField] private float margin = 16f;
        [SerializeField] private float panelWidth = 540f;
        [SerializeField] private float panelHeight = 340f;

        [Inject] private IIntroStageControlService? _controlService;

        private GUIStyle? _titleStyle;
        private GUIStyle? _bodyStyle;
        private GUIStyle? _buttonStyle;

        private IntroStagePresentationContract _contract;
        private bool _dependenciesInjected;
        private bool _isPresentationAttached;
        private string _presenterSignature = string.Empty;

        public string PresenterSignature => _presenterSignature;

        public bool IsPresentationAttached => _isPresentationAttached;

        public bool CanServe(string sessionSignature)
        {
            if (!_isPresentationAttached)
            {
                return false;
            }

            return string.Equals(_presenterSignature, Normalize(sessionSignature), StringComparison.Ordinal);
        }

        public void AttachPresentation(IntroStagePresentationContract contract)
        {
            if (contract.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStagePresenter),
                    "[FATAL][H1][IntroStage] IntroStage presenter received invalid presentation contract.");
            }

            EnsureDependenciesInjected();

            _contract = contract;
            _presenterSignature = contract.SessionSignature;
            _isPresentationAttached = true;

            DebugUtility.Log<IntroStagePresenter>(
                $"[OBS][IntroStage] IntroStagePresenterAttached presenter='{DescribePresenterLabel()}' scene='{gameObject.scene.name}' signature='{_presenterSignature}' introStage='{contract.HasIntroStage}' runResultStage='{contract.HasRunResultStage}' phase='{DescribePhaseName()}' source='scene_local'.",
                DebugUtility.Colors.Info);
        }

        public void DetachPresentation(string reason)
        {
            if (!_isPresentationAttached && string.IsNullOrWhiteSpace(_presenterSignature))
            {
                return;
            }

            DebugUtility.Log<IntroStagePresenter>(
                $"[OBS][IntroStage] IntroStagePresenterDetached presenter='{DescribePresenterLabel()}' reason='{Normalize(reason)}' signature='{_presenterSignature}'.",
                DebugUtility.Colors.Info);

            _contract = default;
            _presenterSignature = string.Empty;
            _isPresentationAttached = false;
        }

        private void OnEnable()
        {
            EnsureDependenciesInjected();

            DebugUtility.LogVerbose<IntroStagePresenter>(
                $"[OBS][IntroStage] IntroStagePresenterEnabled presenter='{DescribePresenterLabel()}' scene='{gameObject.scene.name}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDisable()
        {
            _isPresentationAttached = false;
            _presenterSignature = string.Empty;
            _contract = default;
        }

        private void OnGUI()
        {
            if (!_isPresentationAttached || string.IsNullOrWhiteSpace(_presenterSignature) || _contract.PhaseDefinitionRef == null)
            {
                return;
            }

            Rect panelRect = BuildPanelRect();
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.BeginVertical();
            EnsureStyles();
            GUILayout.Label("IntroStage", _titleStyle!);
            GUILayout.Label($"phase: {DescribePhaseName()}", _bodyStyle!);
            GUILayout.Label($"signature: {_presenterSignature}", _bodyStyle!);
            GUILayout.Label($"intro: {_contract.HasIntroStage}", _bodyStyle!);
            GUILayout.Label($"runResult: {_contract.HasRunResultStage}", _bodyStyle!);
            GUILayout.Space(10f);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Continue", _buttonStyle!, GUILayout.Height(48f)))
            {
                RequestCompletion("IntroStage/ContinueButton");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void RequestCompletion(string reason)
        {
            if (_controlService == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStagePresenter),
                    "[FATAL][H1][IntroStage] IIntroStageControlService indisponivel no presenter local.");
            }

            DebugUtility.Log<IntroStagePresenter>(
                $"[OBS][IntroStage] IntroStagePresenterActionRequested presenter='{DescribePresenterLabel()}' kind='Continue' reason='{Normalize(reason)}' signature='{_presenterSignature}'.",
                DebugUtility.Colors.Info);

            _controlService?.CompleteIntroStage(reason);
        }

        private string DescribePhaseName()
        {
            if (_contract.PhaseDefinitionRef != null)
            {
                return _contract.PhaseDefinitionRef.name;
            }

            return "<none>";
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected || !DependencyManager.HasInstance)
            {
                return;
            }

            try
            {
                DependencyManager.Provider.InjectDependencies(this);
                _dependenciesInjected = true;
            }
            catch
            {
                _dependenciesInjected = false;
            }
        }

        private Rect BuildPanelRect()
        {
            float maxWidth = Mathf.Max(panelWidth, Screen.width - 40f);
            float width = Mathf.Clamp(panelWidth, panelWidth, maxWidth);
            float maxHeight = Mathf.Max(panelHeight, Screen.height - 40f);
            float height = Mathf.Clamp(panelHeight, panelHeight, maxHeight);
            float x = Mathf.Round((Screen.width - width) * 0.5f);
            float y = Mathf.Max(margin, Mathf.Round(Screen.height - height - margin));
            return new Rect(x, y, width, height);
        }

        private static string DescribePresenterLabel()
            => nameof(IntroStagePresenter);

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 40,
                    wordWrap = true
                };
            }

            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30,
                    wordWrap = true
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 30,
                    wordWrap = true
                };
            }
        }

        private static string Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
