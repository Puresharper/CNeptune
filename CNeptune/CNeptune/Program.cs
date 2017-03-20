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
        private const string Authority = "<Authority>";
        private const string Inject = "<Inject>";
        private const string Authentic = "<Authentic>";

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
            var _name = string.Concat(_assembly.Name.Name, ".", Program.Neptune);
            if (_assembly.CustomAttributes.Any(_Attribute => _Attribute.ConstructorArguments.Count == 1 && object.Equals(_Attribute.ConstructorArguments.First().Value, _name))) { return; }
            _assembly.Attribute(() => new InternalsVisibleToAttribute(_name));
            var _gateway = _module.Type(Program.Neptune, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NotPublic | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _cctor = _gateway.Initializer();
            _cctor.Body.Emit(OpCodes.Call, Metadata.Property(() => AppDomain.CurrentDomain).GetGetMethod());
            _cctor.Body.Emit(OpCodes.Ldstr, _name);
            _cctor.Body.Emit(OpCodes.Newobj, Metadata.Constructor(() => new System.Reflection.AssemblyName(Argument<string>.Value)));
            _cctor.Body.Emit(OpCodes.Ldc_I4, (int)System.Reflection.Emit.AssemblyBuilderAccess.Run);
            _cctor.Body.Emit(OpCodes.Call, Metadata<AppDomain>.Method(_AppDomain => _AppDomain.DefineDynamicAssembly(Argument<System.Reflection.AssemblyName>.Value, Argument<System.Reflection.Emit.AssemblyBuilderAccess>.Value)));
            _cctor.Body.Emit(OpCodes.Ldstr, _name);
            _cctor.Body.Emit(OpCodes.Ldc_I4_1);
            _cctor.Body.Emit(OpCodes.Call, Metadata<System.Reflection.Emit.AssemblyBuilder>.Method(_AssemblyBuilder => _AssemblyBuilder.DefineDynamicModule(Argument<string>.Value, Argument<bool>.Value)));
            _cctor.Body.Emit(OpCodes.Stsfld, _gateway.Field<System.Reflection.Emit.ModuleBuilder>(Program.Module, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName));
            _cctor.Body.Emit(OpCodes.Ret);
            var _inject = _gateway.Method(Program.Inject, MethodAttributes.Static | MethodAttributes.Public);
            _inject.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, _module.Import(typeof(System.Reflection.Emit.DynamicMethod))));

            //TODO !
            //var gg = find original method by name
            //var type = mod.DefineType(Guid.NewGuid().ToString("N"), System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class, baseType);
            //var gg1 = t.DefineMethod(gg.Name, System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.NewSlot | System.Reflection.MethodAttributes.ReuseSlot, gg.ReturnType, gg.GetParameters().Select(p => p.ParameterType).ToArray());
            //var body = gg1.GetILGenerator();
            // //emit all parameter but not ldarg_0! (check input method parameter count)
            //body.Emit(OpCodes.Ldarg_1);
            //body.Emit(OpCodes.Ldarg_2);
            //body.Emit(OpCodes.Ldarg_3);
            //body.Emit(OpCodes.ldc_I4? pointer); //swicth on Inptr.size
            //body.EmitCalli(OpCodes.Calli, System.Reflection.CallingConventions.Standard, typeof(int), Type.EmptyTypes, null);
            //body.Emit(OpCodes.Ret);
            //t.DefineMethodOverride(gg1, gg);
            //var ght = t.CreateType();
            //var nnn = ght.GetMethod(gg.Name);
            //RuntimeHelpers.PrepareMethod(nnn.MethodHandle);
            //f.SetValue(null, Activator.CreateInstance(ght));


            _inject.Body.Emit(OpCodes.Ret);



            var _authentic = _gateway.Method<System.Reflection.MethodBase>(Program.Authentic, MethodAttributes.Static | MethodAttributes.Public);
            _authentic.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, _module.Import(typeof(System.Reflection.MethodBase))));

            //TODO!

            _authentic.Body.Emit(OpCodes.Ldnull);
            _authentic.Body.Emit(OpCodes.Ret);



            foreach (var _type in _module.GetTypes().ToArray()) { Program.Manage(_gateway, _type); }
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true });
        }
        
        static private void Manage(TypeDefinition gateway, TypeDefinition authentic)
        {
            if (authentic.IsInterface || authentic.IsValueType) { return; }
            if (authentic.Name == Program.Module) { return; }
            if (authentic.Name == Program.Neptune) { return; }
            var _authentic = authentic.Type(string.Concat(Program.Neptune), TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            var _gateway = gateway.Type(string.Concat("<", authentic.Identity(), ">"), TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
            foreach (var _parameter in authentic.GenericParameters) { _gateway.GenericParameters.Add(_parameter.Copy(_gateway)); }
            var _ctor = _gateway.Constructor();
            _ctor.Body.Emit(OpCodes.Ldarg_0);
            _ctor.Body.Emit(OpCodes.Call, Metadata.Constructor(() => new object()));
            _ctor.Body.Emit(OpCodes.Ret);
            var _authority = _gateway.Field(Program.Authority, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName, _gateway);
            var _cctor = _gateway.Initializer();
            _cctor.Body.Emit(OpCodes.Newobj, _ctor);
            _cctor.Body.Emit(OpCodes.Stsfld, _authority);
            _cctor.Body.Emit(OpCodes.Ret);
            foreach (var _method in authentic.Methods.ToArray()) { Program.Manage(_gateway, _authentic, _authority, _method); }
        }

        static private void Manage(TypeDefinition gateway, TypeDefinition authentic, FieldDefinition authority, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            var _gateway = gateway.Method(string.Concat("<", method.IsConstructor ? method.DeclaringType.Name : method.Name, ">"), MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Public, method.ReturnType);
            var _authentic = authentic.Method(string.Concat("<", method.IsConstructor ? method.DeclaringType.Name : method.Name, ">"), MethodAttributes.Static | MethodAttributes.Public, method.ReturnType);
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
                    method.Body.Emit(OpCodes.Callvirt, _gateway);
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
                    method.Body.Emit(OpCodes.Callvirt, _method);
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
                    method.Body.Emit(OpCodes.Callvirt, _gateway);
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
                    method.Body.Emit(OpCodes.Callvirt, _method);
                    method.Body.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
