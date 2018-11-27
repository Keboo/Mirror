using System;
using Fody;
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

        try
        {
            var assemblyNames = new HashSet<string>();
            foreach (TypeDefinition type in ModuleDefinition.GetAllTypes())
            {
                if (type.TryGetMirrorTargetName(out string mirroredType))
                {
                    TypeDefinition mirrorDefinition = FindMirroredType(mirroredType, type);
                    if (mirrorDefinition != null)
                    {
                        FieldDefinition instanceField = null;
                        if (!type.IsStatic())
                        {
                            instanceField = new FieldDefinition(GetInstanceFieldName(type), FieldAttributes.Public, 
                                ModuleDefinition.ImportReference(mirrorDefinition));
                            type.Fields.Add(instanceField);

                            foreach (MethodDefinition ctor in type.GetConstructors())
                            {
                                MethodDefinition mirroredCtor = GetMirroredMethod(mirrorDefinition, ctor);

                                if (mirroredCtor == null)
                                {
                                    LogError($"Could not find matching constructor in {mirrorDefinition.FullName}");
                                    continue;
                                }
                                
                                ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
                                ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(mirroredCtor)));
                                ctor.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Stfld, instanceField));
                            }
                        }
                        
                        foreach (MethodDefinition externMethod in type.Methods.Where(x => x.RVA == 0))
                        {
                            MethodDefinition mirroredMethod = GetMirroredMethod(mirrorDefinition, externMethod);
                            if (mirroredMethod != null)
                            {
                                externMethod.Body = BuildInvocation(ModuleDefinition.ImportReference(mirroredMethod), externMethod, instanceField);
                                
                                assemblyNames.Add(mirroredMethod.Module.Assembly.Name.Name);
                            }
                            else
                            {
                                LogError($"Could not find mirror method {type.FullName}.{externMethod.GetMirrorMethodName()}");
                            }
                        }
                    }
                    else
                    {
                        LogError($"Could not find mirrored type {mirroredType}");
                    }
                }
            }

            TypeDefinition ignoreChecksAttribute = GetIgnoresAccessChecksToAttribute();
            MethodReference attributeCtor = ignoreChecksAttribute.GetConstructors()
                .Single(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.FullName == typeof(string).FullName);

            foreach (var assembly in assemblyNames)
            {
                byte[] blob = GetAttributeBlob(assembly).ToArray();
                ModuleDefinition.Assembly.CustomAttributes.Add(new CustomAttribute(attributeCtor, blob));
            }
        }
        catch (Exception e)
        {
            LogError(e.ToString());
        }
    }

    private static IEnumerable<byte> GetAttributeBlob(string data)
    {
        //ECMA-335 11.23.3
        //http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf

        //Prolog
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

        //Named arguments - none
        yield return 0x00;
        yield return 0x00;
    }

    private TypeDefinition GetIgnoresAccessChecksToAttribute()
    {
        TypeDefinition attribute = ModuleDefinition.GetType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute");
        if (attribute != null)
        {
            return attribute;
        }

        attribute = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, Imports.System.Attribute);

        var assemblyNameBackingField = new FieldDefinition("<AssemblyName>k__BackingField", FieldAttributes.Private | FieldAttributes.InitOnly,
            Imports.System.String);
        attribute.Fields.Add(assemblyNameBackingField);

        var ctor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            Imports.System.Void);
        attribute.Methods.Add(ctor);
        var assemblyNameParameter = new ParameterDefinition("assemblyName", ParameterAttributes.None, Imports.System.String);
        ctor.Parameters.Add(assemblyNameParameter);

        ILProcessor processor = ctor.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Call, Imports.System.AttributeCtor);
        processor.Emit(OpCodes.Nop);
        processor.Emit(OpCodes.Nop);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldarg_1);
        processor.Emit(OpCodes.Stfld, assemblyNameBackingField);
        processor.Emit(OpCodes.Ret);

        var assemblyNameProperty =
            new PropertyDefinition("AssemblyName", PropertyAttributes.None, Imports.System.String);
        var getMethod = new MethodDefinition("get_AssemblyName",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, Imports.System.String);
        attribute.Methods.Add(getMethod);
        assemblyNameProperty.GetMethod = getMethod;
        processor = assemblyNameProperty.GetMethod.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, assemblyNameBackingField);
        processor.Emit(OpCodes.Ret);

        attribute.Properties.Add(assemblyNameProperty);

        ModuleDefinition.Types.Add(attribute);

        return attribute;
    }

    private TypeDefinition FindMirroredType(string mirroredTypeName, TypeDefinition externType)
    {
        foreach (var assemblyReference in ModuleDefinition.AssemblyReferences)
        {
            AssemblyDefinition assembly = ResolveAssembly(assemblyReference.Name);

            TypeDefinition type = assembly?.MainModule?.GetAllTypes().FirstOrDefault(MatchesExternType);
            if (type != null)
            {
                return type;
            }
        }
        return null;

        bool MatchesExternType(TypeDefinition type)
        {
            if (type.FullName != mirroredTypeName)
            {
                return false;
            }

            if (type.IsStatic() != externType.IsStatic())
            {
                return false;
            }

            return true;
        }
    }

    private static MethodDefinition GetMirroredMethod(TypeDefinition mirroredType, MethodDefinition externMethod)
    {
        string methodName = externMethod.GetMirrorMethodName();

        foreach (MethodDefinition method in mirroredType.Methods)
        {
            if (method.Name != methodName)
            {
                continue;
            }

            if (method.IsStatic != externMethod.IsStatic)
            {
                continue;
            }

            if (method.Parameters.Count != externMethod.Parameters.Count)
            {
                continue;
            }

            if (method.ReturnType.FullName != externMethod.ReturnType.FullName)
            {
                if (externMethod.ReturnType.Resolve()?.TryGetMirrorTargetName(out _) != true)
                {
                    continue;
                }
            }

            //TODO: check return type and parameter types
            return method;
        }

        return null;
    }

    private static MethodBody BuildInvocation(MethodReference mirroredMethod, MethodDefinition externMethod, 
        FieldDefinition instanceField)
    {
        var body = new MethodBody(externMethod);

        ILProcessor processor = body.GetILProcessor();

        FieldDefinition setField = null;
        if (mirroredMethod.ReturnType.FullName != externMethod.ReturnType.FullName)
        {
            var resolvedReturnType = externMethod.ReturnType.Resolve();
            if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
            {
                MethodDefinition defaultCtor = resolvedReturnType.GetConstructors().Single(c => c.Parameters.Count == 0);
                setField = resolvedReturnType.Fields.Single(x => x.Name == GetInstanceFieldName(resolvedReturnType));

                //Assume return type is another mirror class....
                processor.Emit(OpCodes.Newobj, defaultCtor);
                processor.Emit(OpCodes.Dup);
            }
        }

        if (externMethod.IsStatic)
        {
            processor.Emit(OpCodes.Call, mirroredMethod);
        }
        else
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, instanceField);
            processor.Emit(OpCodes.Callvirt, mirroredMethod);
        }

        if (setField != null)
        {
            processor.Emit(OpCodes.Stfld, setField);
        }

        processor.Emit(OpCodes.Ret);

        return body;
    }

    private static string GetInstanceFieldName(TypeReference externType)
    {
        return $"<{externType.Name}>k__InstanceField";
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Runtime";
        yield return "System.Core";
        yield return "netstandard";
    }
}
