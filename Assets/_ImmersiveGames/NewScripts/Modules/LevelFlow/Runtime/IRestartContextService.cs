namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Armazena o snapshot canônico do último start de gameplay para suportar restart e observabilidade.
    /// </summary>
    public interface IRestartContextService
    {
        GameplayStartSnapshot Current { get; }

        /// <summary>
        /// Registra snapshot do start de gameplay e retorna a versão persistida.
        /// </summary>
        GameplayStartSnapshot RegisterGameplayStart(GameplayStartSnapshot snapshot);

        /// <summary>
        /// Tenta ler o snapshot atual. Retorna false quando ainda não existe snapshot válido.
        /// </summary>
        bool TryGetCurrent(out GameplayStartSnapshot snapshot);

        /// <summary>
        /// Limpa o estado atual (útil em cenários de reset global/boot).
        /// </summary>
        void Clear(string reason = null);
    }
}
