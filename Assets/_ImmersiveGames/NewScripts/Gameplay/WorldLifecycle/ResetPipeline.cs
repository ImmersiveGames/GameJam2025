using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.WorldLifecycle
{
    public class ResetPipeline : MonoBehaviour
    {
        public async Task RunAsync(string contextSignature)
        {
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] RunAsync signature={contextSignature} START");
            await ClearStateAsync(contextSignature);
            await ResetSystemsAsync(contextSignature);
            await SpawnEssentialsAsync(contextSignature);
            await ValidateActorRegistryAsync(contextSignature);
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] RunAsync signature={contextSignature} DONE");
        }

        private Task ClearStateAsync(string contextSignature)
        {
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] ClearState signature={contextSignature}");
            // TODO: limpar estados singletons, registries, caches.
            return Task.CompletedTask;
        }

        private Task ResetSystemsAsync(string contextSignature)
        {
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] ResetSystems signature={contextSignature}");
            // TODO: reinicializar sistemas (AI, physics overrides, pools).
            return Task.CompletedTask;
        }

        private Task SpawnEssentialsAsync(string contextSignature)
        {
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] SpawnEssentials signature={contextSignature}");
            // Implementação mínima: espera que Scene já tenha instanciado GameObjects tagged com Player/Eater.
            // Em integrações reais, isto chamaria o spawn manager.
            return Task.CompletedTask;
        }

        private Task ValidateActorRegistryAsync(string contextSignature)
        {
            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] ValidateActorRegistry signature={contextSignature}");

            // Validação simples por tags - garante Player + Eater presentes.
            GameObject player = null;
            GameObject eater = null;

            try
            {
                player = GameObject.FindWithTag("Player");
            }
            catch { /* tag pode não existir em alguns projetos */ }

            try
            {
                eater = GameObject.FindWithTag("Eater");
            }
            catch { /* tag pode não existir */ }

            if (player == null || eater == null)
            {
                DebugUtility.LogError<ResetPipeline>($"[ResetPipeline] Falha de validação: Player found={(player!=null)} Eater found={(eater!=null)} signature={contextSignature}");
                throw new InvalidOperationException("SpawnEssentials inválido: Player e/ou Eater ausentes após reset.");
            }

            DebugUtility.LogVerbose<ResetPipeline>($"[ResetPipeline] ValidateActorRegistry OK signature={contextSignature}");
            return Task.CompletedTask;
        }
    }
}

// Adicionar/alterar:
// - Implementar etapas: ClearState -> ResetSystems -> SpawnEssentials -> ValidateActorRegistry
// - SpawnEssentials deve garantir ActorRegistry contains Player + Eater (count==2) antes de completar.
// - Expor hooks para testes (invariants checks).
