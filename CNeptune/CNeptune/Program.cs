using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CNeptune
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        private const string Module = "<Module>";
        private const string Pointer = "<Pointer>";

        static private readonly System.Reflection.MethodInfo GetMethodFromHandle = Metadata.Method(() => System.Reflection.MethodInfo.GetMethodFromHandle(Argument<RuntimeMethodHandle>.Value, Argument<RuntimeTypeHandle>.Value));
        static private readonly System.Reflection.MethodInfo GetMethodHandle = Metadata<System.Reflection.MethodBase>.Property(_Method => _Method.MethodHandle).GetGetMethod();
        static private readonly System.Reflection.MethodInfo GetFunctionPointer = Metadata<RuntimeMethodHandle>.Method(_Method => _Method.GetFunctionPointer());

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
                    foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
                    {
                        foreach (var _attribute in _element.Parent.Attributes())
                        {
                            if (_attribute.Value.Contains(arguments[1]))
                            {
                                Program.Manage(string.Concat(_directory, _element.Value, _name, ".dll"));
                                return;
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
            return type.IsInterface || type.IsValueType || type.Name == Program.Module || type.Name == Program.Neptune || type.BaseType == type.Module.Import(typeof(MulticastDelegate));
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
            foreach (var _method in type.Methods.ToArray()) { Program.Manage(_type, _method); }
        }

        static private string Name(MethodDefinition method)
        {
            return string.Concat("<", method.IsConstructor ? method.DeclaringType.Name : method.Name, ">");
        }

        static private void Restore(TypeDefinition type)
        {
            var _type = Program.Instrumentation(type);
            if (_type == null) { return; }
            foreach (var _method in type.Methods.ToArray())
            {
                var _name = Program.Name(_method);
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
                        _type.Methods.RemoveAt(_index);
                        break;
                    }
                }
            }
            type.NestedTypes.Remove(_type);
        }

        static private void Manage(TypeDefinition type, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            var _name = Program.Name(method);
            var _authentic = type.Method(_name, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName, method.ReturnType);
            var _gateway = type.Type(_name, TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _pointer = _gateway.Field<IntPtr>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public);
            var _activator = _gateway.Activator();
            _authentic.Body = method.Body;
            _activator.Body.Variable<RuntimeMethodHandle>("<RuntimeMethodHandle>");
            method.Body = new MethodBody(method);
            method.Body.Variable<IntPtr>(Program.Pointer);
            if (method.IsStatic)
            {
                if (method.GenericParameters.Count == 0)
                {
                    foreach (var _parameter in method.Parameters) { _authentic.Parameters.Add(_parameter); }
                    _activator.Body.Emit(OpCodes.Ldtoken, _authentic);
                    _activator.Body.Emit(OpCodes.Ldtoken, _authentic.DeclaringType);
                    _activator.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activator.Body.Emit(OpCodes.Stloc_0);
                    _activator.Body.Emit(OpCodes.Ldloca_S, _activator.Body.Variables[0]);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activator.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activator.Body.Emit(OpCodes.Ret);
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
                    foreach (var _parameter in method.Parameters) { _authentic.Parameters.Add(_parameter); }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in _gateway.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _activator.Body.Emit(OpCodes.Ldtoken, _method);
                    _activator.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activator.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activator.Body.Emit(OpCodes.Stloc_0);
                    _activator.Body.Emit(OpCodes.Ldloca_S, _activator.Body.Variables[0]);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activator.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activator.Body.Emit(OpCodes.Ret);
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
                    _activator.Body.Emit(OpCodes.Ldtoken, _authentic);
                    _activator.Body.Emit(OpCodes.Ldtoken, _authentic.DeclaringType);
                    _activator.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activator.Body.Emit(OpCodes.Stloc_0);
                    _activator.Body.Emit(OpCodes.Ldloca_S, _activator.Body.Variables[0]);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activator.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activator.Body.Emit(OpCodes.Ret);
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
                    foreach (var _parameter in method.Parameters) { _authentic.Parameters.Add(_parameter); }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in _gateway.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _activator.Body.Emit(OpCodes.Ldtoken, _method);
                    _activator.Body.Emit(OpCodes.Ldtoken, _method.DeclaringType);
                    _activator.Body.Emit(OpCodes.Call, Program.GetMethodFromHandle);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                    _activator.Body.Emit(OpCodes.Stloc_0);
                    _activator.Body.Emit(OpCodes.Ldloca_S, _activator.Body.Variables[0]);
                    _activator.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                    _activator.Body.Emit(OpCodes.Stsfld, _pointer);
                    _activator.Body.Emit(OpCodes.Ret);
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
        }
    }
}
