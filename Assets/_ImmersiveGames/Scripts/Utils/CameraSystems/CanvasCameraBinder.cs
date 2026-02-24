using System.Threading.Tasks;
using _ImmersiveGames.Scripts.CameraSystems;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.CameraSystems
{
    /// <summary>
    /// Vincula automaticamente a câmera de gameplay (vinda do IOldCameraResolver)
    /// ao Canvas em modo WorldSpace. Suporta troca de câmera em runtime.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour, IResetInterfaces, IResetScopeFilter, IResetOrder
    {
        #region Private Fields

        private Canvas _canvas;
        private IOldCameraResolver _resolver;
        private bool _subscribed;

        #endregion

        #region Reset Ordering / Filtering

        // UI binder pode rebindar depois do player/câmera.
        public int ResetOrder => 50;

        public bool ShouldParticipate(ResetScope scope)
        {
            // Em geral faz sentido em reset amplo; mas permitir PlayersOnly também é seguro.
            return scope == ResetScope.AllActorsInScene || scope == ResetScope.PlayersOnly;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver))
            {
                DebugUtility.LogError<CanvasCameraBinder>(
                    $"[{name}] OldCameraResolverService não encontrado. CanvasCameraBinder desativado.",
                    this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            BindCamera();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _canvas = null;
        }

        #endregion

        #region Camera Binding Logic

        private void Subscribe()
        {
            if (_resolver == null || _subscribed)
                return;

            _resolver.OnDefaultCameraChanged += OnCameraChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_resolver == null || !_subscribed)
                return;

            _resolver.OnDefaultCameraChanged -= OnCameraChanged;
            _subscribed = false;
        }

        private void BindCamera()
        {
            if (_canvas == null)
                return;

            if (_canvas.renderMode != RenderMode.WorldSpace)
                return;

            if (_resolver == null)
                return;

            var cam = _resolver.GetDefaultCamera();
            if (cam == null)
            {
                DebugUtility.LogWarning<CanvasCameraBinder>(
                    $"[{name}] Nenhuma câmera registrada no OldCameraResolverService.",
                    this);
                return;
            }

            _canvas.worldCamera = cam;
        }

        private void OnCameraChanged(Camera newCamera)
        {
            if (_canvas == null)
                return;

            if (_canvas.renderMode != RenderMode.WorldSpace)
                return;

            if (newCamera == null)
                return;

            _canvas.worldCamera = newCamera;
        }

        #endregion

        #region Reset Steps

        public Task Reset_CleanupAsync(ResetContext ctx)
        {
            // Evita callbacks durante reset.
            Unsubscribe();
            return Task.CompletedTask;
        }

        public Task Reset_RestoreAsync(ResetContext ctx)
        {
            // Nada pesado aqui.
            return Task.CompletedTask;
        }

        public Task Reset_RebindAsync(ResetContext ctx)
        {
            Subscribe();
            BindCamera();
            return Task.CompletedTask;
        }

        #endregion
    }
}

