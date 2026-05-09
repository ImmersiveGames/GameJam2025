using System;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    public readonly struct ActorsMaterializationExecutionCycle
    {
        public ActorsMaterializationExecutionCycle(int phaseLocalEntrySequence, string entrySignature)
        {
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            EntrySignature = string.IsNullOrWhiteSpace(entrySignature) ? string.Empty : entrySignature.Trim();
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
                    $"[FATAL][Config][ActorsExecution] Invalid materialization execution cycle. phaseLocalEntrySequence='{phaseLocalEntrySequence}' entrySignature='{(string.IsNullOrWhiteSpace(entrySignature) ? "<none>" : entrySignature.Trim())}' source='{(string.IsNullOrWhiteSpace(source) ? "<none>" : source.Trim())}'.");
            }

            return cycle;
        }
    }
}
