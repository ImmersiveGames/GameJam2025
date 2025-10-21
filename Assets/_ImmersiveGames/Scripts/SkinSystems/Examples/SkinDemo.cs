using _ImmersiveGames.Scripts.SkinSystems.Configurable;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Examples
{
    public class SkinDemo : MonoBehaviour
    {
        [Header("Skin Components")]
        [SerializeField] private RingActivationSkin ringSkin;
        [SerializeField] private RandomTransformSkin transformSkin;
        [SerializeField] private GroupedMaterialSkin materialSkin;

        [Header("Material Group Controls")]
        [SerializeField] private KeyCode randomizeAllGroupsKey = KeyCode.V;
        [SerializeField] private KeyCode resetAllMaterialsKey = KeyCode.B;
        [SerializeField] private KeyCode cycleGroupMaterialsKey = KeyCode.N;
        [SerializeField] private KeyCode testDiversityKey = KeyCode.M;

        [Header("Global Controls")]
        [SerializeField] private KeyCode randomizeAllKey = KeyCode.Space;
        [SerializeField] private KeyCode resetAllKey = KeyCode.Backspace;

        [Header("Demo Settings")]
        [SerializeField] private bool showDebugMessages = true;
        [SerializeField] private bool logOnStart = true;

        private int _currentMaterialIndex = 0;

        private void Start()
        {
            if (logOnStart)
            {
                LogAllStates();
                DebugUtility.LogVerbose<SkinDemo>("🎮 SkinDemo Started - Check controls in Inspector");
                DebugUtility.LogVerbose<SkinDemo>("SPACE: Randomize All | BACKSPACE: Reset All");
                DebugUtility.LogVerbose<SkinDemo>("V: Randomize Material Groups | B: Reset Materials | N: Cycle Group Materials | M: Test Diversity");
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            HandleRingInput();
            HandleTransformInput();
            HandleMaterialInput();
            HandleGlobalInput();
        }

        #region Input Handlers
        private void HandleRingInput()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ringSkin?.ToggleRing();
                if (showDebugMessages) LogRingState();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                ringSkin?.RandomizeRing();
                if (showDebugMessages) LogRingState();
            }
        }

        private void HandleTransformInput()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                transformSkin?.RandomizeTransform();
                if (showDebugMessages) LogTransformState();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                transformSkin?.ResetToOriginalTransform();
                if (showDebugMessages) DebugUtility.LogVerbose<SkinDemo>("🔄 Transform reset to original");
            }
        }

        private void HandleMaterialInput()
        {
            if (Input.GetKeyDown(randomizeAllGroupsKey))
            {
                materialSkin?.RandomizeAllGroups();
                if (showDebugMessages) 
                {
                    LogMaterialState();
                    LogMaterialDiversity();
                }
            }

            if (Input.GetKeyDown(resetAllMaterialsKey))
            {
                materialSkin?.ResetAllMaterials();
                if (showDebugMessages) DebugUtility.LogVerbose<SkinDemo>("🎨 All materials reset to original");
            }

            if (Input.GetKeyDown(cycleGroupMaterialsKey))
            {
                CycleGroupMaterials();
            }

            if (Input.GetKeyDown(testDiversityKey))
            {
                TestMaterialDiversity();
            }
        }

        private void HandleGlobalInput()
        {
            if (Input.GetKeyDown(randomizeAllKey))
            {
                RandomizeAll();
            }

            if (Input.GetKeyDown(resetAllKey))
            {
                ResetAll();
            }
        }
        #endregion

        #region Material Group Controls
        private void CycleGroupMaterials()
        {
            if (materialSkin == null) return;

            _currentMaterialIndex++;
            materialSkin.ApplyMaterialProgression(_currentMaterialIndex);
        
            if (showDebugMessages)
            {
                DebugUtility.LogVerbose<SkinDemo>($"🎨 Applied material progression index: {_currentMaterialIndex}");
                LogMaterialState();
            }
        }

        private void TestMaterialDiversity()
        {
            if (materialSkin == null) return;

            // Testa várias randomizações para mostrar a diversidade
            DebugUtility.LogVerbose<SkinDemo>("🧪 Testing Material Diversity (3 randomizations):");
        
            for (int i = 0; i < 3; i++)
            {
                materialSkin.RandomizeAllGroups();
                var state = materialSkin.GetGroupedMaterialState();
                DebugUtility.LogVerbose<SkinDemo>($"  Run {i + 1}: {state}");
            
                foreach (var groupState in state.GroupStates)
                {
                    DebugUtility.LogVerbose<SkinDemo>($"    - {groupState.Group.GroupName}: {groupState.UniqueMaterialsCount} unique materials");
                }
            }
        }

        private void LogMaterialDiversity()
        {
            if (materialSkin == null) return;

            var state = materialSkin.GetGroupedMaterialState();
            DebugUtility.LogVerbose<SkinDemo>($"🎲 Material Diversity: {state}");
        
            foreach (var groupState in state.GroupStates)
            {
                if (groupState.UniqueMaterialsCount > 1)
                {
                    DebugUtility.LogVerbose<SkinDemo>($"   - {groupState.Group.GroupName}: {groupState.UniqueMaterialsCount} different materials!");
                }
            }
        }

        [ContextMenu("Randomize Material Groups")]
        public void UIRandomizeMaterialGroups()
        {
            materialSkin?.RandomizeAllGroups();
            LogMaterialState();
            LogMaterialDiversity();
        }

        [ContextMenu("Cycle Group Materials")]
        public void UICycleGroupMaterials()
        {
            CycleGroupMaterials();
        }

        [ContextMenu("Reset All Materials")]
        public void UIResetAllMaterials()
        {
            materialSkin?.ResetAllMaterials();
            DebugUtility.LogVerbose<SkinDemo>("🎨 All materials reset to original");
        }

        [ContextMenu("Test Material Diversity")]
        public void UITestMaterialDiversity()
        {
            TestMaterialDiversity();
        }
        #endregion

        #region Global Controls
        [ContextMenu("🎲 Randomize All")]
        public void RandomizeAll()
        {
            ringSkin?.RandomizeRing();
            transformSkin?.RandomizeTransform();
            materialSkin?.RandomizeAllGroups();
        
            if (showDebugMessages)
            {
                DebugUtility.LogVerbose<SkinDemo>("🎲 ALL SYSTEMS RANDOMIZED");
                LogAllStates();
                LogMaterialDiversity();
            }
        }

        [ContextMenu("🔄 Reset All")]
        public void ResetAll()
        {
            ringSkin?.SetRingVisible(false);
            transformSkin?.ResetToOriginalTransform();
            materialSkin?.ResetAllMaterials();
        
            _currentMaterialIndex = 0;
        
            if (showDebugMessages)
            {
                DebugUtility.LogVerbose<SkinDemo>("🔄 ALL SYSTEMS RESET");
                LogAllStates();
            }
        }
        #endregion

        #region Logging Methods
        private void LogRingState()
        {
            if (ringSkin != null)
            {
                var state = ringSkin.GetRingState();
                DebugUtility.LogVerbose<SkinDemo>($"🎯 Ring: {state}");
            }
        }

        private void LogTransformState()
        {
            if (transformSkin != null)
            {
                var state = transformSkin.GetCurrentTransformState();
                DebugUtility.LogVerbose<SkinDemo>($"🔄 Transform: {state}");
            }
        }

        private void LogMaterialState()
        {
            if (materialSkin != null)
            {
                var state = materialSkin.GetGroupedMaterialState();
                DebugUtility.LogVerbose<SkinDemo>($"🎨 Materials: {state}");
            }
        }

        private void LogAllStates()
        {
            LogRingState();
            LogTransformState();
            LogMaterialState();
        }
        #endregion
    }
}