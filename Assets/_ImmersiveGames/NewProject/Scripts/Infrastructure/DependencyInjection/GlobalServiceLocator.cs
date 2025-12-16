using System;

namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Acesso estático ao provider global para facilitar integração inicial.
    /// Evitar usar em gameplay permanente; preferir injeção explícita em serviços.
    /// </summary>
    public static class GlobalServiceLocator
    {
        public static ScopedServiceProvider Root { get; private set; }

        public static void Initialize(ScopedServiceProvider provider)
        {
            Root = provider ?? throw new ArgumentNullException(nameof(provider));
        }
    }
}
