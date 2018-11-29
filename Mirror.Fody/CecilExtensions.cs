using System;
using System.Linq;
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

        public static bool IsExtern(this MethodDefinition method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return method.RVA == 0;
        }

        public static bool TryGetMirrorTargetName(this ICustomAttributeProvider customAttributeProvider, out string targetName)
        {
            if (customAttributeProvider == null) throw new ArgumentNullException(nameof(customAttributeProvider));

            var mirrorAttribute =
                customAttributeProvider.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "Mirror.MirrorAttribute");
            
            if (mirrorAttribute == null)
            {
                targetName = null;
                return false;
            }

            targetName = mirrorAttribute.ConstructorArguments.Count > 0
                ? mirrorAttribute.ConstructorArguments[0].Value.ToString()
                : "";
            
            return true;
        }

        public static string GetMirrorTargetName(this IMemberDefinition type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!TryGetMirrorTargetName(type, out string targetName))
            {
                targetName = type.Name;
            }
            return targetName;
        }

        public static string GetMirrorTargetName(this MethodDefinition externMethod)
        {
            if (!externMethod.TryGetMirrorTargetName(out string methodName) || 
                string.IsNullOrWhiteSpace(methodName))
            {
                methodName = externMethod.Name;
            }

            return methodName;
        }

        public static FieldDefinition GetInstanceField(this TypeDefinition type)
        {
            string fieldName = type.GetInstanceFieldName();
            return type.Fields.FirstOrDefault(f => f.Name == fieldName);
        }

        public static string GetInstanceFieldName(this TypeReference externType)
        {
            return $"<{externType.Name}>k__InstanceField";
        }
    }
}