#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Compat.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/Compat/LevelFlow/EnterStage/Level EnterStage Presenter Mock")]
    // Historical compat presenter kept for the EnterStage seam.
    public sealed class LevelIntroStageMockPresenter : MonoBehaviour, ILevelIntroStagePresenter
    {
        [Header("Layout")]
        [SerializeField] private float margin = 16f;
        [SerializeField] private float panelWidth = 360f;
        [SerializeField] private float panelHeight = 220f;

        private ILevelStagePresentationService? _stagePresentationService;
        private IIntroStageControlService? _controlService;
        private ILevelIntroStagePresenterRegistry? _presenterRegistry;
        private string _presenterSignature = string.Empty;
        private bool _isRegistered;

        public string PresenterSignature => _presenterSignature;
        public bool IsReady => _isRegistered && gameObject.activeInHierarchy;

        public void BindToSession(string sessionSignature)
        {
            _presenterSignature = Normalize(sessionSignature);
            ResolveDependencies();
            TryRegister();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            TryRegister();
        }

        private void OnDisable()
        {
            if (_presenterRegistry != null && _isRegistered)
            {
                _presenterRegistry.Unregister(this);
                _isRegistered = false;
            }
        }

        private void OnGUI()
        {
            if (!_isRegistered)
            {
                TryRegister();
            }

            if (!TryBuildViewModel(out var model))
            {
                return;
            }

            if (_controlService is not IIntroStageControlService controlService)
            {
                return;
            }

            Rect rect = BuildPanelRect();
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("EnterStage");
            GUILayout.Label($"levelRef: {model.LevelRefName}");
            GUILayout.Label($"contentId: {model.ContentId}");
            GUILayout.Label($"levelSignature: {model.LevelSignature}");
            GUILayout.Label("Mock presentation for EnterStage.");

            GUILayout.Space(8f);

            bool previous = GUI.enabled;
            GUI.enabled = model.CanContinue;
            if (GUILayout.Button("Continue", GUILayout.Height(34f)))
            {
                controlService.CompleteIntroStage("EnterStage/ContinueButton");
            }

            GUI.enabled = previous;
            GUILayout.EndArea();
        }

        private Rect BuildPanelRect()
        {
            float maxWidth = Mathf.Max(320f, Screen.width - 32f);
            float width = Mathf.Clamp(panelWidth, 320f, maxWidth);
            float maxHeight = Mathf.Max(180f, Screen.height - 32f);
            float height = Mathf.Min(panelHeight, maxHeight);
            float x = Mathf.Max(margin, Mathf.Round((Screen.width - width) * 0.5f));
            float y = Mathf.Max(margin, Mathf.Round(Screen.height - height - margin));
            return new Rect(x, y, width, height);
        }

        private bool TryBuildViewModel(out IntroViewModel model)
        {
            model = default;
            ResolveDependencies();
            if (!_isRegistered)
            {
                TryRegister();
            }

            if (_stagePresentationService is not ILevelStagePresentationService stagePresentationService ||
                _controlService is not IIntroStageControlService controlService ||
                _presenterRegistry is not ILevelIntroStagePresenterRegistry presenterRegistry)
            {
                return false;
            }

            if (!stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                !contract.HasIntroStage ||
                contract.LevelRef == null)
            {
                return false;
            }

            if (!controlService.IsIntroStageActive)
            {
                return false;
            }

            if (_isRegistered &&
                string.Equals(_presenterSignature, contract.LevelSignature, System.StringComparison.Ordinal) &&
                presenterRegistry.TryGetCurrentPresenter(out ILevelIntroStagePresenter currentPresenter) &&
                ReferenceEquals(currentPresenter, this))
            {
                model = new IntroViewModel(
                    contract.LevelRef.name,
                    contract.LocalContentId,
                    contract.LevelSignature,
                    true);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_presenterSignature) &&
                !string.Equals(_presenterSignature, contract.LevelSignature, System.StringComparison.Ordinal))
            {
                return false;
            }

            model = new IntroViewModel(
                contract.LevelRef.name,
                contract.LocalContentId,
                contract.LevelSignature,
                true);
            return true;
        }

        private void ResolveDependencies()
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (_stagePresentationService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _stagePresentationService);
            }

            if (_controlService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _controlService);
            }

            if (_presenterRegistry == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _presenterRegistry);
            }
        }

        private void TryRegister()
        {
            if (_presenterRegistry is not ILevelIntroStagePresenterRegistry presenterRegistry)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageMockPresenter),
                    "[FATAL][H1][LevelFlow] ILevelIntroStagePresenterRegistry ausente. O presenter do level nao pode se registrar.");
                return;
            }

            if (_stagePresentationService is not ILevelStagePresentationService stagePresentationService ||
                !stagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                !contract.HasIntroStage)
            {
                return;
            }

            if (presenterRegistry.TryGetCurrentPresenter(out ILevelIntroStagePresenter currentPresenter) &&
                ReferenceEquals(currentPresenter, this) &&
                string.Equals(_presenterSignature, contract.LevelSignature, System.StringComparison.Ordinal))
            {
                _isRegistered = true;
                return;
            }

            if (_isRegistered &&
                string.Equals(_presenterSignature, contract.LevelSignature, System.StringComparison.Ordinal))
            {
                return;
            }

            if (_isRegistered)
            {
                presenterRegistry.Unregister(this);
                _isRegistered = false;
            }

            _presenterSignature = contract.LevelSignature;
            presenterRegistry.Register(this, _presenterSignature);
            _isRegistered = true;

            DebugUtility.Log<LevelIntroStageMockPresenter>(
                $"[OBS][LevelFlow] EnterStagePresenterRegistered levelRef='{contract.LevelRef.name}' contentId='{contract.LocalContentId}' signature='{contract.LevelSignature}' presenter='{name}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private readonly struct IntroViewModel
        {
            public IntroViewModel(string levelRefName, string contentId, string levelSignature, bool canContinue)
            {
                LevelRefName = string.IsNullOrWhiteSpace(levelRefName) ? "<none>" : levelRefName.Trim();
                ContentId = string.IsNullOrWhiteSpace(contentId) ? "<none>" : contentId.Trim();
                LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? "<none>" : levelSignature.Trim();
                CanContinue = canContinue;
            }

            public string LevelRefName { get; }
            public string ContentId { get; }
            public string LevelSignature { get; }
            public bool CanContinue { get; }
        }
    }
}
