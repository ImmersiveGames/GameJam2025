using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.SelectorGeneric;

namespace _ImmersiveGames.Scripts.SkinSystems.Handlers
{
    public static class RootHandlerFactory
    {
        private static readonly Dictionary<ObjectRootType, Type> _typeMap = new()
        {
            { ObjectRootType.ModelRoot, typeof(ModelRoot) },
            { ObjectRootType.FxRoot, typeof(FxRoot) },
            { ObjectRootType.CanvasRoot, typeof(CanvasRoot) },
        };

        public static IRootHandler Create(ObjectRootType rootType)
        {
            var type = _typeMap[rootType];
            return new GenericRootHandler(rootType.ToString(), type);
        }
    }
}