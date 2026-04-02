using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation.Compat
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    // Historical compat mock presenter for the PostStage visual seam.
    public sealed class LevelPostStageMockPresenter : MonoBehaviour, IPostStagePresenter
    {
        private const float PanelWidth = 360f;
        private const float PanelHeight = 190f;

        [SerializeField] private Vector2 panelOffset = new Vector2(24f, 24f);

        private PostStageContext _context;
        private IPostStageControlService _controlService;
        private bool _isBound;
        private bool _hasActionBeenRequested;
        private bool _isRegisteredLogged;

        public string PresenterSignature { get; private set; } = string.Empty;

        public bool IsReady => _isBound &&
                               _controlService != null &&
                               !string.IsNullOrWhiteSpace(PresenterSignature);

        private void OnEnable()
        {
            if (_isRegisteredLogged)
            {
                return;
            }

            _isRegisteredLogged = true;
            DebugUtility.Log<LevelPostStageMockPresenter>(
                $"[OBS][PostRun] PostStagePresenterRegistered presenter='{name}' scene='{gameObject.scene.name}'.",
                DebugUtility.Colors.Info);
        }

        public void BindToSession(PostStageContext context, IPostStageControlService controlService)
        {
            _context = context;
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            PresenterSignature = Normalize(context.Signature);
            _isBound = true;
            _hasActionBeenRequested = false;
        }

        private void OnGUI()
        {
            if (!_isBound || _hasActionBeenRequested)
            {
                return;
            }

            Rect panelRect = new Rect(panelOffset.x, panelOffset.y, PanelWidth, PanelHeight);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("PostStage");
            GUILayout.Label($"Scene: {Normalize(_context.SceneName)}");
            GUILayout.Label($"Signature: {PresenterSignature}");
            GUILayout.Label($"Outcome: {_context.Outcome}");
            GUILayout.Label($"Reason: {Normalize(_context.Reason)}");
            GUILayout.Space(8f);

            if (GUILayout.Button("Continue", GUILayout.Height(32f)))
            {
                _hasActionBeenRequested = true;
                _controlService?.TryComplete("PostStage/ContinueButton");
            }

            if (GUILayout.Button("Skip", GUILayout.Height(32f)))
            {
                _hasActionBeenRequested = true;
                _controlService?.TrySkip("PostStage/SkipButton");
            }

            GUILayout.EndArea();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

