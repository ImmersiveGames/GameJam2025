// Path: _ImmersiveGames/Scripts/DamageSystem/Tests/DamageSystemDebugger.cs
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Tests
{
    [DisallowMultipleComponent]
    public partial class DamageSystemDebugger : MonoBehaviour
    {
        [Header("General Debug")]
        [SerializeField] private bool logAllEvents = false;
        [SerializeField] private bool showVisualDebug = true;
        [SerializeField] private Color debugColor = Color.red;

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