﻿using Fody;
using Mirror.Fody;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class ModuleWeaver : BaseModuleWeaver
{
    private Imports Imports { get; set; }

    public override void Execute()
    {
        Imports = new Imports(FindType, ModuleDefinition, this);
        

        var assemblyNames = new HashSet<string>();
        foreach (TypeDefinition type in GetMirrorTypes())
        {
            var mirrorAttribute =
                type.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "Mirror.MirrorAttribute");
            if (mirrorAttribute != null)
            {
                LogWarning($"Found {type.FullName}");
                var mirroredType = mirrorAttribute.ConstructorArguments[0].Value.ToString();
                TypeDefinition mirrorDefinition = FindMirroredType(mirroredType);
                if (mirrorDefinition != null)
                {
                    mirrorDefinition = ModuleDefinition.ImportReference(mirrorDefinition).Resolve();
                    foreach (MethodDefinition externMethod in type.Methods.Where(x => x.RVA == 0))
                    {
                        MethodDefinition mirroredMethod = mirrorDefinition.Methods.FirstOrDefault(x => x.Name == externMethod.Name);
                        if (mirroredMethod != null)
                        {
                            externMethod.Body = new MethodBody(externMethod);
                            
                            LogWarning("Overriding method");
                            ILProcessor processor = externMethod.Body.GetILProcessor();
                            processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(mirroredMethod));
                            processor.Emit(OpCodes.Ret);

                            assemblyNames.Add(mirroredMethod.Module.Assembly.Name.Name);
                        }
                        else
                        {
                            LogWarning($"Could not find mirror method {externMethod.Name}");
                        }

                        LogWarning($"{externMethod.Name} {externMethod.RVA}");
                    }
                }
                else
                {
                    LogWarning($"Could not find mirrored type {mirroredType}");
                }
            }
        }

        TypeDefinition ignoreChecksAttribute = CreateIgnoresAccessChecksToAttribute();
        MethodReference attributeCtor = ignoreChecksAttribute.GetConstructors().Single();
        //foreach (var a in ModuleDefinition.Assembly.CustomAttributes)
        //{
        //
        //    LogWarning($"Foo {a.AttributeType.FullName}");
        //}

        foreach (var assembly in assemblyNames)
        {
            LogWarning($"Allowing access to {assembly}");
            byte[] blob = GetAttributeBlob(assembly).ToArray();
            ModuleDefinition.Assembly.CustomAttributes.Add(new CustomAttribute(attributeCtor, blob));
        }
    }

    private static IEnumerable<byte> GetAttributeBlob(string data)
    {
        yield return 0x01;
        yield return 0x00;

        //Based on Mono.Cecil ByteBuffer.WriteCompressedUInt32 
        //https://github.com/jbevain/cecil/blob/eea822cad4b6f320c9e1da642fcbc0c129b00a6e/Mono.Cecil.PE/ByteBuffer.cs#L240
        if (data.Length < 0x80)
        {
            yield return (byte)data.Length;
        }
        else if (data.Length < 0x4000)
        {
            yield return ((byte)(0x80 | (data.Length >> 8)));
            yield return ((byte)(data.Length & 0xff));
        }
        else
        {
            yield return ((byte)((data.Length >> 24) | 0xc0));
            yield return ((byte)((data.Length >> 16) & 0xff));
            yield return ((byte)((data.Length >> 8) & 0xff));
            yield return ((byte)(data.Length & 0xff));
        }

        //Based on: DefineCustomAttributeFromBlob()
        //https://github.com/jbevain/cecil/blob/master/Test/Mono.Cecil.Tests/CustomAttributesTests.cs#L469
        foreach (var @byte in Encoding.UTF8.GetBytes(data))
        {
            yield return @byte;
        }

        yield return 0x00;
        yield return 0x00;
    }

    private TypeDefinition CreateIgnoresAccessChecksToAttribute()
    {
        TypeDefinition attribute = ModuleDefinition.GetType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute");
        if (attribute != null)
        {
            return attribute;
        }

        attribute = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute", TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
        attribute.BaseType = FindType("System.Attribute");

        return attribute;
    }

    private TypeDefinition FindMirroredType(string typeName)
    {
        foreach (var assemblyReference in ModuleDefinition.AssemblyReferences)
        {
            AssemblyDefinition assembly = ResolveAssembly(assemblyReference.Name);
            LogWarning($"{assemblyReference.Name} -> {assembly?.FullName}");

            TypeDefinition type = assembly?.MainModule?.GetAllTypes().FirstOrDefault(t => t.FullName == typeName);
            if (type != null)
            {
                return type;
            }
        }
        return null;
    }

    private IEnumerable<TypeDefinition> GetMirrorTypes()
    {
        return from typeDefinition in ModuleDefinition.GetAllTypes()
               from customAttribute in typeDefinition.CustomAttributes 
               where customAttribute.AttributeType.FullName == "Mirror.MirrorAttribute" 
               select typeDefinition;
        //return ModuleDefinition.GetAllTypes().Where(type =>
        //    type.CustomAttributes.Any(a => a.AttributeType.FullName == "Mirror.MirrorAttribute"));
    }
    
    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield break;
        //yield return "mscorlib";
        //yield return "System";
        //yield return "System.Runtime";
        //yield return "System.Core";
        //yield return "netstandard";
        //yield return "System.Collections";
        //yield return "System.ObjectModel";
        //yield return "System.Threading";
    }
}
