using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Parte da implementação do Eater focada em recursos, AutoFlow e cura.
    /// 
    /// OBS: Nesta etapa, todos os métodos continuam em EaterBehavior.Core.cs.
    /// Este arquivo partial existe para permitir uma futura migração organizada.
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour
    {
        private RuntimeAttributeAutoFlowBridge _autoFlowBridge;
        private bool _missingAutoFlowBridgeLogged;
        private bool _autoFlowUnavailableLogged;
        private bool _missingResourceSystemLogged;
        private IDamageReceiver _selfDamageReceiver;
        private bool _missingSelfDamageReceiverLogged;
        
        internal bool TryApplySelfHealing(RuntimeAttributeType runtimeAttributeType, float amount,
            DamageType healDamageType = DamageType.Pure)
        {
            if (amount <= Mathf.Epsilon)
            {
                return false;
            }

            if (!TryGetSelfDamageReceiver(out IDamageReceiver damageReceiver))
            {
                if (logStateTransitions && !_missingSelfDamageReceiverLogged)
                {
                    DebugUtility.LogWarning(
                        "DamageReceiver do eater não encontrado. Não foi possível recuperar recursos via DamageSystem.",
                        this,
                        this);
                    _missingSelfDamageReceiverLogged = true;
                }

                return false;
            }

            _missingSelfDamageReceiverLogged = false;

            float clampedAmount = Mathf.Max(0f, amount);
            if (clampedAmount <= Mathf.Epsilon)
            {
                return false;
            }

            string attackerId = Master != null ? Master.ActorId : string.Empty;
            string targetId = damageReceiver.GetReceiverId();
            Vector3 hitPosition = transform.position;

            var context = new DamageContext(attackerId, targetId, -clampedAmount, runtimeAttributeType, healDamageType, hitPosition);
            damageReceiver.ReceiveDamage(context);
            return true;
        }

        private bool TryGetSelfDamageReceiver(out IDamageReceiver damageReceiver)
        {
            if (_selfDamageReceiver == null)
            {
                if (Master != null)
                {
                    string actorId = Master.ActorId;
                    if (!string.IsNullOrEmpty(actorId)
                        && DependencyManager.Provider.TryGetForObject(actorId, out IDamageReceiver resolvedReceiver))
                    {
                        _selfDamageReceiver = resolvedReceiver;
                    }
                }

                if (_selfDamageReceiver == null)
                {
                    TryGetComponent(out _selfDamageReceiver);
                }
            }

            damageReceiver = _selfDamageReceiver;
            return damageReceiver != null;
        }
        
        internal bool ResumeAutoFlow(string reason)
        {
            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("RuntimeAttributeAutoFlowBridge não encontrado para controlar AutoFlow.", ref _missingAutoFlowBridgeLogged);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("RuntimeAttributeAutoFlowBridge ainda não possui serviço inicializado.", ref _autoFlowUnavailableLogged);
                return false;
            }

            bool resumed = _autoFlowBridge.ResumeAutoFlow();
            LogAutoFlowResult(resumed,
                resumed
                    ? $"AutoFlow retomado ({reason})."
                    : $"AutoFlow permaneceu pausado ({reason}).");
            return resumed;
        }

        internal bool PauseAutoFlow(string reason)
        {
            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue("RuntimeAttributeAutoFlowBridge não encontrado para pausar AutoFlow.", ref _missingAutoFlowBridgeLogged);
                return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue("RuntimeAttributeAutoFlowBridge ainda não possui serviço inicializado.", ref _autoFlowUnavailableLogged);
                return false;
            }

            bool paused = _autoFlowBridge.PauseAutoFlow();
            LogAutoFlowResult(paused,
                paused
                    ? $"AutoFlow pausado ({reason})."
                    : $"Falha ao pausar AutoFlow ({reason}).");
            return paused;
        }
        
         private bool TryEnsureAutoFlowBridge()
        {
            if (_autoFlowBridge != null)
            {
                return true;
            }

            if (TryGetComponent(out RuntimeAttributeAutoFlowBridge bridge))
            {
                _autoFlowBridge = bridge;
                _missingAutoFlowBridgeLogged = false;
                _autoFlowUnavailableLogged = false;
                _missingResourceSystemLogged = false;
                return true;
            }

            return false;
        }

        private void LogAutoFlowIssue(string message, ref bool cacheFlag)
        {
            if (!logStateTransitions || cacheFlag)
            {
                return;
            }

            DebugUtility.LogWarning(message, this, this);
            cacheFlag = true;
        }

        private void LogAutoFlowResult(bool success, string message)
        {
            if (!logStateTransitions)
            {
                return;
            }

            if (success)
            {
                DebugUtility.LogVerbose(message, DebugUtility.Colors.Success, this, this);
            }
            else
            {
                DebugUtility.LogWarning(message, this, this);
            }
        }

        internal bool TryRestoreResource(RuntimeAttributeType runtimeAttributeType, float amount)
        {
            if (amount <= Mathf.Epsilon)
            {
                return false;
            }

            if (!TryEnsureAutoFlowBridge())
            {
                LogAutoFlowIssue(
                    "RuntimeAttributeAutoFlowBridge não encontrado para recuperar recursos manualmente.",
                    ref _missingAutoFlowBridgeLogged);
           

            return false;
            }

            if (!_autoFlowBridge.HasAutoFlowService)
            {
                LogAutoFlowIssue(
                    "RuntimeAttributeAutoFlowBridge ainda não possui serviço inicializado para recuperar recursos manualmente.",
                    ref _autoFlowUnavailableLogged);
                return false;
            }

            RuntimeAttributeContext runtimeAttributeContext = _autoFlowBridge.GetResourceSystem();
            if (runtimeAttributeContext == null)
            {
                LogAutoFlowIssue(
                    "RuntimeAttributeContext indisponível ao tentar recuperar recursos manualmente.",
                    ref _missingResourceSystemLogged);
                return false;
            }

            _missingResourceSystemLogged = false;
            runtimeAttributeContext.Modify(runtimeAttributeType, Mathf.Max(0f, amount));
            return true;
        }
    }
}