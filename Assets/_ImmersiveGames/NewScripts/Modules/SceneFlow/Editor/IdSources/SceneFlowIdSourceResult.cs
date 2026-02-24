using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Resultado normalizado de coleta de IDs para drawers.
    /// </summary>
    public readonly struct SceneFlowIdSourceResult
    {
        public SceneFlowIdSourceResult(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues)
        {
            Values = values;
            DuplicateValues = duplicateValues;
        }

        public IReadOnlyList<string> Values { get; }
        public IReadOnlyList<string> DuplicateValues { get; }
    }
}
