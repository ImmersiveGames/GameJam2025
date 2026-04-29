using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionFlow.Host.PostRun.Presentation
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    [AddComponentMenu("ImmersiveGames/NewScripts/RunResultStage/RunResultStage Presenter Content")]
    // Presenter local passivo do RunResultStage. A surface visual e configurada manualmente na phase.
    public sealed class RunResultStageMockPresenter : MonoBehaviour, IRunResultStagePresenter
    {
        [Header("Layout")]
        [SerializeField] private float margin = 20f;
        [SerializeField] private float panelWidth = 720f;
        [SerializeField] private float panelHeight = 420f;

        [Inject] private IGameplaySessionContextService _sessionContextService;
        [Inject] private IGameplayPhaseRuntimeService _phaseRuntimeService;
        [Inject] private IRunResultStagePresenterHost _presenterHost;

        private RunResultStage _stage;
        private IRunResultStageControl _runResultStageControl;
        private bool _dependenciesInjected;
        private bool _isPresentationAttached;
        private string _presenterSignature = string.Empty;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        public string PresenterSignature => _presenterSignature;

        public bool IsReady => _isPresentationAttached && !string.IsNullOrWhiteSpace(_presenterSignature);

        public void AttachToRunResultStage(RunResultStage stage, IRunResultStageControl control)
        {
            if (control == null)
            {
                HardFailFastH1.Trigger(typeof(RunResultStageMockPresenter),
                    "[FATAL][H1][RunResultStage] RunResultStage presenter sem control tipado no attach.");
            }

            if (!stage.IsGameplayScene || string.IsNullOrWhiteSpace(stage.Signature))
            {
                HardFailFastH1.Trigger(typeof(RunResultStageMockPresenter),
                    "[FATAL][H1][RunResultStage] RunResultStage recebido invalido no attach.");
            }

            EnsureDependenciesInjected();

            if (_isPresentationAttached && !string.Equals(_presenterSignature, stage.Signature, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(RunResultStageMockPresenter),
                    $"[FATAL][H1][RunResultStage] Presenter attach tentou trocar stage sem detach previo. current='{_presenterSignature}' next='{stage.Signature}'.");
            }

            _stage = stage;
            _runResultStageControl = control;
            _presenterSignature = Normalize(stage.Signature);
            _isPresentationAttached = true;

            DebugUtility.Log<RunResultStageMockPresenter>(
                $"[OBS][RunResultStage] RunResultStagePresenterAttached presenter='{DescribePresenterLabel()}' scene='{gameObject.scene.name}' signature='{_presenterSignature}' result='{_stage.Result}' reason='{Normalize(_stage.Reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void DetachFromRunResultStage(string reason)
        {
            if (!_isPresentationAttached && string.IsNullOrWhiteSpace(_presenterSignature))
            {
                return;
            }

            DebugUtility.Log<RunResultStageMockPresenter>(
                $"[OBS][RunResultStage] RunResultStagePresenterDetached presenter='{DescribePresenterLabel()}' reason='{Normalize(reason)}' signature='{_presenterSignature}'.",
                DebugUtility.Colors.Info);

            _isPresentationAttached = false;
            _presenterSignature = string.Empty;
            _runResultStageControl = null;
            _stage = default;
        }

        private void OnEnable()
        {
            EnsureDependenciesInjected();
            EnsureTypedRegistration();

            DebugUtility.LogVerbose<RunResultStageMockPresenter>(
                $"[OBS][RunResultStage] RunResultStagePresenterEnabled presenter='{DescribePresenterLabel()}' scene='{gameObject.scene.name}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDisable()
        {
            if (_presenterHost != null)
            {
                _presenterHost.TryUnregisterPresenter(this, "OnDisable");
            }

            _isPresentationAttached = false;
            _presenterSignature = string.Empty;
            _runResultStageControl = null;
            _stage = default;
        }

        private void OnGUI()
        {
            if (!TryBuildViewModel(out RunResultViewModel model))
            {
                return;
            }

            Rect panelRect = BuildPanelRect();
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.BeginVertical();
            EnsureStyles();
            GUILayout.Label("RunResultStage", _titleStyle);
            GUILayout.Label($"result: {model.Result}", _bodyStyle);
            GUILayout.Label($"reason: {model.Reason}", _bodyStyle);
            GUILayout.Label($"signature: {model.Signature}", _bodyStyle);
            GUILayout.Label($"phase: {model.PhaseName}", _bodyStyle);
            GUILayout.Space(10f);
            GUILayout.FlexibleSpace();

            bool previousEnabled = GUI.enabled;
            GUI.enabled = model.CanContinue;
            if (GUILayout.Button("Continue", _buttonStyle, GUILayout.Height(48f)))
            {
                OnClickContinue();
            }

            GUI.enabled = previousEnabled;
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public void OnClickContinue()
        {
            RequestCompletion("RunResultStage/ContinueButton");
        }

        internal bool TryBuildViewModel(out RunResultViewModel model)
        {
            model = default;

            if (!_isPresentationAttached || _stage.Signature.Length == 0 || _runResultStageControl == null)
            {
                return false;
            }

            model = new RunResultViewModel(
                _stage.Result.ToString(),
                Normalize(_stage.Reason),
                _presenterSignature,
                DescribePhaseName(),
                DescribePhaseSignature(),
                DescribeSessionSignature(),
                _runResultStageControl.IsActive);
            return true;
        }

        internal Rect BuildPanelRect()
        {
            float maxWidth = Mathf.Max(420f, Screen.width - 40f);
            float width = Mathf.Clamp(panelWidth, 420f, maxWidth);
            float maxHeight = Mathf.Max(300f, Screen.height - 40f);
            float height = Mathf.Clamp(panelHeight, 300f, maxHeight);
            float x = Mathf.Max(margin, Mathf.Round((Screen.width - width) * 0.5f));
            float y = Mathf.Max(margin, Mathf.Round(Screen.height - height - margin));
            return new Rect(x, y, width, height);
        }

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

        private void RequestCompletion(string reason)
        {
            if (!_isPresentationAttached)
            {
                return;
            }

            if (_runResultStageControl == null)
            {
                HardFailFastH1.Trigger(typeof(RunResultStageMockPresenter),
                    "[FATAL][H1][RunResultStage] Control service ausente no presenter local.");
            }

            DebugUtility.Log<RunResultStageMockPresenter>(
                $"[OBS][RunResultStage] RunResultStagePresenterActionRequested presenter='{DescribePresenterLabel()}' kind='Continue' reason='{Normalize(reason)}' signature='{_presenterSignature}'.",
                DebugUtility.Colors.Info);

            _runResultStageControl?.TryComplete(reason);
        }

        private string DescribePhaseName()
        {
            if (_phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot) && snapshot.IsValid)
            {
                return snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>";
            }

            return "<none>";
        }

        private string DescribePhaseSignature()
        {
            if (_phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot) && snapshot.IsValid)
            {
                return Normalize(snapshot.PhaseRuntimeSignature);
            }

            return "<none>";
        }

        private string DescribeSessionSignature()
        {
            if (_sessionContextService != null && _sessionContextService.TryGetCurrent(out GameplaySessionContextSnapshot snapshot) && snapshot.IsValid)
            {
                return Normalize(snapshot.SessionSignature);
            }

            return "<none>";
        }

        private void EnsureTypedRegistration()
        {
            if (_presenterHost == null)
            {
                HardFailFastH1.Trigger(typeof(RunResultStageMockPresenter),
                    "[FATAL][H1][RunResultStage] IRunResultStagePresenterHost indisponivel para registro tipado.");
                return;
            }

            _presenterHost.TryAdoptPresenter(this, DescribePresenterLabel());
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            if (!DependencyManager.HasInstance)
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

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string DescribePresenterLabel()
            => nameof(RunResultStageMockPresenter);

        internal readonly struct RunResultViewModel
        {
            public RunResultViewModel(string result, string reason, string signature, string phaseName, string phaseSignature, string sessionSignature, bool canContinue)
            {
                Result = string.IsNullOrWhiteSpace(result) ? "<none>" : result.Trim();
                Reason = string.IsNullOrWhiteSpace(reason) ? "<none>" : reason.Trim();
                Signature = string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();
                PhaseName = string.IsNullOrWhiteSpace(phaseName) ? "<none>" : phaseName.Trim();
                PhaseSignature = string.IsNullOrWhiteSpace(phaseSignature) ? "<none>" : phaseSignature.Trim();
                SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? "<none>" : sessionSignature.Trim();
                CanContinue = canContinue;
            }

            public string Result { get; }
            public string Reason { get; }
            public string Signature { get; }
            public string PhaseName { get; }
            public string PhaseSignature { get; }
            public string SessionSignature { get; }
            public bool CanContinue { get; }
        }
    }
}

