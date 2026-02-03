using System;
namespace _ImmersiveGames.NewScripts.Core.Events
{
    /// <summary>
    /// Legacy compatibility wrapper for code that expects FilteredEventBus&lt;TEvent&gt;
    /// with scope passed as object and mixed parameter order.
    /// </summary>
    public static class FilteredEventBus<TEvent>
    {
        public static void Register(EventBinding<TEvent> binding, object scope) =>
            FilteredEventBus<object, TEvent>.Register(scope, binding);

        public static void Register(object scope, EventBinding<TEvent> binding) =>
            FilteredEventBus<object, TEvent>.Register(scope, binding);

        public static void Unregister(EventBinding<TEvent> binding, object scope) =>
            FilteredEventBus<object, TEvent>.Unregister(scope, binding);

        public static void Unregister(object scope) =>
            FilteredEventBus<object, TEvent>.Clear(scope);

        public static void RaiseFiltered(TEvent evt, object scope) =>
            FilteredEventBus<object, TEvent>.Raise(scope, evt);

        public static void Clear(object scope) =>
            FilteredEventBus<object, TEvent>.Clear(scope);

        public static void ClearAll() =>
            FilteredEventBus<object, TEvent>.ClearAll();
    }
}
