using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityPlayerActorScanTarget
    {
        public ActivityCapabilityPlayerActorScanTarget(
            string playerActorId,
            string playerSlotId,
            GameObject actorRoot,
            string sourceScene,
            string sourceContent,
            string source)
        {
            PlayerActorId = Normalize(playerActorId);
            PlayerSlotId = Normalize(playerSlotId);
            ActorRoot = actorRoot;
            SourceScene = Normalize(sourceScene);
            SourceContent = Normalize(sourceContent);
            Source = Normalize(source);
        }

        public string PlayerActorId { get; }
        public string PlayerSlotId { get; }
        public GameObject ActorRoot { get; }
        public string SourceScene { get; }
        public string SourceContent { get; }
        public string Source { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerActorId) && ActorRoot != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
