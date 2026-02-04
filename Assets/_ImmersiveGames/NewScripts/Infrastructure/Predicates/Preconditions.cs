using System;
namespace _ImmersiveGames.NewScripts.Infrastructure.Predicates
{
    /// <summary>
    /// Utilitários básicos de validação usados pela infraestrutura.
    /// </summary>
    public static class Preconditions
    {
        /// <summary>
        /// Garante que a referência não seja nula.
        /// </summary>
        /// <typeparam name="T">Tipo da referência validada.</typeparam>
        /// <param name="reference">Referência a ser validada.</param>
        /// <param name="message">Mensagem opcional para detalhar o erro.</param>
        /// <returns>A referência validada.</returns>
        /// <exception cref="ArgumentNullException">Lançada quando a referência é nula.</exception>
        public static T CheckNotNull<T>(T reference, string message = "Argument cannot be null.")
        {
            if (reference == null)
            {
                throw new ArgumentNullException(message);
            }

            return reference;
        }

        /// <summary>
        /// Garante que uma condição de estado seja verdadeira.
        /// </summary>
        /// <param name="expression">Expressão boolean a ser avaliada.</param>
        /// <param name="message">Mensagem opcional para detalhar o erro.</param>
        /// <exception cref="InvalidOperationException">Lançada quando a condição é falsa.</exception>
        public static void CheckState(bool expression, string message = "Invalid state.")
        {
            if (!expression)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Garante que uma condição de estado seja verdadeira e permite formatar a mensagem.
        /// </summary>
        /// <param name="expression">Expressão boolean a ser avaliada.</param>
        /// <param name="messageTemplate">Modelo da mensagem.</param>
        /// <param name="messageArgs">Argumentos de formatação.</param>
        /// <exception cref="InvalidOperationException">Lançada quando a condição é falsa.</exception>
        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            if (!expression)
            {
                throw new InvalidOperationException(string.Format(messageTemplate, messageArgs));
            }
        }
    }
}

