using _ImmersiveGames.Scripts.AudioSystem;
using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    /// <summary>
    /// Núcleo do debugger do sistema de dano — coordena módulos auxiliares.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class DamageSystemDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logAllEvents = true;
        [SerializeField] private bool showVisualDebug = true;
        [SerializeField] private Color debugColor = Color.red;

        [Header("Test Settings")]
        [SerializeField] private float testDamage = 10f;
        [SerializeField] private ResourceType testResource = ResourceType.Health;
        [SerializeField] private float reviveHealth = 100f;

        [Header("Audio Debug")]
        [SerializeField] private bool testAudio = true;

        // Components
        private DamageReceiver _receiver;
        private DamageDealer _dealer;
        private PlayerAudioController _audio;

        // Cache de event bindings
        private readonly List<object> _eventBindings = new();

        private void Awake()
        {
            FindComponents();
        }

        private void Start()
        {
            RegisterLocalEvents();
            RegisterGlobalEvents();

            LogStartup();
        }

        private void FindComponents()
        {
            _receiver = GetComponent<DamageReceiver>();
            _dealer = GetComponent<DamageDealer>();
            _audio = GetComponent<PlayerAudioController>();
        }

        private void LogStartup()
        {
            Debug.Log("🧩 DamageSystemDebugger Initialized");
            Debug.Log($"   Receiver: {_receiver != null}");
            Debug.Log($"   Dealer: {_dealer != null}");
            Debug.Log($"   AudioController: {_audio != null}");
        }

        private void OnDestroy()
        {
            UnregisterLocalEvents();
            UnregisterGlobalEvents();
        }

        private string GetObjectName()
        {
            return _receiver?.Actor?.ActorName ?? gameObject.name;
        }

        public bool IsVerbose => logAllEvents;
        public bool VisualDebug => showVisualDebug;
        public Color DebugColor => debugColor;
        public DamageReceiver Receiver => _receiver;
        public DamageDealer Dealer => _dealer;
        public PlayerAudioController Audio => _audio;
        public float TestDamage => testDamage;
        public float ReviveHealth => reviveHealth;
        public ResourceType TestResource => testResource;
    }
}
