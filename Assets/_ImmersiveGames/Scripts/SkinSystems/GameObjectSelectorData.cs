using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.SelectorGeneric;
using _ImmersiveGames.Scripts.Utils.SelectorGeneric.Strategies;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    [CreateAssetMenu(fileName = "GameObjectSelectorData", menuName = "SkinSystem/SkinData")]
    public class GameObjectSelectorData : ScriptableObject, IGameObjectSelector
    {
        [SerializeField] private string selectorName;
        [SerializeField] private ObjectRootType objectRootType = ObjectRootType.ModelRoot;

        [SerializeField] private List<GameObject> prefabs = new();
        [SerializeField] private List<bool> activationMask = new();
        [SerializeField] private List<string> prefabTags = new();

        [SerializeField] private SelectionStrategyType strategyType = SelectionStrategyType.First;
        [SerializeField] private int specificIndex;

        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private Vector3 scaleOffset = Vector3.one;

        public ObjectRootType ObjectRootType => objectRootType;
        public string SelectorName => selectorName;
        public Vector3 PositionOffset => positionOffset;
        public Quaternion RotationOffset => Quaternion.Euler(rotationOffset);
        public Vector3 ScaleOffset => scaleOffset;
        public SelectionStrategyType SelectModelStrategy => strategyType;

        public IEnumerable<(GameObject prefab, bool active, string tag)> Select(int listId)
        {
            SyncMaskWithPrefabs();

            var strategy = GetStrategy<GameObject>();
            var selectedPrefabs = strategy.Select(prefabs, listId, specificIndex);

            foreach (var prefab in selectedPrefabs)
            {
                int index = prefabs.IndexOf(prefab);
                if (index >= 0)
                {
                    bool active = activationMask[index];
                    string tag = index < prefabTags.Count ? prefabTags[index] : string.Empty;
                    yield return (prefab, active, tag);
                }
            }
        }


        private void SyncMaskWithPrefabs()
        {
            while (activationMask.Count < prefabs.Count)
                activationMask.Add(true);
            while (activationMask.Count > prefabs.Count)
                activationMask.RemoveAt(activationMask.Count - 1);
        }

        private ISelectionStrategy<T> GetStrategy<T>()
        {
            return strategyType switch
            {
                SelectionStrategyType.First => new FirstStrategy<T>(),
                SelectionStrategyType.Random => new RandomStrategy<T>(),
                SelectionStrategyType.SpecificIndex => new SpecificIndexStrategy<T>(),
                SelectionStrategyType.Next => new NextStrategy<T>(),
                SelectionStrategyType.All => new AllStrategy<T>(),
                _ => new FirstStrategy<T>()
            };
        }
    }
}
