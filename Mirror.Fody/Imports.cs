using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Mirror.Fody
{
    public class Imports
    {
        public Imports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition,
            ModuleWeaver moduleWeaver)
        {
            System = new SystemImport(findType, moduleDefinition, moduleWeaver);
        }

        public SystemImport System { get; }

        public class SystemImport
        {
            public SystemImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition,
                ModuleWeaver moduleWeaver)
            {
                String = moduleDefinition.ImportReference(findType("System.String"));
                Void = moduleDefinition.ImportReference(findType("System.Void"));
                ValueType = moduleDefinition.ImportReference(findType("System.ValueType"));

                TypeDefinition attributeType = findType("System.Attribute");
                Attribute = moduleDefinition.ImportReference(attributeType);
                AttributeCtor =
                    moduleDefinition.ImportReference(attributeType.GetConstructors()
                        .Single(c => c.Parameters.Count == 0));

                TypeDefinition objectType = findType("System.Object");
                ObjectCtor = moduleDefinition.ImportReference(objectType.GetConstructors().Single());
            }

            public TypeReference ValueType { get; }

            public TypeReference String { get; }

            public TypeReference Void { get; }

            public TypeReference Attribute { get; }

            public MethodReference AttributeCtor { get; }

            public MethodReference ObjectCtor { get; }
        }
    }
}