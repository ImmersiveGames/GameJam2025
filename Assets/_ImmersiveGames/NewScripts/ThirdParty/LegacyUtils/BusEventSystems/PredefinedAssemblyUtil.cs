using System;
using System.Collections.Generic;
using System.Reflection;
namespace _ImmersiveGames.Scripts.Utils.BusEventSystems {
    /// <summary>
    /// A utility class, PredefinedAssemblyUtil, provides methods to interact with predefined assemblies.
    /// It allows you to get all types in the current AppDomain that implement from a specific Interface type.
    /// For more details, <see href="https://docs.unity3d.com/2023.3/Documentation/Manual/ScriptCompileOrderFolders.html">visit Unity Documentation</see>
    /// </summary>
    public static class PredefinedAssemblyUtil {
        /// <summary>
        /// Enum that defines the specific predefined types of assemblies for navigation.
        /// </summary>    
        private enum AssemblyType {
            AssemblyCSharp,
            AssemblyCSharpEditor,
            AssemblyCSharpEditorFirstPass,
            AssemblyCSharpFirstPass
        }

        /// <summary>
        /// Maps the assembly name to the corresponding AssemblyType.
        /// </summary>
        /// <param name="assemblyName">ActorName of the assembly.</param>
        /// <returns>AssemblyType corresponding to the assembly name, null if no match.</returns>
        private static AssemblyType? GetAssemblyType(string assemblyName) {
            return assemblyName switch {
                "Assembly-CSharp" => AssemblyType.AssemblyCSharp,
                "Assembly-CSharp-Editor" => AssemblyType.AssemblyCSharpEditor,
                "Assembly-CSharp-Editor-firstpass" => AssemblyType.AssemblyCSharpEditorFirstPass,
                "Assembly-CSharp-firstpass" => AssemblyType.AssemblyCSharpFirstPass,
                _ => null
            };
        }

        /// <summary>
        /// Method looks through a given assembly and adds types that fulfill a certain interface to the provided collection.
        /// </summary>
        /// <param name="assemblyTypes">Array of Type objects representing all the types in the assembly.</param>
        /// <param name="interfaceType">Type representing the interface to be checked against.</param>
        /// <param name="results">Collection of types where a result should be added.</param>
        private static void AddTypesFromAssembly(Type[] assemblyTypes, Type interfaceType, ICollection<Type> results)
        {
            if (assemblyTypes == null) return;
            foreach (var type in assemblyTypes)
            {
                if (type != interfaceType && interfaceType.IsAssignableFrom(type)) {
                    results.Add(type);
                }
            }
        }
    
        /// <summary>
        /// Gets all Types from all assemblies in the current AppDomain that implement the provided interface type.
        /// </summary>
        /// <param name="interfaceType">Interface type to get all the Types for.</param>
        /// <returns>List of Types implementing the provided interface type.</returns>    
        public static List<Type> GetTypes(Type interfaceType) {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
            var assemblyTypes = new Dictionary<AssemblyType, Type[]>();
            var types = new List<Type>();
            foreach (var t in assemblies)
            {
                AssemblyType? assemblyType = GetAssemblyType(t.GetName().Name);
                if (assemblyType != null) {
                    assemblyTypes.Add((AssemblyType) assemblyType, t.GetTypes());
                }
            }
        
            assemblyTypes.TryGetValue(AssemblyType.AssemblyCSharp, out Type[] assemblyCSharpTypes);
            AddTypesFromAssembly(assemblyCSharpTypes, interfaceType, types);

            assemblyTypes.TryGetValue(AssemblyType.AssemblyCSharpFirstPass, out Type[] assemblyCSharpFirstPassTypes);
            AddTypesFromAssembly(assemblyCSharpFirstPassTypes, interfaceType, types);
        
            return types;
        }
    }
}