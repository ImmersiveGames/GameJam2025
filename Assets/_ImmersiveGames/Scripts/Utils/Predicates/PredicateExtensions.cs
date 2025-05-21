namespace _ImmersiveGames.Scripts.Utils.Predicates
{
    /// <summary>
    /// Métodos de extensão para facilitar a composição de predicados.
    /// </summary>
    public static class PredicateExtensions
    {
        /// <summary>
        /// Combina dois predicados com uma operação AND.
        /// </summary>
        /// <param name="left">O primeiro predicado.</param>
        /// <param name="right">O segundo predicado.</param>
        /// <returns>Um predicado And que é verdadeiro se ambos os predicados forem verdadeiros.</returns>
        public static IPredicate And(this IPredicate left, IPredicate right)
        {
            return new And(left, right);
        }

        /// <summary>
        /// Combina dois predicados com uma operação OR.
        /// </summary>
        /// <param name="left">O primeiro predicado.</param>
        /// <param name="right">O segundo predicado.</param>
        /// <returns>Um predicado Or que é verdadeiro se pelo menos um dos predicados for verdadeiro.</returns>
        public static IPredicate Or(this IPredicate left, IPredicate right)
        {
            return new Or(left, right);
        }

        /// <summary>
        /// Nega um predicado.
        /// </summary>
        /// <param name="predicate">O predicado a ser negado.</param>
        /// <returns>Um predicado Not que retorna o oposto do predicado original.</returns>
        public static IPredicate Not(this IPredicate predicate)
        {
            return new Not(predicate);
        }
    }
}