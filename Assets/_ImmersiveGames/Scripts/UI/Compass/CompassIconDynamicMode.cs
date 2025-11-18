namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Define o modo de resolução de ícone para um alvo rastreável na bússola.
    /// </summary>
    public enum CompassIconDynamicMode
    {
        /// <summary>
        /// Usa o sprite estático configurado no ScriptableObject.
        /// </summary>
        Static = 0,

        /// <summary>
        /// Usa o ícone de recurso do planeta associado (PlanetResourcesSo.ResourceIcon).
        /// </summary>
        PlanetResourceIcon = 1
    }
}
