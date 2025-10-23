using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Debug
{
    /// <summary>
    /// Utilitário de debug para acompanhar o fluxo de estados e eventos do Eater em tempo real.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Eater/Eater Behavior Debug Utility")]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterBehaviorDebugUtility : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField, Tooltip("Comportamento principal do Eater que será inspecionado.")]
        private EaterBehavior behavior;
        [SerializeField, Tooltip("Master do Eater para capturar eventos de mordida e consumo.")]
        private EaterMaster master;

        [Header("Opções de Log")]
        [SerializeField, Tooltip("Escreve uma entrada de log sempre que o estado do comportamento mudar.")]
        private bool autoLogStateChanges = true;
        [SerializeField, Tooltip("Gera um snapshot detalhado a cada transição de estado.")]
        private bool logSnapshotOnChange = true;
        [SerializeField, Tooltip("Loga eventos de comer, mordidas e consumo emitidos pelo Master.")]
        private bool logMasterEvents = true;

        private bool _captureNextTransition;
        private readonly StringBuilder _builder = new StringBuilder(512);

        private void Reset()
        {
            behavior = GetComponent<EaterBehavior>();
            master = GetComponent<EaterMaster>();
        }

        private void Awake()
        {
            if (behavior == null)
            {
                behavior = GetComponent<EaterBehavior>();
            }

            if (master == null)
            {
                master = GetComponent<EaterMaster>();
            }
        }

        private void OnEnable()
        {
            if (behavior != null)
            {
                behavior.EventStateChanged += OnStateChanged;
            }

            if (master != null)
            {
                master.EventStartEatPlanet += OnStartEatPlanet;
                master.EventEndEatPlanet += OnEndEatPlanet;
                master.EventEaterBite += OnBite;
                master.EventConsumeResource += OnConsumeResource;
                master.EventEaterTakeDamage += OnTakeDamage;
            }
        }

        private void OnDisable()
        {
            if (behavior != null)
            {
                behavior.EventStateChanged -= OnStateChanged;
            }

            if (master != null)
            {
                master.EventStartEatPlanet -= OnStartEatPlanet;
                master.EventEndEatPlanet -= OnEndEatPlanet;
                master.EventEaterBite -= OnBite;
                master.EventConsumeResource -= OnConsumeResource;
                master.EventEaterTakeDamage -= OnTakeDamage;
            }
        }

        [ContextMenu("Debug/Log Snapshot Atual")]
        private void ContextLogSnapshot()
        {
            LogSnapshot("📸 Snapshot manual do Eater");
        }

        [ContextMenu("Debug/Capturar Próxima Transição")]
        private void ContextCaptureNextTransition()
        {
            _captureNextTransition = true;
            DebugUtility.Log<EaterBehaviorDebugUtility>("Próxima transição será registrada com detalhes.", instance: this);
        }

        private void OnStateChanged(IState previous, IState current)
        {
            string from = GetStateName(previous);
            string to = GetStateName(current);

            if (autoLogStateChanges || _captureNextTransition)
            {
                DebugUtility.Log<EaterBehaviorDebugUtility>($"🔄 Estado alterado: {from} → {to}", instance: this);
            }

            if (logSnapshotOnChange || _captureNextTransition)
            {
                LogSnapshot($"📊 Snapshot após transição {from} → {to}");
            }

            _captureNextTransition = false;
        }

        private void OnStartEatPlanet(IDetectable target)
        {
            if (!logMasterEvents)
            {
                return;
            }

            DebugUtility.Log<EaterBehaviorDebugUtility>($"🍽️ Início do consumo: {DescribeTarget(target)}", instance: this);
        }

        private void OnEndEatPlanet(IDetectable target)
        {
            if (!logMasterEvents)
            {
                return;
            }

            DebugUtility.Log<EaterBehaviorDebugUtility>($"✅ Consumo finalizado: {DescribeTarget(target)}", instance: this);
        }

        private void OnBite(IDetectable target)
        {
            if (!logMasterEvents)
            {
                return;
            }

            DebugUtility.Log<EaterBehaviorDebugUtility>($"🦷 Mordida aplicada em: {DescribeTarget(target)}", instance: this);
        }

        private void OnConsumeResource(IDetectable target, bool satisfied, IActor byActor)
        {
            if (!logMasterEvents)
            {
                return;
            }

            string source = byActor?.ActorName ?? "Desconhecido";
            DebugUtility.Log<EaterBehaviorDebugUtility>($"🍽️ Consumiu recurso de {DescribeTarget(target)} | Satisfeito: {satisfied} | Fonte: {source}", instance: this);
        }

        private void OnTakeDamage(IActor byActor)
        {
            if (!logMasterEvents)
            {
                return;
            }

            string attacker = byActor?.ActorName ?? "Desconhecido";
            DebugUtility.Log<EaterBehaviorDebugUtility>($"⚠️ Eater recebeu dano de {attacker}", instance: this);
        }

        private void LogSnapshot(string title)
        {
            if (behavior == null)
            {
                DebugUtility.LogWarning<EaterBehaviorDebugUtility>("Nenhum EaterBehavior configurado para debug.", this);
                return;
            }

            EaterBehaviorDebugSnapshot snapshot = behavior.CreateDebugSnapshot();
            if (!snapshot.IsValid)
            {
                DebugUtility.LogWarning<EaterBehaviorDebugUtility>("StateMachine do Eater ainda não está pronta para snapshot.", this);
                return;
            }

            _builder.Clear();
            _builder.AppendLine(title);
            _builder.AppendLine($"- Estado atual: {snapshot.CurrentState}");
            _builder.AppendLine($"- Fome: {snapshot.IsHungry}, Comendo: {snapshot.IsEating}");
            _builder.AppendLine($"- Alvo: {(snapshot.HasTarget ? snapshot.TargetName : "Nenhum")}");
            _builder.AppendLine($"- Timer do estado: {snapshot.StateTimer:F2}s");

            if (snapshot.HasWanderingTimer)
            {
                _builder.AppendLine($"- Timer de vagar: running={snapshot.WanderingTimerRunning}, finalizado={snapshot.WanderingTimerFinished}, valor={snapshot.WanderingTimerValue:F2}s/{snapshot.WanderingDuration:F2}s");
            }

            if (snapshot.HasPlayerAnchor)
            {
                _builder.AppendLine($"- Âncora de players: {snapshot.PlayerAnchor}");
            }

            if (snapshot.HasAutoFlow)
            {
                _builder.AppendLine($"- AutoFlow: ativo={snapshot.AutoFlowActive}, pendente={snapshot.PendingHungryEffects}");
            }

            _builder.AppendLine($"- Desejos ativos: {snapshot.DesiresActive}");
            _builder.AppendLine($"- Posição atual: {snapshot.Position}");

            DebugUtility.Log<EaterBehaviorDebugUtility>(_builder.ToString(), instance: this);
        }

        private static string DescribeTarget(IDetectable target)
        {
            if (target?.Owner == null)
            {
                return "Sem alvo";
            }

            string actorName = target.Owner.ActorName;
            return string.IsNullOrEmpty(actorName) ? target.Owner.Transform.name : actorName;
        }

        private static string GetStateName(IState state)
        {
            return state?.GetType().Name ?? "None";
        }
    }
}
