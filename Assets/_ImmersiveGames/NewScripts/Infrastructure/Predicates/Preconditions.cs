using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.Predicates
{
    /// <summary>
    /// Utilitário para validação de argumentos e estados (portado do legado, mantendo API mínima).
    /// </summary>
    public static class Preconditions
    {
        public static T CheckNotNull<T>(T reference, string message = "Argument cannot be null.")
        {
            if (reference == null)
            {
                throw new ArgumentNullException(message);
            }
            return reference;
        }

        public static void CheckState(bool expression, string message = "Invalid state.")
        {
            if (!expression)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            if (!expression)
            {
                throw new InvalidOperationException(string.Format(messageTemplate, messageArgs));
            }
        }
    }
}
