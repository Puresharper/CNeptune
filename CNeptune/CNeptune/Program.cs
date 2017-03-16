using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CNeptune
{
    static public class Program
    {
        static public void Main(string[] args)
        {
            var _location = args[0];
            var _resolver = new DefaultAssemblyResolver();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(_location));
            var _assembly = AssemblyDefinition.ReadAssembly(_location, new ReaderParameters() { AssemblyResolver = _resolver, ReadSymbols = true, ReadingMode = ReadingMode.Immediate });
            Program.Manage(_assembly);
            _assembly.Write(Path.GetFileName(_location), new WriterParameters { WriteSymbols = true });
        }

        static private void Manage(AssemblyDefinition assembly)
        {
            foreach (var _module in assembly.Modules.ToArray()) { Program.Manage(_module); }
        }

        static private void Manage(ModuleDefinition module)
        {
            foreach (var _type in module.Types.ToArray()) { Program.Manage(_type); }
        }

        static private void Manage(TypeDefinition type)
        {
            if (type.IsInterface || type.IsValueType) { return; }
            if (type.Name == "<Module>") { return; }
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate)) { type.Attributes = (type.Attributes & ~TypeAttributes.NestedPrivate) | TypeAttributes.NestedAssembly; }
            else if (type.Attributes.HasFlag(TypeAttributes.NestedFamily)) { type.Attributes = (type.Attributes & ~TypeAttributes.NestedFamily) | TypeAttributes.NestedFamORAssem; }
            var _module = type.Module;
            var _type = new TypeDefinition(null, string.Concat("<Neptune>"), TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName, _module.Import(typeof(object)));
            var _attribute = new CustomAttribute(_module.Import(typeof(EditorBrowsableAttribute).GetConstructor(new Type[] { typeof(EditorBrowsableState) })));
            _attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.Import(typeof(EditorBrowsableState)), EditorBrowsableState.Never));
            _type.CustomAttributes.Add(_attribute);
            _attribute = new CustomAttribute(_module.Import(typeof(BrowsableAttribute).GetConstructor(new Type[] { typeof(bool) })));
            _attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.Import(typeof(bool)), false));
            _type.CustomAttributes.Add(_attribute);
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            var _ctor = new MethodDefinition(".ctor", methodAttributes, _module.Import(typeof(void)));
            var _body = _ctor.Body.GetILProcessor();
            _body.Append(_body.Create(OpCodes.Ldarg_0));
            _body.Append(_body.Create(OpCodes.Call, _module.Import(typeof(object).GetConstructor(Type.EmptyTypes))));
            _body.Append(_body.Create(OpCodes.Ret));
            _type.Methods.Add(_ctor);
            var _field = new FieldDefinition("Authority", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName, _type);
            _type.Fields.Add(_field);
            var _cctor = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName, _module.Import(typeof(void)));
            _body = _cctor.Body.GetILProcessor();
            _body.Append(_body.Create(OpCodes.Newobj, _ctor));
            _body.Append(_body.Create(OpCodes.Stsfld, _field));
            _body.Append(_body.Create(OpCodes.Ret));
            _type.Methods.Add(_cctor);
            foreach (var _method in type.Methods.ToArray()) { Program.Manage(_type, _field, _method); }
            type.NestedTypes.Add(_type);
        }

        static private void Manage(TypeDefinition type, FieldDefinition field, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            var _gateway = new MethodDefinition(string.Concat("<", method.Name, ">"), MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName, method.ReturnType);
            var _authentic = new MethodDefinition(string.Concat("<<", method.Name, ">>"), MethodAttributes.Static | MethodAttributes.Assembly, method.ReturnType);
            if (method.IsStatic)
            {
                if (method.GenericParameters.Count == 0)
                {
                    foreach (var _parameter in method.Parameters)
                    {
                        _authentic.Parameters.Add(_parameter);
                        _gateway.Parameters.Add(_parameter);
                    }
                    _authentic.Body = method.Body;
                    var _body = _gateway.Body.GetILProcessor();
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index + 1)); break;
                        }
                    }
                    _body.Append(_body.Create(OpCodes.Call, _authentic));
                    _body.Append(_body.Create(OpCodes.Ret));
                    method.Body = new MethodBody(method);
                    _body = method.Body.GetILProcessor();
                    _body.Append(_body.Create(OpCodes.Ldsfld, field));
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_0)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 3: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index)); break;
                        }
                    }
                    _body.Append(_body.Create(OpCodes.Call, _gateway));
                    _body.Append(_body.Create(OpCodes.Ret));
                }
                else
                {
                    foreach (var _parameter in method.GenericParameters)
                    {
                        _gateway.GenericParameters.Add(new GenericParameter(_parameter.Name, _gateway));
                        _authentic.GenericParameters.Add(new GenericParameter(_parameter.Name, _authentic));
                    }
                    foreach (var _parameter in method.Parameters)
                    {
                        _authentic.Parameters.Add(_parameter);
                        _gateway.Parameters.Add(_parameter);
                    }
                    _authentic.Body = method.Body;
                    var _body = _gateway.Body.GetILProcessor();
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index + 1)); break;
                        }
                    }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _body.Append(_body.Create(OpCodes.Call, _method));
                    _body.Append(_body.Create(OpCodes.Ret));
                    method.Body = new MethodBody(method);
                    _body = method.Body.GetILProcessor();
                    _body.Append(_body.Create(OpCodes.Ldsfld, field));
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_0)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 3: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index)); break;
                        }
                    }
                    _method = new GenericInstanceMethod(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _body.Append(_body.Create(OpCodes.Call, _method));
                    _body.Append(_body.Create(OpCodes.Ret));
                }
            }
            else
            {
                if (method.GenericParameters.Count == 0)
                {
                    _authentic.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    _gateway.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    foreach (var _parameter in method.Parameters)
                    {
                        _authentic.Parameters.Add(_parameter);
                        _gateway.Parameters.Add(_parameter);
                    }
                    _authentic.Body = method.Body;
                    var _body = _gateway.Body.GetILProcessor();
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index + 1)); break;
                        }
                    }
                    _body.Append(_body.Create(OpCodes.Call, _authentic));
                    _body.Append(_body.Create(OpCodes.Ret));
                    method.Body = new MethodBody(method);
                    _body = method.Body.GetILProcessor();
                    _body.Append(_body.Create(OpCodes.Ldsfld, field));
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_0)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 3: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index)); break;
                        }
                    }
                    _body.Append(_body.Create(OpCodes.Call, _gateway));
                    _body.Append(_body.Create(OpCodes.Ret));
                }
                else
                {
                    foreach (var _parameter in method.GenericParameters)
                    {
                        _gateway.GenericParameters.Add(new GenericParameter(_parameter.Name, _gateway));
                        _authentic.GenericParameters.Add(new GenericParameter(_parameter.Name, _authentic));
                    }
                    _authentic.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    _gateway.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    foreach (var _parameter in method.Parameters)
                    {
                        _authentic.Parameters.Add(_parameter);
                        _gateway.Parameters.Add(_parameter);
                    }
                    _authentic.Body = method.Body;
                    var _body = _gateway.Body.GetILProcessor();
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index + 1)); break;
                        }
                    }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _body.Append(_body.Create(OpCodes.Call, _method));
                    _body.Append(_body.Create(OpCodes.Ret));
                    method.Body = new MethodBody(method);
                    _body = method.Body.GetILProcessor();
                    _body.Append(_body.Create(OpCodes.Ldsfld, field));
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _body.Append(_body.Create(OpCodes.Ldarg_0)); break;
                            case 1: _body.Append(_body.Create(OpCodes.Ldarg_1)); break;
                            case 2: _body.Append(_body.Create(OpCodes.Ldarg_2)); break;
                            case 3: _body.Append(_body.Create(OpCodes.Ldarg_3)); break;
                            default: _body.Append(_body.Create(OpCodes.Ldarg_S, _index)); break;
                        }
                    }
                    _method = new GenericInstanceMethod(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _body.Append(_body.Create(OpCodes.Call, _method));
                    _body.Append(_body.Create(OpCodes.Ret));
                }
            }
            type.Methods.Add(_gateway);
            type.Methods.Add(_authentic);
        }
    }
}
