using System;
using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public readonly struct ActorsMaterializationExecutionCycle
    {
        public ActorsMaterializationExecutionCycle(int phaseLocalEntrySequence, string entrySignature)
        {
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            EntrySignature = entrySignature.TrimToEmpty();
        }

        public int PhaseLocalEntrySequence { get; }
        public string EntrySignature { get; }
        public bool IsValid => PhaseLocalEntrySequence > 0 && !string.IsNullOrWhiteSpace(EntrySignature);

        public string ToStampKey()
        {
            if (!IsValid)
            {
                return string.Empty;
            }

            return $"entrySeq:{PhaseLocalEntrySequence}|entrySignature:{EntrySignature}";
        }

        public static ActorsMaterializationExecutionCycle CreateOrFail(int phaseLocalEntrySequence, string entrySignature, string source)
        {
            var cycle = new ActorsMaterializationExecutionCycle(phaseLocalEntrySequence, entrySignature);
            if (!cycle.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Invalid materialization execution cycle. phaseLocalEntrySequence='{phaseLocalEntrySequence}' entrySignature='{(entrySignature.TrimToOrDefault("<none>"))}' source='{(source.TrimToOrDefault("<none>"))}'.");
            }

            return cycle;
        }
    }
}
