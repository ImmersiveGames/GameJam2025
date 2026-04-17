using System;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Content.Definitions.Levels.Config
{
    [Serializable]
    public sealed partial class SceneBuildIndexRef : IEquatable<SceneBuildIndexRef>
    {
        [SerializeField] private int buildIndex = -1;
        [SerializeField] private string sceneName = string.Empty;

        public int BuildIndex => buildIndex;
        public string SceneName => sceneName ?? string.Empty;
        public bool IsValid => buildIndex >= 0;

        public void SyncFromEditorAsset()
        {
            SyncFromEditorAssetEditor();
        }

        partial void SyncFromEditorAssetEditor();

        public bool Equals(SceneBuildIndexRef other)
        {
            return other != null && buildIndex == other.buildIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneBuildIndexRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return buildIndex;
        }
    }
}

