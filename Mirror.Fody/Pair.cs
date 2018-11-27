using Mono.Cecil;

namespace Mirror.Fody
{
    public class TypeMap
    {
        public TypeMap(TypeDefinition externType, TypeDefinition mirrorType)
        {
            ExternType = externType;
            MirrorType = mirrorType;
        }


        public TypeDefinition ExternType { get; }
        public TypeDefinition MirrorType { get; }
    }
}