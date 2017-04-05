using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

using BindingFlags = System.Reflection.BindingFlags;
using ConstructorInfo = System.Reflection.ConstructorInfo;
using DynamicMethod = System.Reflection.Emit.DynamicMethod;
using MethodBase = System.Reflection.MethodBase;
using MethodInfo = System.Reflection.MethodInfo;

namespace CNeptune
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        private const string Module = "<Module>";
        private const string Pointer = "<Pointer>";
        private const string Update = "<Update>";
        private const string Override = "<Override>";
        private const string Method = "<Method>";

        static private readonly MethodInfo GetTypeFromHandle = Metadata.Method(() => Type.GetTypeFromHandle(Argument<RuntimeTypeHandle>.Value));
        static private readonly MethodInfo GetMethodFromHandle = Metadata.Method(() => MethodInfo.GetMethodFromHandle(Argument<RuntimeMethodHandle>.Value, Argument<RuntimeTypeHandle>.Value));
        static private readonly MethodInfo GetMethodHandle = Metadata<MethodBase>.Property(_Method => _Method.MethodHandle).GetGetMethod();
        static private readonly MethodInfo GetFunctionPointer = Metadata<RuntimeMethodHandle>.Method(_Method => _Method.GetFunctionPointer());
        static private readonly MethodInfo CreateDelegate = Metadata.Method(() => Delegate.CreateDelegate(Argument<Type>.Value, Argument<MethodInfo>.Value));
        static private readonly MethodInfo GetMethodDescriptor = Metadata<DynamicMethod>.Type.GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        static private readonly MethodInfo MethodEqualityOperator = Metadata<MethodInfo>.Type.GetMethod("op_Equality");
        static private readonly ConstructorInfo Exception = Metadata.Constructor(() => new NotSupportedException());

        static public void Main(string[] arguments)
        {
            if (arguments == null) { throw new ArgumentNullException(); }
            switch (arguments.Length)
            {
                case 1:
                    Program.Manage(arguments[0]);
                    break;
                case 2:
                    var _directory = string.Concat(Path.GetDirectoryName(arguments[0]), @"\");
                    var _document = XDocument.Load(arguments[0]);
                    var _namespace = _document.Root.Name.Namespace;
                    var _name = _document.Descendants(_namespace.GetName("AssemblyName")).Single().Value;
                    var _type = _document.Descendants(_namespace.GetName("OutputType")).SingleOrDefault();
                    foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
                    {
                        foreach (var _attribute in _element.Parent.Attributes())
                        {
                            if (_attribute.Value.Contains(arguments[1]))
                            {
                                switch (_type == null ? "Library" : _type.Value)
                                {
                                    case "Library": Program.Manage(string.Concat(_directory, _element.Value, _name, ".dll")); return;
                                    case "WinExe":
                                    case "Exe": Program.Manage(string.Concat(_directory, _element.Value, _name, ".exe")); return;
                                    default: throw new NotSupportedException($"Unknown OutputType: {_type.Value}");
                                }
                            }
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        static private void Manage(string assembly)
        {
            var _resolver = new DefaultAssemblyResolver();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(assembly));
            var _assembly = AssemblyDefinition.ReadAssembly(assembly, new ReaderParameters() { AssemblyResolver = _resolver, ReadSymbols = true, ReadingMode = ReadingMode.Immediate });
            var _module = _assembly.MainModule;
            foreach (var _type in _module.GetTypes().ToArray()) { Program.Manage(_type); }
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true });
        }

        static private bool Bypass(TypeDefinition type)
        {
            return type.IsInterface || type.IsValueType || type.Name == Program.Module || type.Name == Program.Neptune || (type.BaseType != null && type.BaseType.Resolve() == type.Module.Import(typeof(MulticastDelegate)).Resolve());
        }

        static private TypeDefinition Instrumentation(TypeDefinition type)
        {
            foreach (var _type in type.NestedTypes) { if (_type.Name == Program.Neptune) { return _type; } }
            return null;
        }

        static private void Manage(TypeDefinition type)
        {
            if (Program.Bypass(type)) { return; }
            Program.Restore(type);
            var _type = type.Type(string.Concat(Program.Neptune), TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _update = _type.Method(string.Concat("<", Program.Update, ">"), MethodAttributes.Public | MethodAttributes.Static);
            _update.Parameters.Add(new ParameterDefinition(Program.Method, ParameterAttributes.None, type.Module.Import(typeof(MethodBase))));
            _update.Parameters.Add(new ParameterDefinition(Program.Override, ParameterAttributes.None, type.Module.Import(typeof(Func<MethodInfo, MethodInfo>))));
            var _field = _type.Field<Action<MethodBase, Func<MethodInfo, MethodInfo>>>(Program.Update, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly);
            var _activation = _type.Activation();
            var _pointer = Program.GetMethodPointer(_type, _activation);
            _activation.Body.Emit(OpCodes.Ldtoken, _field.FieldType);
            _activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
            _activation.Body.Emit(OpCodes.Ldtoken, _update);
            _activation.Body.Emit(OpCodes.Ldtoken, _update.DeclaringType);
            _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
            _activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
            _activation.Body.Emit(OpCodes.Stsfld, _field);
            _activation.Body.Emit(OpCodes.Ret);
            foreach (var _method in type.Methods.ToArray()) { Program.Manage(_type, _update, _pointer, _method); }
            _update.Body.Emit(OpCodes.Newobj, Metadata.Constructor(() => new NotSupportedException()));
            _update.Body.Emit(OpCodes.Throw);
            _update.Body.Emit(OpCodes.Ret);
        }

        static private string Authentic(MethodDefinition method)
        {
            return string.Concat("<", method.IsConstructor ? (method.DeclaringType.Name.Contains('`') ? method.DeclaringType.Name.Substring(0, method.DeclaringType.Name.IndexOf('`')) : method.DeclaringType.Name) : method.Name, ">");
        }

        static private string Gateway(TypeReference type)
        {
            var _index = type.Name.IndexOf('`');
            var _name = _index < 0 ? type.Name : type.Name.Substring(0, _index);
            if (type.GenericParameters.Count == 0) { return string.Concat("<", _name, ">"); }
            _name = string.Concat(_name, "<", type.GenericParameters.Count.ToString(), ">");
            return string.Concat("<", _name, string.Concat("<", string.Concat(type.GenericParameters.Select(_parameter => string.Concat("<", _parameter.Name, ">"))), ">"), ">");
        }

        static private string Gateway(MethodDefinition method)
        {
            return string.Concat("<", method.IsConstructor ? method.DeclaringType.Name : method.Name, method.GenericParameters.Count > 0 ? string.Concat("<", method.GenericParameters.Count, ">") : string.Empty, method.Parameters.Count > 0 ? string.Concat("<", string.Concat(method.Parameters.Select(_parameter => Program.Gateway(_parameter.ParameterType))), ">") : string.Empty, ">");
        }

        static private void Restore(TypeDefinition type)
        {
            var _type = Program.Instrumentation(type);
            if (_type == null) { return; }
            foreach (var _method in type.Methods.ToArray())
            {
                var _name = Program.Authentic(_method);
                for (var _index = 0; _index < _type.NestedTypes.Count; _index++)
                {
                    if (_type.NestedTypes[_index].Name == _name)
                    {
                        _type.NestedTypes.RemoveAt(_index);
                        break;
                    }
                }
                for (var _index = 0; _index < _type.Methods.Count; _index++)
                {
                    if (_type.Methods[_index].Name == _name)
                    {
                        _method.Body = _type.Methods[_index].Body;
                        //TODO translate for generic!
                        _type.Methods.RemoveAt(_index);
                        break;
                    }
                }
            }
            type.NestedTypes.Remove(_type);
        }

        static private MethodDefinition GetMethodPointer(TypeDefinition type, MethodDefinition activation)
        {
            var _field = type.Field<Func<DynamicMethod, RuntimeMethodHandle>>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly);
            activation.Body.Emit(OpCodes.Ldtoken, _field.FieldType);
            activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
            activation.Body.Emit(OpCodes.Ldtoken, Program.GetMethodDescriptor);
            activation.Body.Emit(OpCodes.Ldtoken, Program.GetMethodDescriptor.DeclaringType);
            activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
            activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
            activation.Body.Emit(OpCodes.Stsfld, _field);
            var _method = type.Method<IntPtr>(Program.Pointer, MethodAttributes.Static | MethodAttributes.Public);
            _method.Parameter<MethodInfo>();
            var _variable = _method.Body.Variable<RuntimeMethodHandle>();
            _method.Body.Emit(OpCodes.Ldarg_0);
            _method.Body.Emit(OpCodes.Isinst, Metadata<DynamicMethod>.Type);
            using (_method.Body.Brfalse())
            {
                _method.Body.Emit(OpCodes.Ldsfld, _field);
                _method.Body.Emit(OpCodes.Ldarg_0);
                _method.Body.Emit(OpCodes.Call, Metadata<Func<MethodInfo, MethodInfo>>.Method(_Function => _Function.Invoke(Argument<MethodInfo>.Value)));
                _method.Body.Emit(OpCodes.Stloc_0);
                _method.Body.Emit(OpCodes.Ldloca_S, _variable);
                _method.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                _method.Body.Emit(OpCodes.Ret);
            }
            _method.Body.Emit(OpCodes.Ldarg_0);
            _method.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
            _method.Body.Emit(OpCodes.Stloc_0);
            _method.Body.Emit(OpCodes.Ldloca_S, _variable);
            _method.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
            _method.Body.Emit(OpCodes.Ret);
            return _method;
        }

        static private void Manage(TypeDefinition type, MethodDefinition update, MethodDefinition pointer, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            var _authentic = type.Method(Program.Authentic(method), MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName, method.ReturnType);
            var _gateway = type.Type(Program.Gateway(method), TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _activation = _gateway.Activation();
            var _pointer = _gateway.Field<IntPtr>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public);
            var _override = _gateway.Field<Func<MethodInfo, MethodInfo>>(Program.Override, FieldAttributes.Static | FieldAttributes.Public);
            if (method.GenericParameters.Count == 0) // TODO : add support for generic in next release!
            {
                update.Body.Emit(OpCodes.Ldarg_0);
                update.Body.Emit(OpCodes.Ldtoken, method);
                update.Body.Emit(OpCodes.Ldtoken, method.DeclaringType);
                update.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                update.Body.Emit(OpCodes.Call, Program.MethodEqualityOperator);
                using (update.Body.Brfalse())
                {
                    update.Body.Emit(OpCodes.Ldarg_1);
                    update.Body.Emit(OpCodes.Stsfld, _override);
                    update.Body.Emit(OpCodes.Ldarg_1);
                    update.Body.Emit(OpCodes.Ldtoken, _authentic);
                    update.Body.Emit(OpCodes.Ldtoken, _authentic.DeclaringType);
                    update.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    update.Body.Emit(OpCodes.Call, Metadata<Func<MethodInfo, MethodInfo>>.Method(_Function => _Function.Invoke(Argument<MethodInfo>.Value)));
                    update.Body.Emit(OpCodes.Call, pointer);
                    update.Body.Emit(OpCodes.Stsfld, _pointer);
                    update.Body.Emit(OpCodes.Ret);
                }
            }
            var _method = _gateway.Method<MethodInfo>(Program.Override, MethodAttributes.Static | MethodAttributes.Public);
            _method.Parameters.Add(new ParameterDefinition(Program.Method, ParameterAttributes.None, _method.ReturnType));
            _method.Body.Emit(OpCodes.Ldarg_0);
            _method.Body.Emit(OpCodes.Ret);
            _authentic.Body = method.Body;
            _activation.Body.Variable<RuntimeMethodHandle>();
            method.Body = new MethodBody(method);
            method.Body.Variable<IntPtr>(Program.Pointer);
            if (method.IsStatic)
            {
                if (method.GenericParameters.Count == 0)
                {
                    foreach (var _parameter in method.Parameters) { _authentic.Parameters.Add(_parameter); }
                    _activation.Body.Emit(OpCodes.Ldtoken, _override.FieldType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
                    _activation.Body.Emit(OpCodes.Stsfld, _override);
                    _activation.Body.Emit(OpCodes.Ldtoken, _authentic);
                    _activation.Body.Emit(OpCodes.Ldtoken, _authentic.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activation.Body.Emit(OpCodes.Stloc_0);
                    _activation.Body.Emit(OpCodes.Ldloca_S, _activation.Body.Variables[0]);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activation.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activation.Body.Emit(OpCodes.Ret);
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                            case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                            case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                            case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                            default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[_index]); break;
                        }
                    }
                    method.Body.Emit(OpCodes.Ldsfld, _pointer);
                    method.Body.Emit(OpCodes.Calli, _authentic.ReturnType, _authentic.Parameters);
                    method.Body.Emit(OpCodes.Ret);
                }
                else
                {
                    foreach (var _parameter in method.GenericParameters)
                    {
                        _gateway.GenericParameters.Add(_parameter.Copy(_gateway));
                        _authentic.GenericParameters.Add(_parameter.Copy(_authentic));
                    }
                    foreach (var _parameter in method.Parameters)
                    {
                        for (var _index = 0; _index < method.GenericParameters.Count; _index++)
                        {
                            if (method.GenericParameters[_index] == _parameter.ParameterType)
                            {
                                _authentic.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _authentic.GenericParameters[_index]));
                                goto generic;
                            }
                        }
                        _authentic.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _parameter.ParameterType));
                        generic:;
                    }
                    var _generic = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in _gateway.GenericParameters) { _generic.GenericArguments.Add(_parameter); }
                    _activation.Body.Emit(OpCodes.Ldtoken, _override.FieldType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
                    _activation.Body.Emit(OpCodes.Stsfld, _override);
                    _activation.Body.Emit(OpCodes.Ldtoken, _generic);
                    _activation.Body.Emit(OpCodes.Ldtoken, _generic.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activation.Body.Emit(OpCodes.Stloc_0);
                    _activation.Body.Emit(OpCodes.Ldloca_S, _activation.Body.Variables[0]);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activation.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activation.Body.Emit(OpCodes.Ret);
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                            case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                            case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                            case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                            default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[_index]); break;
                        }
                    }
                    var _type = new GenericInstanceType(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _type.GenericArguments.Add(_parameter); }
                    method.Body.Emit(OpCodes.Ldsfld, new FieldReference(_pointer.Name, _pointer.FieldType, _type));
                    method.Body.Emit(OpCodes.Calli, _authentic.ReturnType, _authentic.Parameters);
                    method.Body.Emit(OpCodes.Ret);
                }
            }
            else
            {
                if (method.GenericParameters.Count == 0)
                {
                    _authentic.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    foreach (var _parameter in method.Parameters) { _authentic.Parameters.Add(_parameter); }
                    _activation.Body.Emit(OpCodes.Ldtoken, _override.FieldType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
                    _activation.Body.Emit(OpCodes.Stsfld, _override);
                    _activation.Body.Emit(OpCodes.Ldtoken, _authentic);
                    _activation.Body.Emit(OpCodes.Ldtoken, _authentic.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activation.Body.Emit(OpCodes.Stloc_0);
                    _activation.Body.Emit(OpCodes.Ldloca_S, _activation.Body.Variables[0]);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activation.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activation.Body.Emit(OpCodes.Ret);
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                            case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                            case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                            case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                            default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[_index]); break;
                        }
                    }
                    method.Body.Emit(OpCodes.Ldsfld, _pointer);
                    method.Body.Emit(OpCodes.Calli, _authentic.ReturnType, _authentic.Parameters);
                    method.Body.Emit(OpCodes.Ret);
                }
                else
                {
                    foreach (var _parameter in method.GenericParameters)
                    {
                        _gateway.GenericParameters.Add(_parameter.Copy(_gateway));
                        _authentic.GenericParameters.Add(_parameter.Copy(_authentic));
                    }
                    _authentic.Parameters.Add(new ParameterDefinition(method.DeclaringType));
                    foreach (var _parameter in method.Parameters)
                    {
                        for (var _index = 0; _index < method.GenericParameters.Count; _index++)
                        {
                            if (method.GenericParameters[_index] == _parameter.ParameterType)
                            {
                                _authentic.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _authentic.GenericParameters[_index]));
                                goto generic;
                            }
                        }
                        _authentic.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _parameter.ParameterType));
                        generic:;
                    }
                    var _type = new GenericInstanceType(_gateway);
                    var _generic = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in _gateway.GenericParameters)
                    {
                        _type.GenericArguments.Add(_parameter);
                        _generic.GenericArguments.Add(_parameter);
                    }
                    _activation.Body.Emit(OpCodes.Ldtoken, _override.FieldType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetTypeFromHandle);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method);
                    _activation.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Call, Program.CreateDelegate);
                    _activation.Body.Emit(OpCodes.Stsfld, new FieldReference(_override.Name, _override.FieldType, _type));
                    _activation.Body.Emit(OpCodes.Ldtoken, _generic);
                    _activation.Body.Emit(OpCodes.Ldtoken, _generic.DeclaringType);
                    _activation.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activation.Body.Emit(OpCodes.Stloc_0);
                    _activation.Body.Emit(OpCodes.Ldloca_S, _activation.Body.Variables[0]);
                    _activation.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activation.Body.Emit(OpCodes.Stsfld, new FieldReference(_pointer.Name, _pointer.FieldType, _type));
                    _activation.Body.Emit(OpCodes.Ret);
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                            case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                            case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                            case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                            default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[_index]); break;
                        }
                    }
                    _type = new GenericInstanceType(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _type.GenericArguments.Add(_parameter); }
                    method.Body.Emit(OpCodes.Ldsfld, new FieldReference(_pointer.Name, _pointer.FieldType, _type));
                    method.Body.Emit(OpCodes.Calli, _authentic.ReturnType, _authentic.Parameters);
                    method.Body.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
