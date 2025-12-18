using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Participante dummy de soft reset para o escopo de jogadores.
    /// Apenas emite logs verbosos para validar a coleta e a execução assíncrona.
    /// </summary>
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        public ResetScope Scope => ResetScope.Players;

        public int Order => 0;

        public async Task ResetAsync(ResetContext context)
        {
            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetAsync START (context={context})");

            await Task.Yield();

            DebugUtility.LogVerbose(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] ResetAsync END (context={context})");
        }
    }
}
