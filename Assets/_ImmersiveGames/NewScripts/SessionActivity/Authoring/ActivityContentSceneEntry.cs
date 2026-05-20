using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [Serializable]
    public sealed class ActivityContentSceneEntry
    {
        [SerializeField] private string entryId;
        [SerializeField] private SceneKeyAsset sceneKey;
        [SerializeField] private ActivityContentRequiredness requiredness = ActivityContentRequiredness.Required;

        public string EntryId => Normalize(entryId);
        public SceneKeyAsset SceneKey => sceneKey;
        public ActivityContentRequiredness Requiredness => requiredness;
        public bool IsRequired => requiredness == ActivityContentRequiredness.Required;
        public bool HasSceneKey => sceneKey != null;

        public void ValidateOrThrow(string source, int index)
        {
            string validationSource = Normalize(source);

            if (requiredness == ActivityContentRequiredness.Unknown)
            {
                throw new InvalidOperationException($"{validationSource} content scene entry at index {index} requires explicit requiredness.");
            }

            if (requiredness == ActivityContentRequiredness.Required && sceneKey == null)
            {
                throw new InvalidOperationException($"{validationSource} content scene entry at index {index} is required but has no SceneKeyAsset.");
            }

            if (sceneKey != null && string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                throw new InvalidOperationException($"{validationSource} content scene entry at index {index} references SceneKeyAsset '{sceneKey.name}' with empty SceneName.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
