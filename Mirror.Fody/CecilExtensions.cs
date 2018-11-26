using System;
using Mono.Cecil;

namespace Mirror.Fody
{
    internal static class CecilExtensions
    {
        public static bool IsStatic(this TypeDefinition type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.IsAbstract && type.IsSealed;
        }
    }
}