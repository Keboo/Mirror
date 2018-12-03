using System;
using Fody;
using Mirror.Fody;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using TypeMap = Mirror.Fody.TypeMap;

public class ModuleWeaver : BaseModuleWeaver
{
    private Imports Imports { get; set; }

    public override void Execute()
    {
        Imports = new Imports(FindType, ModuleDefinition, this);

        try
        {
            TypeDefinition unusedType = CreateUnusedType();
            ModuleDefinition.Types.Add(unusedType);

            var assemblyNames = new HashSet<string>();
            var mappedTypes = new List<TypeMap>();
            foreach (TypeDefinition type in ModuleDefinition.GetAllTypes())
            {
                TypeDefinition mirrorType = ProcessMirrorType(type, unusedType);
                if (mirrorType != null)
                {
                    mappedTypes.Add(new TypeMap(type, mirrorType));
                }
            }

            foreach (TypeMap typeMap in mappedTypes)
            {
                FieldDefinition instanceField = typeMap.ExternType.GetInstanceField();

                foreach (MethodDefinition externMethod in typeMap.ExternType.Methods.Where(x => x.IsExtern() && x.Parameters.All(p => p.ParameterType.FullName != unusedType.FullName)))
                {
                    if (externMethod.IsGetter || externMethod.IsSetter)
                    {
                        PropertyDefinition externProperty = typeMap.ExternType.Properties.Single(p =>
                            p.GetMethod == externMethod || p.SetMethod == externMethod);
                        string mirrorTargetName = externProperty.GetMirrorTargetName();
                        PropertyDefinition mirroredProperty = GetMirroredProperty(typeMap.MirrorType, externMethod, mirrorTargetName);
                        if (mirroredProperty != null)
                        {
                            try
                            {
                                ForwardMethodToProperty(externMethod, mirroredProperty, instanceField, unusedType);
                                assemblyNames.Add(mirroredProperty.Module.Assembly.Name.Name);
                            }
                            catch
                            {
                                LogError($"Error in {mirroredProperty.Name} {externMethod.Name}");
                                throw;
                            }

                        }
                        else
                        {
                            FieldDefinition mirroredField = GetMirroredField(typeMap.MirrorType, externMethod,
                                externProperty.PropertyType, mirrorTargetName);
                            if (mirroredField != null)
                            {
                                ForwardMethodToField(externMethod, ModuleDefinition.ImportReference(mirroredField), instanceField, unusedType);
                                assemblyNames.Add(mirroredField.Module.Assembly.Name.Name);
                            }
                            else
                            {
                                LogError($"Could not find mirrored property or field {typeMap.MirrorType.FullName}.{mirrorTargetName}");
                            }
                        }
                    }
                    else
                    {
                        MethodDefinition mirroredMethod = GetMirroredMethod(typeMap.MirrorType, externMethod);
                        if (mirroredMethod != null)
                        {
                            ForwardMethodToMethod(ModuleDefinition.ImportReference(mirroredMethod),
                                externMethod, instanceField, unusedType);

                            assemblyNames.Add(mirroredMethod.Module.Assembly.Name.Name);
                        }
                        else
                        {
                            string parameters = string.Join(", ",
                                externMethod.Parameters.Select(p => p.ParameterType.Resolve().GetMirrorTargetName()));
                            LogError($"Could not find mirrored method {typeMap.MirrorType.FullName}.{externMethod.GetMirrorTargetName()}({parameters})");
                        }
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

    private TypeDefinition ProcessMirrorType(TypeDefinition type, TypeDefinition unusedType)
    {
        if (type.TryGetMirrorTargetName(out string mirroredType))
        {
            TypeDefinition mirrorDefinition = FindMirroredType(mirroredType, type);
            if (mirrorDefinition != null)
            {
                if (!type.IsStatic())
                {
                    var instanceField = new FieldDefinition(type.GetInstanceFieldName(), FieldAttributes.Public,
                        ModuleDefinition.ImportReference(mirrorDefinition));
                    type.Fields.Add(instanceField);

                    //foreach (MethodDefinition ctor in type.GetConstructors().Where(c => c.Parameters.All(p => p.ParameterType != unusedType)))
                    //{
                    //    MethodDefinition mirroredCtor = GetMirroredMethod(mirrorDefinition, ctor);
                    //
                    //    if (mirroredCtor == null)
                    //    {
                    //        //Try to find a parameter-less ctor
                    //        LogError($"Could not find matching constructor in {mirrorDefinition.FullName}");
                    //        continue;
                    //    }
                    //
                    //    ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
                    //    int index = 1;
                    //    foreach (var parameter in ctor.Parameters)
                    //    {
                    //        ctor.Body.Instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg, parameter));
                    //    }
                    //    ctor.Body.Instructions.Insert(index++, Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(mirroredCtor)));
                    //    ctor.Body.Instructions.Insert(index, Instruction.Create(OpCodes.Stfld, instanceField));
                    //}

                    var hiddenCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                        Imports.System.Void)
                    {
                        Parameters = { new ParameterDefinition("ignored", ParameterAttributes.None, unusedType) }
                    };
                    ILProcessor processor = hiddenCtor.Body.GetILProcessor();
                    //processor.Emit(OpCodes.Ldarg_0);
                    //processor.Emit(OpCodes.Call, Imports.System.ObjectCtor);
                    processor.Emit(OpCodes.Nop);
                    processor.Emit(OpCodes.Ret);
                    type.Methods.Add(hiddenCtor);
                }
                return mirrorDefinition;
            }
            LogError($"Could not find mirrored type {mirroredType}");
        }
        return null;
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
        if (externType.HasGenericParameters)
        {
            mirroredTypeName += $"`{externType.GenericParameters.Count}";
        }

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

    private TypeDefinition CreateUnusedType()
    {
        //Used to ensure we avoid overload collision
        return new TypeDefinition("Mirror", "<Unused>",
            TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed |
            TypeAttributes.BeforeFieldInit, Imports.System.ValueType)
        {
            PackingSize = 0,
            ClassSize = 1
        };
    }

    private static FieldDefinition GetMirroredField(TypeDefinition mirroredType, MethodDefinition externMethod,
        TypeReference externPropertyType, string mirrorTargetName)
    {
        foreach (FieldDefinition field in mirroredType.Fields)
        {
            if (field.Name != mirrorTargetName)
            {
                continue;
            }

            if (field.IsStatic != externMethod.IsStatic)
            {
                continue;
            }

            if (field.FieldType.FullName != externPropertyType.FullName)
            {
                if (externPropertyType.Resolve()?.TryGetMirrorTargetName(out _) != true)
                {
                    continue;
                }
            }

            return field;
        }

        return null;
    }

    private static PropertyDefinition GetMirroredProperty(TypeDefinition mirroredType, MethodDefinition externMethod,
        string mirrorTargetName)
    {
        foreach (PropertyDefinition property in mirroredType.Properties)
        {
            if (externMethod.IsGetter && property.GetMethod != null)
            {
                if (property.GetMethod.Name != mirrorTargetName && property.Name != mirrorTargetName)
                {
                    continue;
                }

                if (property.GetMethod.IsStatic != externMethod.IsStatic)
                {
                    continue;
                }

                if (property.GetMethod.ReturnType.FullName != externMethod.ReturnType.FullName)
                {
                    if (externMethod.ReturnType.Resolve()?.TryGetMirrorTargetName(out _) != true)
                    {
                        continue;
                    }
                }
                return property;
            }

            if (externMethod.IsSetter && property.SetMethod != null)
            {
                if (property.SetMethod.Name != mirrorTargetName && property.Name != mirrorTargetName)
                {
                    continue;
                }

                if (property.SetMethod.IsStatic != externMethod.IsStatic)
                {
                    continue;
                }

                if (property.SetMethod.Parameters[0].ParameterType.FullName != externMethod.Parameters[0].ParameterType.FullName)
                {
                    if (externMethod.Parameters[0].ParameterType.Resolve()?.TryGetMirrorTargetName(out _) != true)
                    {
                        continue;
                    }
                }
                return property;
            }

        }

        return null;
    }

    private static MethodDefinition GetMirroredMethod(TypeDefinition mirroredType, MethodDefinition externMethod)
    {
        string mirrorTargetName = externMethod.GetMirrorTargetName();

        foreach (MethodDefinition method in mirroredType.Methods)
        {
            if (method.Name != mirrorTargetName)
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

            bool parametersMatch = true;
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (method.Parameters[i].ParameterType.FullName !=
                    externMethod.Parameters[i].ParameterType.FullName)
                {
                    if (externMethod.Parameters[i].ParameterType.Resolve()
                            .TryGetMirrorTargetName(out string mirrorName) &&
                        method.Parameters[i].ParameterType.FullName == mirrorName)
                    {
                        continue;
                    }

                    parametersMatch = false;
                    break;
                }
            }

            if (!parametersMatch)
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

            return method;
        }
        return null;
    }

    private void ForwardMethodToProperty(MethodDefinition externMethod, PropertyDefinition mirroredProperty,
        FieldDefinition instanceField, TypeDefinition unusedType)
    {
        ILProcessor processor = externMethod.Body.GetILProcessor();

        if (externMethod.IsStatic)
        {
            if (externMethod.IsGetter)
            {
                FieldDefinition returnValueInstanceField = null;

                var resolvedReturnType = externMethod.ReturnType.Resolve();
                if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
                {
                    MethodDefinition hiddenCtor = resolvedReturnType.GetConstructors()
                        .Single(c => c.Parameters.Count == 1 &&
                                     c.Parameters[0].ParameterType == unusedType);
                    returnValueInstanceField = resolvedReturnType.Fields.Single(x => x.Name == resolvedReturnType.GetInstanceFieldName());

                    var unusedVariable = new VariableDefinition(unusedType);
                    externMethod.Body.Variables.Add(unusedVariable);

                    processor.Emit(OpCodes.Ldloca_S, unusedVariable);
                    processor.Emit(OpCodes.Initobj, unusedType);
                    processor.Emit(OpCodes.Ldloc, unusedVariable);
                    processor.Emit(OpCodes.Newobj, hiddenCtor);
                    processor.Emit(OpCodes.Dup);
                }

                processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(mirroredProperty.GetMethod));

                if (returnValueInstanceField != null)
                {
                    processor.Emit(OpCodes.Stfld, returnValueInstanceField);
                }

                processor.Emit(OpCodes.Ret);
            }
            else if (externMethod.IsSetter)
            {
                processor.Emit(OpCodes.Ldarg_0);
                var parameterType = externMethod.Parameters[0].ParameterType.Resolve();
                if (parameterType?.TryGetMirrorTargetName(out string _) == true)
                {
                    FieldDefinition parameterInstanceField = parameterType.Fields.Single(x => x.Name == parameterType.GetInstanceFieldName());
                    processor.Emit(OpCodes.Ldfld, parameterInstanceField);
                }
                processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(mirroredProperty.SetMethod));
                processor.Emit(OpCodes.Ret);
            }
            else
            {
                LogError("Property must have a getter or setter");
            }
        }
        else
        {
            if (externMethod.IsGetter)
            {
                FieldDefinition returnValueInstanceField = null;

                var resolvedReturnType = externMethod.ReturnType.Resolve();
                if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
                {
                    MethodDefinition hiddenCtor = resolvedReturnType.GetConstructors()
                        .Single(c => c.Parameters.Count == 1 &&
                                     c.Parameters[0].ParameterType == unusedType);
                    returnValueInstanceField = resolvedReturnType.Fields.Single(x => x.Name == resolvedReturnType.GetInstanceFieldName());

                    var unusedVariable = new VariableDefinition(unusedType);
                    externMethod.Body.Variables.Add(unusedVariable);

                    processor.Emit(OpCodes.Ldloca_S, unusedVariable);
                    processor.Emit(OpCodes.Initobj, unusedType);
                    processor.Emit(OpCodes.Ldloc, unusedVariable);
                    processor.Emit(OpCodes.Newobj, hiddenCtor);
                    processor.Emit(OpCodes.Dup);
                }

                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, instanceField);
                processor.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(mirroredProperty.GetMethod));
                if (returnValueInstanceField != null)
                {
                    processor.Emit(OpCodes.Stfld, returnValueInstanceField);
                }
                processor.Emit(OpCodes.Ret);
            }
            else if (externMethod.IsSetter)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, instanceField);
                processor.Emit(OpCodes.Ldarg_1);
                var parameterType = externMethod.Parameters[0].ParameterType.Resolve();
                if (parameterType?.TryGetMirrorTargetName(out string _) == true)
                {
                    FieldDefinition parameterInstanceField = parameterType.Fields.Single(x => x.Name == parameterType.GetInstanceFieldName());
                    processor.Emit(OpCodes.Ldfld, parameterInstanceField);
                }

                processor.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(mirroredProperty.SetMethod));
                processor.Emit(OpCodes.Nop);
                processor.Emit(OpCodes.Ret);
            }
            else
            {
                LogError("Property must have a getter or setter");
            }
        }
    }

    private static void ForwardMethodToField(MethodDefinition externMethod, FieldReference mirroredField, FieldDefinition instanceField,
        TypeDefinition unusedType)
    {
        ILProcessor processor = externMethod.Body.GetILProcessor();

        if (externMethod.IsStatic)
        {
            if (externMethod.IsGetter)
            {
                FieldDefinition returnValueInstanceField = null;

                var resolvedReturnType = externMethod.ReturnType.Resolve();
                if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
                {
                    MethodDefinition hiddenCtor = resolvedReturnType.GetConstructors()
                        .Single(c => c.Parameters.Count == 1 &&
                                     c.Parameters[0].ParameterType == unusedType);
                    returnValueInstanceField = resolvedReturnType.Fields.Single(x => x.Name == resolvedReturnType.GetInstanceFieldName());

                    var unusedVariable = new VariableDefinition(unusedType);
                    externMethod.Body.Variables.Add(unusedVariable);

                    processor.Emit(OpCodes.Ldloca_S, unusedVariable);
                    processor.Emit(OpCodes.Initobj, unusedType);
                    processor.Emit(OpCodes.Ldloc, unusedVariable);
                    processor.Emit(OpCodes.Newobj, hiddenCtor);
                    processor.Emit(OpCodes.Dup);
                }
                processor.Emit(OpCodes.Ldsfld, mirroredField);
                if (returnValueInstanceField != null)
                {
                    processor.Emit(OpCodes.Stfld, returnValueInstanceField);
                }
                processor.Emit(OpCodes.Ret);
            }
            else if (externMethod.IsSetter)
            {
                processor.Emit(OpCodes.Ldarg_0);
                var parameterType = externMethod.Parameters[0].ParameterType.Resolve();
                if (parameterType?.TryGetMirrorTargetName(out string _) == true)
                {
                    FieldDefinition returnValueInstanceField = parameterType.Fields.Single(x => x.Name == parameterType.GetInstanceFieldName());
                    processor.Emit(OpCodes.Ldfld, returnValueInstanceField);
                }
                processor.Emit(OpCodes.Stsfld, mirroredField);
                processor.Emit(OpCodes.Ret);
            }
        }
        else
        {
            if (externMethod.IsGetter)
            {
                FieldDefinition returnValueInstanceField = null;

                var resolvedReturnType = externMethod.ReturnType.Resolve();
                if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
                {
                    MethodDefinition hiddenCtor = resolvedReturnType.GetConstructors()
                        .Single(c => c.Parameters.Count == 1 &&
                                     c.Parameters[0].ParameterType == unusedType);
                    returnValueInstanceField = resolvedReturnType.Fields.Single(x => x.Name == resolvedReturnType.GetInstanceFieldName());

                    var unusedVariable = new VariableDefinition(unusedType);
                    externMethod.Body.Variables.Add(unusedVariable);

                    processor.Emit(OpCodes.Ldloca_S, unusedVariable);
                    processor.Emit(OpCodes.Initobj, unusedType);
                    processor.Emit(OpCodes.Ldloc, unusedVariable);
                    processor.Emit(OpCodes.Newobj, hiddenCtor);
                    processor.Emit(OpCodes.Dup);
                }

                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, instanceField);
                processor.Emit(OpCodes.Ldfld, mirroredField);
                if (returnValueInstanceField != null)
                {
                    processor.Emit(OpCodes.Stfld, returnValueInstanceField);
                }
                processor.Emit(OpCodes.Ret);
            }
            else if (externMethod.IsSetter)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, instanceField);
                processor.Emit(OpCodes.Ldarg_1);
                var parameterType = externMethod.Parameters[0].ParameterType.Resolve();
                if (parameterType?.TryGetMirrorTargetName(out string _) == true)
                {
                    FieldDefinition returnValueInstanceField = parameterType.Fields.Single(x => x.Name == parameterType.GetInstanceFieldName());
                    processor.Emit(OpCodes.Ldfld, returnValueInstanceField);
                }
                processor.Emit(OpCodes.Stfld, mirroredField);
                processor.Emit(OpCodes.Ret);
            }
        }
    }

    private static void ForwardMethodToMethod(MethodReference mirroredMethod, MethodDefinition externMethod,
        FieldDefinition instanceField, TypeDefinition unusedType)
    {
        ILProcessor processor = externMethod.Body.GetILProcessor();

        FieldDefinition setField = null;
        if (mirroredMethod.ReturnType.FullName != externMethod.ReturnType.FullName)
        {
            var resolvedReturnType = externMethod.ReturnType.Resolve();
            if (resolvedReturnType?.TryGetMirrorTargetName(out string _) == true)
            {
                MethodDefinition hiddenCtor = resolvedReturnType.GetConstructors()
                    .Single(c => c.Parameters.Count == 1 &&
                                 c.Parameters[0].ParameterType == unusedType);
                setField = resolvedReturnType.Fields.Single(x => x.Name == resolvedReturnType.GetInstanceFieldName());

                var unusedVariable = new VariableDefinition(unusedType);
                externMethod.Body.Variables.Add(unusedVariable);

                processor.Emit(OpCodes.Ldloca_S, unusedVariable);
                processor.Emit(OpCodes.Initobj, unusedType);
                processor.Emit(OpCodes.Ldloc, unusedVariable);
                processor.Emit(OpCodes.Newobj, hiddenCtor);
                processor.Emit(OpCodes.Dup);
            }
        }

        if (externMethod.IsConstructor)
        {
            processor.Emit(OpCodes.Ldarg_0);
            foreach (var parameter in externMethod.Parameters)
            {
                processor.Emit(OpCodes.Ldarg, parameter);
            }
            processor.Emit(OpCodes.Newobj, mirroredMethod);
            processor.Emit(OpCodes.Stfld, instanceField);
        }
        else
        {
            if (externMethod.IsStatic)
            {
                processor.Emit(OpCodes.Call, mirroredMethod);
            }
            else
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, instanceField);
                foreach (var parameter in externMethod.Parameters)
                {
                    processor.Emit(OpCodes.Ldarg, parameter);

                    var resolvedParameterType = parameter.ParameterType.Resolve();
                    if (resolvedParameterType.TryGetMirrorTargetName(out string _))
                    {
                        processor.Emit(OpCodes.Ldfld, resolvedParameterType.GetInstanceField());
                    }
                }

                processor.Emit(OpCodes.Callvirt, mirroredMethod);
            }
        }

        if (setField != null)
        {
            processor.Emit(OpCodes.Stfld, setField);
        }

        processor.Emit(OpCodes.Ret);
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
