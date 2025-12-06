using System.Collections.Generic;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Tests
{
    /// <summary>
    /// Tester para validar o SimpleSceneTransitionPlanner.
    /// Não carrega nem descarrega cenas: apenas calcula e loga o contexto.
    /// </summary>
    public class SceneTransitionPlannerTester : MonoBehaviour
    {
        [Header("Cenas alvo após a transição")]
        [SerializeField] private string[] targetScenes;
        [SerializeField] private string explicitTargetActiveScene = "";
        [SerializeField] private bool useFade = true;

        private ISceneTransitionPlanner _planner;

        private void Awake()
        {
            _planner = new SimpleSceneTransitionPlanner();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[SceneTransitionPlannerTester] Tecla P pressionada - calculando contexto de transição.");

                // Snapshot do estado atual
                var state = SceneState.FromSceneManager();

                // Constrói o contexto
                var context = _planner.BuildContext(
                    state,
                    new List<string>(targetScenes),
                    explicitTargetActiveScene,
                    useFade);

                Debug.Log($"[SceneTransitionPlannerTester] Resultado: {context}");
            }
        }
    }
}