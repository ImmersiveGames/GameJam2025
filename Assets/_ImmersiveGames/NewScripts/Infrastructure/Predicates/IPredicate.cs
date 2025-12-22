namespace _ImmersiveGames.NewScripts.Infrastructure.Predicates
{
    /// <summary>
    /// Contrato simples para predicados avaliáveis utilizados pela FSM.
    /// </summary>
    public interface IPredicate
    {
        /// <summary>
        /// Avalia a condição encapsulada e retorna se ela é verdadeira.
        /// </summary>
        /// <returns>Verdadeiro quando a condição é satisfeita.</returns>
        bool Evaluate();
    }
}
