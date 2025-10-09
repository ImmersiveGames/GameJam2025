// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.cs
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public partial class DamageSystemDebugger : MonoBehaviour
    {
        [Header("General Debug")]
        [SerializeField] private bool logAllEvents;
        [SerializeField] private bool showVisualDebug = true;
        [SerializeField] private Color debugColor = Color.red;

        // Novo: Campos para testes de áudio (volumes ajustáveis no Inspector)
        [Header("Audio Test Volumes")]
        [SerializeField] private float shootVolume = 1f;
        [SerializeField] private float hitVolume = 1f;
        [SerializeField] private float deathVolume = 1f;
        [SerializeField] private float reviveVolume = 1f;
        
        [Header("BGM Test Settings")]
        [SerializeField] private SoundData testBgmData; // Atribua um SoundData no Inspector para testes
        [SerializeField] private float fadeDuration = 2f;

        private DamageReceiver _receiver;
        private PlayerAudioController _audio;
        private DamageDealer _dealer;

        private void Awake()
        {
            _receiver = GetComponent<DamageReceiver>();
            _audio = GetComponent<PlayerAudioController>();
            _dealer = GetComponent<DamageDealer>();
        }

        private void Start()
        {
            RegisterLocalEvents();
            RegisterGlobalEvents();
        }

        private void OnDestroy()
        {
            UnregisterLocalEvents();
            UnregisterGlobalEvents();
        }

        private string GetObjectName() => _receiver?.Actor?.ActorName ?? gameObject.name;
        public bool IsVerbose => logAllEvents;
    }
}