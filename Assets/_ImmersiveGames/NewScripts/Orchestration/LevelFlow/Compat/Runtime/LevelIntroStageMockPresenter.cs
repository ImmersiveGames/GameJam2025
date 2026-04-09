#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Compat.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/IntroStage/IntroStage Presenter Content")]
    // Presenter local de conteudo da IntroStage. A projeção visual é ativada pelo host.
    public sealed class LevelIntroStageMockPresenter : MonoBehaviour, ILevelIntroStagePresenter
    {
        [Header("Layout")]
        [SerializeField] private float margin = 16f;
        [SerializeField] private float panelWidth = 420f;
        [SerializeField] private float panelHeight = 200f;

        private IIntroStageControlService? _controlService;
        private GameObject? _visualSurfaceRoot;
        private LevelStagePresentationContract _presentationContract;
        private string _presenterSignature = string.Empty;
        private bool _isPresentationAttached;
        private bool _isPresentationClosed;

        public string PresenterSignature => _presenterSignature;

        public bool IsPresentationAttached => _isPresentationAttached && !_isPresentationClosed;

        public bool CanServe(string sessionSignature)
            => !string.IsNullOrWhiteSpace(_presenterSignature) &&
               string.Equals(_presenterSignature, Normalize(sessionSignature), System.StringComparison.Ordinal);

        public void AttachPresentation(LevelStagePresentationContract contract)
        {
            if (!contract.IsValid || contract.PhaseDefinitionRef == null || string.IsNullOrWhiteSpace(contract.LevelSignature) ||
                string.IsNullOrWhiteSpace(contract.LocalContentId) || !contract.HasIntroStage)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageMockPresenter),
                    "[FATAL][H1][IntroStage] Presenter attach recebeu contrato invalido ou sem IntroStage.");
            }

            if (_isPresentationAttached)
            {
                if (!string.Equals(_presenterSignature, contract.LevelSignature, System.StringComparison.Ordinal))
                {
                    HardFailFastH1.Trigger(typeof(LevelIntroStageMockPresenter),
                        $"[FATAL][H1][IntroStage] Presenter attach tentou trocar contrato sem detach previo. current='{_presenterSignature}' next='{contract.LevelSignature}'.");
                }

                _presentationContract = contract;
                _presenterSignature = contract.LevelSignature;
                _isPresentationClosed = false;
                ResolveDependencies();
                EnsureVisualSurface();
                return;
            }

            _presentationContract = contract;
            _presenterSignature = contract.LevelSignature;
            _isPresentationClosed = false;
            _isPresentationAttached = true;
            ResolveDependencies();

            if (_controlService == null)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageMockPresenter),
                    "[FATAL][H1][IntroStage] IIntroStageControlService ausente. O presenter da intro nao pode ser ativado.");
            }

            string contractContentName = ResolveContractContentName(contract);
            DebugUtility.Log<LevelIntroStageMockPresenter>(
                $"[OBS][IntroStage][Compat] IntroStagePresenterAttached presenterType='{GetType().Name}' contentName='{contractContentName}' contentId='{contract.LocalContentId}' signature='{contract.LevelSignature}' compatResidual='true'.",
                DebugUtility.Colors.Info);

            EnsureVisualSurface();
        }

        public void DetachPresentation(string reason)
        {
            ReleasePresentationState(logDetached: true, reason);
        }

        private void OnEnable()
        {
            ResolveDependencies();
        }

        private void OnDisable()
        {
            ReleasePresentationState(logDetached: false, "OnDisable");
        }

        public void OnClickContinue()
        {
            if (_controlService == null)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageMockPresenter),
                    "[FATAL][H1][IntroStage] IIntroStageControlService ausente. O presenter da intro nao pode confirmar.");
                return;
            }

            _controlService.CompleteIntroStage("IntroStage/ContinueButton");
        }

        internal bool TryBuildViewModel(out IntroViewModel model)
        {
            model = default;

            if (!_isPresentationAttached || _isPresentationClosed)
            {
                return false;
            }

            if (_controlService is not IIntroStageControlService controlService)
            {
                return false;
            }

            if (!_presentationContract.IsValid || _presentationContract.PhaseDefinitionRef == null ||
                string.IsNullOrWhiteSpace(_presentationContract.LevelSignature) ||
                string.IsNullOrWhiteSpace(_presentationContract.LocalContentId))
            {
                return false;
            }

            string contentName = ResolveContractContentName(_presentationContract);
            model = new IntroViewModel(
                contentName,
                _presentationContract.LocalContentId,
                _presentationContract.LevelSignature,
                controlService.IsIntroStageActive);
            return true;
        }

        internal Rect BuildPanelRect()
        {
            float maxWidth = Mathf.Max(320f, Screen.width - 32f);
            float width = Mathf.Clamp(panelWidth, 320f, maxWidth);
            float maxHeight = Mathf.Max(180f, Screen.height - 32f);
            float height = Mathf.Min(panelHeight, maxHeight);
            float x = Mathf.Max(margin, Mathf.Round((Screen.width - width) * 0.5f));
            float y = Mathf.Max(margin, Mathf.Round(Screen.height - height - margin));
            return new Rect(x, y, width, height);
        }

        private void ResolveDependencies()
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (_controlService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _controlService);
            }
        }

        private void EnsureVisualSurface()
        {
            if (!_isPresentationAttached || _isPresentationClosed || _visualSurfaceRoot != null)
            {
                return;
            }

            GameObject surfaceRoot = new GameObject("IntroStagePresenterSurface");
            surfaceRoot.transform.SetParent(transform, false);
            surfaceRoot.hideFlags = HideFlags.DontSave;
            surfaceRoot.AddComponent<LevelIntroStageMockPresenterSurface>().Bind(this);
            _visualSurfaceRoot = surfaceRoot;

            string contractContentName = ResolveContractContentName(_presentationContract);
            DebugUtility.Log<LevelIntroStageMockPresenter>(
                $"[OBS][IntroStage][Compat] IntroStagePresenterSurfaceCreated presenterType='{GetType().Name}' contentName='{contractContentName}' contentId='{_presentationContract.LocalContentId}' signature='{_presentationContract.LevelSignature}' compatResidual='true'.",
                DebugUtility.Colors.Info);
        }

        private void DestroyVisualSurface()
        {
            if (_visualSurfaceRoot == null)
            {
                return;
            }

            GameObject visualSurface = _visualSurfaceRoot;
            _visualSurfaceRoot = null;

            if (Application.isPlaying)
            {
                Destroy(visualSurface);
            }
            else
            {
                DestroyImmediate(visualSurface);
            }

            DebugUtility.Log<LevelIntroStageMockPresenter>(
                $"[OBS][IntroStage][Compat] IntroStagePresenterSurfaceDestroyed presenterType='{GetType().Name}' signature='{_presenterSignature}' compatResidual='true'.",
                DebugUtility.Colors.Info);
        }

        private void ReleasePresentationState(bool logDetached, string reason)
        {
            if (!_isPresentationAttached && _visualSurfaceRoot == null)
            {
                return;
            }

            DestroyVisualSurface();

            if (logDetached)
            {
                DebugUtility.Log<LevelIntroStageMockPresenter>(
                    $"[OBS][IntroStage][Compat] IntroStagePresenterDetached presenterType='{GetType().Name}' signature='{_presenterSignature}' reason='{Normalize(reason)}' compatResidual='true'.",
                    DebugUtility.Colors.Info);
            }

            _isPresentationAttached = false;
            _isPresentationClosed = true;
            _presenterSignature = string.Empty;
            _presentationContract = default;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string ResolveContractContentName(LevelStagePresentationContract contract)
        {
            if (contract.PhaseDefinitionRef != null)
            {
                return contract.PhaseDefinitionRef.name;
            }

            return "<none>";
        }

        internal readonly struct IntroViewModel
        {
            public IntroViewModel(string contentName, string contentId, string sessionSignature, bool canContinue)
            {
                ContentName = string.IsNullOrWhiteSpace(contentName) ? "<none>" : contentName.Trim();
                ContentId = string.IsNullOrWhiteSpace(contentId) ? "<none>" : contentId.Trim();
                SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? "<none>" : sessionSignature.Trim();
                CanContinue = canContinue;
            }

            public string ContentName { get; }
            public string ContentId { get; }
            public string SessionSignature { get; }
            public bool CanContinue { get; }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class LevelIntroStageMockPresenterSurface : MonoBehaviour
    {
        private LevelIntroStageMockPresenter? _owner;

        public void Bind(LevelIntroStageMockPresenter owner)
        {
            _owner = owner;
        }

        private void OnGUI()
        {
            if (_owner == null)
            {
                return;
            }

            if (!_owner.TryBuildViewModel(out LevelIntroStageMockPresenter.IntroViewModel model))
            {
                return;
            }

            Rect rect = _owner.BuildPanelRect();
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("IntroStage");
            GUILayout.Label($"content: {model.ContentName}");
            GUILayout.Label($"contentId: {model.ContentId}");
            GUILayout.Label($"signature: {model.SessionSignature}");
            GUILayout.Space(8f);

            bool previous = GUI.enabled;
            GUI.enabled = model.CanContinue;
            if (GUILayout.Button("Continue", GUILayout.Height(34f)))
            {
                _owner.OnClickContinue();
            }

            GUI.enabled = previous;
            GUILayout.EndArea();
        }
    }
}
