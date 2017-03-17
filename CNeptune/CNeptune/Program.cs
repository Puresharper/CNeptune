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

namespace CNeptune
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        private const string Module = "<Module>";

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
            if (_assembly.CustomAttributes.Any(_Attribute => object.Equals(_Attribute.ConstructorArguments.First().Value, Program.Neptune))) { return; }

            //TODO Auto-Mod-lookup
            var _type = _module.GetType(null, Program.Module);
            var _neptune = new FieldDefinition(Program.Neptune, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName, _module.Import(typeof(System.Reflection.Emit.ModuleBuilder)));
            _type.Fields.Add(_neptune);

            //CREATE METHOD to find trusted module builder
            //var a = typeof(ClassLibrary2.Class1);
            //var m = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("dd"), System.Reflection.Emit.AssemblyBuilderAccess.Run).DefineDynamicModule("blabla");
            //var t = m.DefineType("gg", System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public, typeof(object));
            //t.DefineField("Module", typeof(ModuleBuilder), System.Reflection.FieldAttributes.Static | System.Reflection.FieldAttributes.Public);
            //var gg = t.CreateType();
            //gg.GetField("Module").SetValue(null, m);

            var _attribute = new CustomAttribute(_module.Import(typeof(InternalsVisibleToAttribute).GetConstructor(new Type[] { typeof(string) })));
            _attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.Import(typeof(string)), Program.Neptune));
            _assembly.CustomAttributes.Add(_attribute);
            Program.Manage(_module);
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true });
        }

        static private void Manage(ModuleDefinition module)
        {
            foreach (var _type in module.Types.ToArray()) { Program.Manage(_type); }
        }

        static private void Manage(TypeDefinition type)
        {
            if (type.IsInterface || type.IsValueType) { return; }
            if (type.Name == Program.Module) { return; }
            foreach (var _nested in type.NestedTypes) { Program.Manage(_nested); }
            if (type.Attributes.HasFlag(TypeAttributes.NestedPrivate)) { type.Attributes = (type.Attributes & ~TypeAttributes.NestedPrivate) | TypeAttributes.NestedAssembly; }
            else if (type.Attributes.HasFlag(TypeAttributes.NestedFamily)) { type.Attributes = (type.Attributes & ~TypeAttributes.NestedFamily) | TypeAttributes.NestedFamORAssem; }
            foreach (var _field in type.Fields.ToArray()) { Program.Manage(_field); }
            var _module = type.Module;
            var _neptune = new TypeDefinition(null, string.Concat(Program.Neptune), TypeAttributes.Class | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName, _module.Import(typeof(object)));
            var _attribute = new CustomAttribute(_module.Import(typeof(EditorBrowsableAttribute).GetConstructor(new Type[] { typeof(EditorBrowsableState) })));
            _attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.Import(typeof(EditorBrowsableState)), EditorBrowsableState.Never));
            _neptune.CustomAttributes.Add(_attribute);
            _attribute = new CustomAttribute(_module.Import(typeof(BrowsableAttribute).GetConstructor(new Type[] { typeof(bool) })));
            _attribute.ConstructorArguments.Add(new CustomAttributeArgument(_module.Import(typeof(bool)), false));
            _neptune.CustomAttributes.Add(_attribute);
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            var _ctor = new MethodDefinition(".ctor", methodAttributes, _module.Import(typeof(void)));
            var _body = _ctor.Body.GetILProcessor();
            _body.Append(_body.Create(OpCodes.Ldarg_0));
            _body.Append(_body.Create(OpCodes.Call, _module.Import(typeof(object).GetConstructor(Type.EmptyTypes))));
            _body.Append(_body.Create(OpCodes.Ret));
            _neptune.Methods.Add(_ctor);
            var _authority = new FieldDefinition("Authority", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName, _neptune);
            _neptune.Fields.Add(_authority);
            var _cctor = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName, _module.Import(typeof(void)));
            _body = _cctor.Body.GetILProcessor();
            _body.Append(_body.Create(OpCodes.Newobj, _ctor));
            _body.Append(_body.Create(OpCodes.Stsfld, _authority));
            _body.Append(_body.Create(OpCodes.Ret));
            _neptune.Methods.Add(_cctor);
            foreach (var _method in type.Methods.ToArray()) { Program.Manage(_neptune, _authority, _method); }
            type.NestedTypes.Add(_neptune);
        }

        static private void Manage(FieldDefinition field)
        {
            if (field.Attributes.HasFlag(FieldAttributes.Private)) { field.Attributes = (field.Attributes & ~FieldAttributes.Private) | FieldAttributes.Assembly; }
            else if (field.Attributes.HasFlag(FieldAttributes.Family)) { field.Attributes = (field.Attributes & ~FieldAttributes.Family) | FieldAttributes.FamORAssem; }
        }

        static private void Manage(TypeDefinition neptune, FieldDefinition authority, MethodDefinition method)
        {
            if (method.IsConstructor && method.IsStatic) { return; }
            if (method.Attributes.HasFlag(MethodAttributes.Private)) { method.Attributes = (method.Attributes & ~MethodAttributes.Private) | MethodAttributes.Assembly; }
            else if (method.Attributes.HasFlag(MethodAttributes.Family)) { method.Attributes = (method.Attributes & ~MethodAttributes.Family) | MethodAttributes.FamORAssem; }
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
                    _body.Append(_body.Create(OpCodes.Ldsfld, authority));
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
                    _body.Append(_body.Create(OpCodes.Ldsfld, authority));
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
                    _body.Append(_body.Create(OpCodes.Ldsfld, authority));
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
                    _body.Append(_body.Create(OpCodes.Ldsfld, authority));
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
            neptune.Methods.Add(_gateway);
            neptune.Methods.Add(_authentic);
        }
    }
}
