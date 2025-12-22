namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço para operar a GameLoopStateMachine em runtime puro C# (sem MonoBehaviour).
    /// </summary>
    public interface IGameLoopService
    {
        /// <summary>
        /// Inicializa a FSM concreta do GameLoop.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Avança a FSM conforme o delta de tempo informado.
        /// </summary>
        /// <param name="dt">Delta time da atualização (pode ser ignorado pela FSM).</param>
        void Tick(float dt);

        /// <summary>
        /// Marca intenção de iniciar o loop de jogo (Menu → Playing).
        /// </summary>
        void RequestStart();

        /// <summary>
        /// Marca intenção de pausar o loop.
        /// </summary>
        void RequestPause();

        /// <summary>
        /// Marca intenção de retomar o loop.
        /// </summary>
        void RequestResume();

        /// <summary>
        /// Marca intenção de resetar o loop para o estado inicial.
        /// </summary>
        void RequestReset();

        /// <summary>
        /// Libera recursos e listeners associados ao serviço.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Nome amigável do estado atual (opcional).
        /// </summary>
        string CurrentStateName { get; }
    }
}
