using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CNeptune
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        private const string Native = "<Module>";

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
            if (_assembly.CustomAttributes.Any(_Attribute => _Attribute.ConstructorArguments.Count == 1 && object.Equals(_Attribute.ConstructorArguments.First().Value, Program.Neptune))) { return; }
            _assembly.Attribute(() => new InternalsVisibleToAttribute(Program.Neptune));
            var _native = _module.GetType(null, Program.Native);
            var _gateway = _module.Type(Program.Neptune, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NotPublic);
            var _neptune = _gateway.Field<System.Reflection.Emit.ModuleBuilder>(Program.Neptune, FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.SpecialName);

            //TODO Auto-Mod-lookup
            //var _cctor = _native.Initializer();
            //_cctor.Body.Variable<System.Reflection.Assembly[]>("domain");
            //_cctor.Body.Variable<int>("index");
            //_cctor.Body.Variable<System.Reflection.Assembly>("assembly");
            //_cctor.Body.Variable<System.Reflection.Emit.ModuleBuilder>("module");
            //_cctor.Body.Variable<System.Reflection.Emit.TypeBuilder>("type");
            //_cctor.Body.Emit(OpCodes.Call, Metadata.Property(() => AppDomain.CurrentDomain).GetGetMethod());
            //_cctor.Body.Emit(OpCodes.Call, Metadata<AppDomain>.Method(_AppDomain => _AppDomain.GetAssemblies()));
            //_cctor.Body.Emit(OpCodes.Stloc_0);

            ////label 1!
            //_cctor.Body.Emit(OpCodes.Ldloc_0);
            //_cctor.Body.Emit(OpCodes.Ldlen);
            //_cctor.Body.Emit(OpCodes.Ldloc_1);
            //_cctor.Body.Emit(OpCodes.Bge_S); //goto not found


            //_cctor.Body.Emit(OpCodes.Ldloc_0);
            //_cctor.Body.Emit(OpCodes.Ldloc_1);
            //_cctor.Body.Emit(OpCodes.Ldelem_Ref);
            //_cctor.Body.Emit(OpCodes.Stloc_2);
            //_cctor.Body.Emit(OpCodes.Ldloc_2);
            //_cctor.Body.Emit(OpCodes.Call, Metadata<System.Reflection.Assembly>.Property(_Assembly => _Assembly.FullName).GetGetMethod());
            //_cctor.Body.Emit(OpCodes.Ldstr, Program.Neptune);
            //_cctor.Body.Emit(OpCodes.Beq_S); // goto matching!
            //_cctor.Body.Emit(OpCodes.Ldloc_1);
            //_cctor.Body.Emit(OpCodes.Ldc_I4_1);
            //_cctor.Body.Emit(OpCodes.Add);
            //_cctor.Body.Emit(OpCodes.Stloc_1);
            ////goto label 1

            ////notfound
            //_cctor.Body.Emit(OpCodes.Call, Metadata.Property(() => AppDomain.CurrentDomain).GetGetMethod());
            //_cctor.Body.Emit(OpCodes.Ldstr, Program.Neptune);
            //_cctor.Body.Emit(OpCodes.Newobj, Metadata.Constructor(() => new System.Reflection.AssemblyName(Argument<string>.Value)));
            //_cctor.Body.Emit(OpCodes.Ldc_I4_1);
            //_cctor.Body.Emit(OpCodes.Call, Metadata<AppDomain>.Method(_AppDomain => _AppDomain.DefineDynamicAssembly(Argument<System.Reflection.AssemblyName>.Value, Argument<System.Reflection.Emit.AssemblyBuilderAccess>.Value)));
            //_cctor.Body.Emit(OpCodes.Ldstr, Program.Neptune);
            //_cctor.Body.Emit(OpCodes.Ldc_I4_1);
            //_cctor.Body.Emit(OpCodes.Call, Metadata<System.Reflection.Emit.AssemblyBuilder>.Method(_AssemblyBuilder => _AssemblyBuilder.DefineDynamicModule(Argument<string>.Value, Argument<bool>.Value)));
            //_cctor.Body.Emit(OpCodes.Stloc_3);
            //_cctor.Body.Emit(OpCodes.Ldloc_3);
            //_cctor.Body.Emit(OpCodes.Ldstr, Program.Neptune);
            //_cctor.Body.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.SpecialName));
            //_cctor.Body.Emit(OpCodes.Call, Metadata<System.Reflection.Emit.ModuleBuilder>.Method(_ModuleBuilder => _ModuleBuilder.DefineType(Argument<string>.Value, Argument<System.Reflection.TypeAttributes>.Value)));
            //_cctor.Body.Emit(OpCodes.Stloc, 4);
            //_cctor.Body.Emit(OpCodes.Stloc, 4);
            ////define field
            ////create type
            ////find field
            ////call SetValue with current module
            ////set spfield  = module
            ////ret

            ////matching!
            ////assembly.GetType("neptune");
            ////find field
            ////get Value
            ////set spfield
            ////ret



            foreach (var _type in _module.GetTypes().ToArray()) { Program.Manage(_gateway, _type); }
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true });
        }
        
        static private void Manage(TypeDefinition gateway, TypeDefinition authentic)
        {
            if (authentic.IsInterface || authentic.IsValueType) { return; }
            if (authentic.Name == Program.Native) { return; }
            if (authentic.Name == Program.Neptune) { return; }
            var _authentic = authentic.Type(string.Concat(Program.Neptune), TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _gateway = gateway.Type(string.Concat("<", authentic.MetadataToken.ToInt32(), ">"), TypeAttributes.Class | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _ctor = _gateway.Constructor();
            _ctor.Body.Emit(OpCodes.Ldarg_0);
            _ctor.Body.Emit(OpCodes.Call, Metadata.Constructor(() => new object()));
            _ctor.Body.Emit(OpCodes.Ret);
            var _authority = _gateway.Field(Program.Neptune, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName, _authentic);
            var _cctor = _gateway.Initializer();            
            _cctor.Body.Emit(OpCodes.Newobj, _ctor);
            _cctor.Body.Emit(OpCodes.Stsfld, _authority);
            _cctor.Body.Emit(OpCodes.Ret);
            foreach (var _method in authentic.Methods.ToArray()) { Program.Manage(_gateway, _authentic, _authority, _method); }
        }

        static private void Manage(TypeDefinition gateway, TypeDefinition authentic, FieldDefinition authority, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            var _gateway = gateway.Method(string.Concat("<", method.MetadataToken.ToInt32(), ">"), MethodAttributes.Virtual | MethodAttributes.Assembly | MethodAttributes.SpecialName, method.ReturnType);
            var _authentic = authentic.Method(string.Concat("<", method.IsConstructor ? method.DeclaringType.Name : method.Name, ">"), MethodAttributes.Static | MethodAttributes.Assembly, method.ReturnType);
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
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _gateway.Body.Emit(OpCodes.Ldarg_1); break;
                            case 1: _gateway.Body.Emit(OpCodes.Ldarg_2); break;
                            case 2: _gateway.Body.Emit(OpCodes.Ldarg_3); break;
                            default: _gateway.Body.Emit(OpCodes.Ldarg_S, _gateway.Parameters[_index]); break;
                        }
                    }
                    _gateway.Body.Emit(OpCodes.Call, _authentic);
                    _gateway.Body.Emit(OpCodes.Ret);
                    method.Body = new MethodBody(method);
                    method.Body.Emit(OpCodes.Ldsfld, authority);
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
                    method.Body.Emit(OpCodes.Call, _gateway);
                    method.Body.Emit(OpCodes.Ret);
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
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _gateway.Body.Emit(OpCodes.Ldarg_1); break;
                            case 1: _gateway.Body.Emit(OpCodes.Ldarg_2); break;
                            case 2: _gateway.Body.Emit(OpCodes.Ldarg_3); break;
                            default: _gateway.Body.Emit(OpCodes.Ldarg_S, _gateway.Parameters[_index]); break;
                        }
                    }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _gateway.Body.Emit(OpCodes.Call, _method);
                    _gateway.Body.Emit(OpCodes.Ret);
                    method.Body = new MethodBody(method);
                    method.Body.Emit(OpCodes.Ldsfld, authority);
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
                    _method = new GenericInstanceMethod(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    method.Body.Emit(OpCodes.Call, _method);
                    method.Body.Emit(OpCodes.Ret);
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
                        _authentic.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _parameter.ParameterType));
                        _gateway.Parameters.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _parameter.ParameterType));
                    }
                    _authentic.Body = method.Body;
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _gateway.Body.Emit(OpCodes.Ldarg_1); break;
                            case 1: _gateway.Body.Emit(OpCodes.Ldarg_2); break;
                            case 2: _gateway.Body.Emit(OpCodes.Ldarg_3); break;
                            default: _gateway.Body.Emit(OpCodes.Ldarg_S, _gateway.Parameters[_index]); break;
                        }
                    }
                    _gateway.Body.Emit(OpCodes.Call, _authentic);
                    _gateway.Body.Emit(OpCodes.Ret);
                    method.Body = new MethodBody(method);
                    method.Body.Emit(OpCodes.Ldsfld, authority);
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
                    method.Body.Emit(OpCodes.Call, _gateway);
                    method.Body.Emit(OpCodes.Ret);
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
                    for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
                    {
                        switch (_index)
                        {
                            case 0: _gateway.Body.Emit(OpCodes.Ldarg_1); break;
                            case 1: _gateway.Body.Emit(OpCodes.Ldarg_2); break;
                            case 2: _gateway.Body.Emit(OpCodes.Ldarg_3); break;
                            default: _gateway.Body.Emit(OpCodes.Ldarg_S, _gateway.Parameters[_index]); break;
                        }
                    }
                    var _method = new GenericInstanceMethod(_authentic);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    _gateway.Body.Emit(OpCodes.Call, _method);
                    _gateway.Body.Emit(OpCodes.Ret);
                    method.Body = new MethodBody(method);
                    method.Body.Emit(OpCodes.Ldsfld, authority);
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
                    _method = new GenericInstanceMethod(_gateway);
                    foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                    method.Body.Emit(OpCodes.Call, _method);
                    method.Body.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
