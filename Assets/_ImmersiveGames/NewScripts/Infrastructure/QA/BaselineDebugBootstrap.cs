using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// QA-only (opt-in) bootstrap para supressão defensiva de "repeated-call warnings".
    ///
    /// POLÍTICA (corrigida):
    /// - Por padrão (sem define), este arquivo NÃO executa nada (zero side-effects, zero logs).
    /// - Só habilita quando o projeto define: NEWSCRIPTS_QA_BASELINE
    /// - Runner (quando existir) deve chamar SetRunnerActive(true/false) explicitamente.
    ///
    /// Motivação:
    /// - Evitar poluição de logs e comportamento global invisível em builds/dev/editor.
    /// - Manter a ferramenta disponível apenas para cenários de QA controlados.
    /// </summary>
    internal static class BaselineDebugBootstrap
    {
        // Mantém API estável mesmo quando desabilitado.
        internal static void SetRunnerActive(bool active)
        {
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && NEWSCRIPTS_QA_BASELINE
            IsRunnerActive = active;
#else
            _ = active;
#endif
        }

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && NEWSCRIPTS_QA_BASELINE
        private static bool IsRunnerActive { get; set; }

        private const string DriverName = "BaselineDebugBootstrapDriver";

        private static bool _hasSavedPrevious;
        private static bool _previousRepeatedVerbose = true;
        private static GameObject _driverObject;

        // Silencioso por padrão. Se quiser logar localmente, troque para true.
        private const bool VerboseLogs = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsRunnerActive = false;
            _hasSavedPrevious = false;
            _previousRepeatedVerbose = true;
            DestroyDriverIfExists();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRepeatedCallWarningsPreScene()
        {
            // Não faça nada se nenhum runner estiver ativo.
            // Isso remove totalmente o impacto em boot "normal".
            if (!IsRunnerActive)
            {
                return;
            }

            if (_hasSavedPrevious)
            {
                return;
            }

            _previousRepeatedVerbose = _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.GetRepeatedCallVerbose();
            _hasSavedPrevious = true;

            _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.SetRepeatedCallVerbose(false);

            if (VerboseLogs)
            {
                _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.Log(
                    typeof(BaselineDebugBootstrap),
                    "[Baseline] Repeated-call warning desabilitado (QA runner ativo).");
            }

            CreateDriver();
        }

        private static void CreateDriver()
        {
            if (_driverObject != null)
            {
                return;
            }

            _driverObject = new GameObject(DriverName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<BaselineDebugBootstrapDriver>();
        }

        private static void DestroyDriverIfExists()
        {
            if (_driverObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_driverObject);
            }
            else
            {
                Object.DestroyImmediate(_driverObject);
            }

            _driverObject = null;
        }

        private sealed class BaselineDebugBootstrapDriver : MonoBehaviour
        {
            private System.Collections.IEnumerator Start()
            {
                // 1 frame: garante que o runner tenha chance de marcar ativo/inativo corretamente
                yield return null;

                if (!_hasSavedPrevious)
                {
                    Destroy(gameObject);
                    yield break;
                }

                // Se o runner ainda está ativo, ele é o owner da política; não restauramos.
                if (IsRunnerActive)
                {
                    if (VerboseLogs)
                    {
                        _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.Log(
                            typeof(BaselineDebugBootstrap),
                            "[Baseline] Skip restore (runner ainda ativo).");
                    }

                    Destroy(gameObject);
                    yield break;
                }

                // Sem runner -> restaura ao valor anterior.
                _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.SetRepeatedCallVerbose(_previousRepeatedVerbose);

                if (VerboseLogs)
                {
                    _ImmersiveGames.NewScripts.Infrastructure.DebugLog.DebugUtility.Log(
                        typeof(BaselineDebugBootstrap),
                        "[Baseline] Repeated-call warning restaurado (runner inativo).");
                }

                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                if (ReferenceEquals(_driverObject, gameObject))
                {
                    _driverObject = null;
                }
            }
        }
#endif
    }
}
