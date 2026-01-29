using System;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Definição configurável de um nível (fonte de verdade para LevelManager).
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "ImmersiveGames/Levels/Level Definition",
        order = 0)]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = string.Empty;
        [SerializeField] private string contentId = string.Empty;
        [SerializeField] private string contentSignature = string.Empty;
        [SerializeField] private LevelChangeOptions defaultOptions = new();
        [SerializeField] [TextArea] private string notes;

        public string LevelId => Normalize(levelId);
        public string ContentId => Normalize(contentId);
        public string ContentSignature => Normalize(contentSignature);

        public string Notes => notes;

        public LevelChangeOptions DefaultOptions => defaultOptions ?? new LevelChangeOptions();

        public LevelPlan ToPlan()
        {
            return new LevelPlan(LevelId, ContentId, ContentSignature);
        }

        public ContentSwapPlan ToContentSwapPlan()
        {
            return new ContentSwapPlan(ContentId, ContentSignature);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
