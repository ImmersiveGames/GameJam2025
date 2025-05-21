using System;
namespace _ImmersiveGames.Scripts.Utils.Predicates
{
    /// <summary>
    /// Fornece métodos utilitários para validação de argumentos e estados.
    /// </summary>
    public static class Preconditions
    {
        /// <summary>
        /// Verifica se uma referência não é nula, lançando uma exceção se for.
        /// </summary>
        /// <typeparam name="T">O tipo da referência.</typeparam>
        /// <param name="reference">A referência a ser verificada.</param>
        /// <param name="message">A mensagem da exceção, se fornecida.</param>
        /// <returns>A referência se não for nula.</returns>
        /// <exception cref="ArgumentNullException">Lançada se a referência for nula.</exception>
        public static T CheckNotNull<T>(T reference, string message = "Argument cannot be null.")
        {
            if (reference == null)
                throw new ArgumentNullException(message);
            return reference;
        }

        /// <summary>
        /// Verifica se uma expressão de estado é verdadeira, lançando uma exceção se for falsa.
        /// </summary>
        /// <param name="expression">A expressão a ser avaliada.</param>
        /// <param name="message">A mensagem da exceção, se fornecida.</param>
        /// <exception cref="InvalidOperationException">Lançada se a expressão for falsa.</exception>
        public static void CheckState(bool expression, string message = "Invalid state.")
        {
            if (!expression)
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Verifica se uma expressão de estado é verdadeira, formatando uma mensagem com argumentos.
        /// </summary>
        /// <param name="expression">A expressão a ser avaliada.</param>
        /// <param name="messageTemplate">O modelo da mensagem.</param>
        /// <param name="messageArgs">Os argumentos para formatar a mensagem.</param>
        /// <exception cref="InvalidOperationException">Lançada se a expressão for falsa.</exception>
        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            if (!expression)
                throw new InvalidOperationException(string.Format(messageTemplate, messageArgs));
        }
        
    }
}