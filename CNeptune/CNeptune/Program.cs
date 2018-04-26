using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using CNeptuneBase.ImplementationDetails;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using MethodBase = System.Reflection.MethodBase;
using MethodInfo = System.Reflection.MethodInfo;

[assembly: DebuggerDisplay("{AttributeType.FullName}", Target = typeof(Mono.Cecil.CustomAttribute))]

namespace CNeptune
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        public const string Module = "<Module>";
        public const string Pointer = "<Pointer>";

        static private readonly MethodInfo GetMethodHandle = Metadata<MethodBase>.Property(_Method => _Method.MethodHandle).GetGetMethod();
        static private readonly MethodInfo GetFunctionPointer = Metadata<RuntimeMethodHandle>.Method(_Method => _Method.GetFunctionPointer());
        static private readonly MethodInfo CreateDelegate = Metadata.Method(() => Delegate.CreateDelegate(Argument<Type>.Value, Argument<MethodInfo>.Value));
        static private readonly MethodInfo GetTypeFromHandle = System.Metadata.Method(() => Type.GetTypeFromHandle(Argument<RuntimeTypeHandle>.Value));

        static public void Main(string[] arguments)
        {
            // If true do not overwrite input files
            bool _dryRun = false;
            if (arguments == null) { throw new ArgumentNullException(); }
            // An option for ease of debugging CNeptune where we don't overwrite input so we can do successive debug runs
            if (arguments.Length > 0 && arguments[0].ToLower() == "/dryrun")
            {
                _dryRun = true;
                arguments = arguments.Skip(1).ToArray();
            }
            switch (arguments.Length)
            {
                case 1:
                    Program.Manage(arguments[0], _dryRun);
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
                                    case "Library": Program.Manage(string.Concat(_directory, _element.Value, _name, ".dll"), _dryRun); return;
                                    case "WinExe":
                                    case "Exe": Program.Manage(string.Concat(_directory, _element.Value, _name, ".exe"), _dryRun); return;
                                    default: throw new NotSupportedException($"Unknown OutputType: {_type.Value}");
                                }
                            }
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        static private void Manage(string assembly, bool dryRun)
        {
            const string _pdbExtension = ".pdb";
            if (dryRun)
            {
                var _tempDllFileName = Path.ChangeExtension(assembly, ".temp" + Path.GetExtension(assembly));
                var _tempPdbFileName = Path.ChangeExtension(_tempDllFileName, _pdbExtension);
                if (File.Exists(_tempDllFileName)) File.Delete(_tempDllFileName);
                if (File.Exists(_tempPdbFileName)) File.Delete(_tempPdbFileName);
                File.Copy(assembly, _tempDllFileName);
                var _pdbFile = Path.ChangeExtension(assembly, _pdbExtension);
                if (File.Exists(_pdbFile))
                {
                    File.Copy(_pdbFile, _tempPdbFileName);
                }
                assembly = _tempDllFileName;
            }
            var _resolver = new DefaultAssemblyResolver();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(assembly));
            var _hasPdbFile = File.Exists(Path.ChangeExtension(assembly, _pdbExtension));
            var _readerParameters = new ReaderParameters() { AssemblyResolver = _resolver, ReadSymbols = _hasPdbFile, ReadingMode = ReadingMode.Immediate };
            var _assembly = AssemblyDefinition.ReadAssembly(assembly, _readerParameters);
            var _isManaged = IsManagedByNeptune(_assembly);
            var _module = _assembly.MainModule;
            var _hasNeptuneMethods = false;
            foreach (var _type in _module.GetTypes().ToArray())
            {
                _hasNeptuneMethods |= Program.Manage(_type, _isManaged ?? true);
            }
            if (_hasNeptuneMethods)
            {
                _assembly.Attribute<HasNeptuneMethodsAttribute>();
                _assembly.Write(assembly, new WriterParameters { WriteSymbols = true  });
            }
        }
        #region NeptuneAttribute
        private static bool? IsManagedByNeptune(AssemblyDefinition assembly)
        {
            var neptuneAttribute = assembly.CustomAttributes.FirstOrDefault(a => IsSameType(a.AttributeType, Metadata<NeptuneAttribute>.Type));
            var value = neptuneAttribute?.ConstructorArguments[0].Value;
            return (bool?) value;
        }

        private static bool? IsManagedByNeptune(TypeDefinition type)
        {
            var neptuneAttribute = type.CustomAttributes.FirstOrDefault(a => IsSameType(a.AttributeType, Metadata<NeptuneAttribute>.Type));
            var value = neptuneAttribute?.ConstructorArguments[0].Value;
            return (bool?) value;
        }

        private static bool? IsManagedByNeptune(MethodDefinition method)
        {
            var neptuneAttribute = method.CustomAttributes.FirstOrDefault(a => IsSameType(a.AttributeType, Metadata<NeptuneAttribute>.Type));
            var value = neptuneAttribute?.ConstructorArguments[0].Value;
            return (bool?) value;
        }

        private static bool IsSameType(TypeReference a, Type b)
        {
            return a.FullName == b.FullName;
        }
        #endregion

        static private bool Bypass(TypeDefinition type)
        {
            return type.IsInterface || type.IsValueType || type.Name == Program.Module || (type.BaseType != null && type.BaseType.Resolve() == type.Module.Import(typeof(MulticastDelegate)).Resolve());
        }

        static private bool Bypass(MethodDefinition method)
        {
            return method.Body == null || (method.IsConstructor && method.IsStatic);
        }

        static private bool Manage(TypeDefinition type, bool defaultTypeIsManaged)
        {
            foreach (var _type in type.NestedTypes) { if (_type.Name == Program.Neptune) { throw new InvalidOperationException("Assembly already rewritten by CNeptune"); } }
            if (Program.Bypass(type)) { return false; }
            //todo Jens what about testing nested methods for NeptuneAttribute?
            bool? _typeIsManaged = null;
            for (var t = type; t != null && !_typeIsManaged.HasValue; t = t.DeclaringType)
            {
                _typeIsManaged = IsManagedByNeptune(t);
            }
            var _defaultMethodIsManaged = _typeIsManaged ?? defaultTypeIsManaged;
            var _neptuneMethodCount = 0;
            MethodDefinition _initializer = null;
            foreach (var _method in type.Methods.ToArray())
            {
                if (_method.IsConstructor && _method.IsStatic) { _initializer = _method; }
                if (Program.Bypass(_method)) { continue; }
                if (!(IsManagedByNeptune(_method) ?? _defaultMethodIsManaged)) { continue; }
                Program.Manage(_method, _neptuneMethodCount);
                if (type.HasGenericParameters || _method.HasGenericParameters) { _neptuneMethodCount++; }
            }
            if (_neptuneMethodCount > 0)
            {
                type.Attribute<HasNeptuneMethodsAttribute>();
                if (_initializer != null)
                {
                    InsertTypeInstantiatedCall(type, _initializer);
                }
                else
                {
                    _initializer = type.Initializer();
                    InsertTypeInstantiatedCall(type, _initializer);
                    _initializer.Body.Emit(OpCodes.Ret);
                }
                type.Field("<NeptuneMethodCount>", FieldAttributes.Public, _neptuneMethodCount);
            }

            return true;
        }

        private static void InsertTypeInstantiatedCall(TypeDefinition type, MethodDefinition initializer)
        {
            var typeInstantiatedMethodReference = initializer.Module.Import(typeof(CNeptuneBase.InstantiationListener).GetMethod(nameof(CNeptuneBase.InstantiationListener.TypeInstantiating)));
            var _type = type.MakeGenericType(type.GenericParameters);
            initializer.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldtoken, _type));
            initializer.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, initializer.DeclaringType.Module.Import(Program.GetTypeFromHandle)));
            initializer.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, initializer.DeclaringType.Module.Import(typeInstantiatedMethodReference)));
        }

        static private TypeDefinition Authority(this TypeDefinition type)
        {
            foreach (var _type in type.NestedTypes) { if (_type.Name == Program.Neptune) { return _type; } }
            return type.Type(Program.Neptune, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
        }

        static private TypeDefinition Authority(this TypeDefinition type, string name)
        {
            var _authority = type.Authority();
            foreach (var _type in _authority.NestedTypes) { if (_type.Name == name) { return _type; } }
            return _authority.Type(name, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
        }
        
        static private MethodDefinition Authentic(this MethodDefinition method, int neptuneMethodIndex)
        {
            var _type = method.DeclaringType.Authority("<Authentic>");
            var _copy = new Copy(method);
            var _method = _type.Method(method.IsConstructor ? "<Constructor>" : method.Name, MethodAttributes.Static | MethodAttributes.Public);
            _copy.TypeGenericity = _type.GenericParameters.ToArray();
            foreach (var _parameter in method.GenericParameters) { _method.GenericParameters.Add(_parameter.Copy(_method)); }
            _copy.MethodGenericity = _method.GenericParameters.ToArray();
            _method.ReturnType = _copy[method.ReturnType];
            if (method.HasGenericParameters && _type.HasGenericParameters)
            {
                _method.Attribute(() => new NeptuneMethodIndexAttribute(neptuneMethodIndex));
            }
            if (!method.IsStatic)
            {
                TypeReference _parameterType = method.DeclaringType;
                if (_parameterType.HasGenericParameters)
                {
                    _parameterType = _parameterType.MakeGenericType(_type.GenericParameters);
                }
                _method.Parameters.Add(new ParameterDefinition("this", ParameterAttributes.None, _parameterType));
            }
            foreach (var _parameter in method.Parameters) { _method.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _copy[_parameter.ParameterType])); }
            _copy.Signature = _method.Parameters.ToArray();
            var _body = _method.Body;
            _body.InitLocals = method.Body.InitLocals;
            foreach (var _variable in method.Body.Variables) { _body.Add(new VariableDefinition(_variable.Name, _copy[_variable.VariableType])); }
            _copy.Variation = _body.Variables.ToArray();
            foreach (var _instruction in method.Body.Instructions) { _body.Instructions.Add(_copy[_instruction]); }

            //TODO : for virtual method => replace base call to "pure path"!
            if (method.IsVirtual)
            {
                //lookup base call to same method definition and swap it to direct base authentic call!
                //it will allow to wrap the entire virtual call.
            }

            foreach (var _exception in method.Body.ExceptionHandlers)
            {
                _body.ExceptionHandlers.Add(new ExceptionHandler(_exception.HandlerType)
                {
                    CatchType = _exception.CatchType,
                    TryStart = _copy[_exception.TryStart],
                    TryEnd = _copy[_exception.TryEnd],
                    HandlerType = _exception.HandlerType,
                    HandlerStart = _copy[_exception.HandlerStart],
                    HandlerEnd = _copy[_exception.HandlerEnd]
                });
            }
            return _method;
        }

        static private FieldDefinition Intermediate(this MethodDefinition method, MethodDefinition authentic, int neptuneMethodIndex)
        {
            var _intermediate = method.DeclaringType.Authority("<Intermediate>").Type(method.IsConstructor ? $"<<Constructor>>" : $"<{method.Name}>", TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            foreach (var _parameter in method.GenericParameters) { _intermediate.GenericParameters.Add(new GenericParameter(_parameter.Name, _intermediate)); }
            var _field = _intermediate.Field<IntPtr>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public);
            FieldReference _fieldReference = _field;
            MethodReference _authenticReference = authentic;
            GenericInstanceType _authenticGenericType = null;
            if (_intermediate.HasGenericParameters)
            {
                _intermediate.Field("<NeptuneMethodIndex>", FieldAttributes.Public, neptuneMethodIndex);
                var _intermediateGenericType = _intermediate.MakeGenericType(_intermediate.GenericParameters);
                _fieldReference = _intermediateGenericType.Reference(_fieldReference);
                if (method.DeclaringType.HasGenericParameters)
                {
                    var _typeParameters = _intermediate.GenericParameters.Take(method.DeclaringType.GenericParameters.Count).ToArray();
                    var _methodParameters = _intermediate.GenericParameters.Skip(method.DeclaringType.GenericParameters.Count).ToArray();
                    _authenticGenericType = authentic.DeclaringType.MakeGenericType(_typeParameters);
                    _authenticReference = _authenticGenericType.Reference(authentic, _intermediate.GenericParameters);
                }
                else if (method.HasGenericParameters)
                {
                    _authenticReference = authentic.MakeGenericMethod(_intermediate.GenericParameters);
                }
            }
            var _initializer = _intermediate.Initializer();
            var _variable = _initializer.Body.Variable<RuntimeMethodHandle>();
            _initializer.Body.Variable<Func<IntPtr>>();
            // todo Jens fix generic method in generic type to us ldtoken
            if (method.DeclaringType.HasGenericParameters && method.HasGenericParameters)
            {
                // work around for ldtoken _authenticReference not working correctly for generic method in generic type
                var methodInstantiatingMethodReference = _initializer.Module.Import(typeof(CNeptuneBase.InstantiationListener).GetMethod(nameof(CNeptuneBase.InstantiationListener.MethodInstantiating)));
                var _intermediateReference = _intermediate.MakeGenericType(_intermediate.GenericParameters);
                _initializer.Body.Emit(OpCodes.Ldtoken, _intermediateReference);
                _initializer.Body.Emit(OpCodes.Ldtoken, _authenticGenericType);
                _initializer.Body.Emit(OpCodes.Call, _initializer.DeclaringType.Module.Import(Program.GetTypeFromHandle));
                _initializer.Body.Emit(OpCodes.Call, _initializer.DeclaringType.Module.Import(methodInstantiatingMethodReference));
            }
            else
            {
                _initializer.Body.Emit(_authenticReference);
            }
            _initializer.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
            _initializer.Body.Emit(OpCodes.Stloc_0);
            _initializer.Body.Emit(OpCodes.Ldloca_S, _variable);
            _initializer.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
            _initializer.Body.Emit(OpCodes.Stsfld, _fieldReference);
            if (_intermediate.HasGenericParameters)
            {
                var methodInstantiatedMethodReference = _initializer.Module.Import(typeof(CNeptuneBase.InstantiationListener).GetMethod(nameof(CNeptuneBase.InstantiationListener.MethodInstantiated)));
                var _intermediateReference = _intermediate.MakeGenericType(_intermediate.GenericParameters);
                _initializer.Body.Emit(OpCodes.Ldtoken, _intermediateReference);
                _initializer.Body.Emit(OpCodes.Call, _initializer.DeclaringType.Module.Import(Program.GetTypeFromHandle));
                _initializer.Body.Emit(OpCodes.Call, _initializer.DeclaringType.Module.Import(methodInstantiatedMethodReference));
            }

            //TODO : IOC of AOP !? What the? in fact it will be used to be able to inject on method on demand but a late as possible.
            //Action<MethodBase> _update;
            //lock (AppDomain.CurrentDomain.Evidence.SyncRoot) { _update = AppDomain.CurrentDomain.GetData("<Neptune<Update>>") as Action<MethodBase>; }
            //if (_update != null) { _update(...); }
            _initializer.Body.Emit(OpCodes.Ret);
            _initializer.Body.OptimizeMacros();
            return _field;
        }

        static private void Manage(this MethodDefinition method, int neptuneMethodIndex)
        {
            var _authentic = method.Authentic(neptuneMethodIndex);
            FieldReference _intermediate = method.Intermediate(_authentic, neptuneMethodIndex);
            method.Body = new MethodBody(method);
            for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
            {
                switch (_index)
                {
                    case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                    case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                    case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                    case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                    default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[method.IsStatic ? _index : _index - 1]); break;
                }
            }
            MethodReference _authenticReference = _authentic;
            if (method.DeclaringType.HasGenericParameters || method.HasGenericParameters)
            {
                method.Attribute(() => new NeptuneMethodIndexAttribute(neptuneMethodIndex));
                var _genericParameters = method.DeclaringType.GenericParameters.Concat(method.GenericParameters).ToArray();
                var _genericInstanceType = _intermediate.DeclaringType.MakeGenericType(_genericParameters);
                _intermediate = _genericInstanceType.Reference(_intermediate);
                if (method.DeclaringType.HasGenericParameters)
                {
                    _authenticReference = _genericInstanceType.Reference(_authenticReference, _genericParameters);
                }
                else if (method.HasGenericParameters)
                {
                    _authenticReference = _authenticReference.MakeGenericMethod(method.GenericParameters, _genericParameters);
                }
            }
            method.Body.Emit(OpCodes.Ldsfld, _intermediate);
            method.Body.Emit(OpCodes.Calli, _authenticReference.ReturnType, _authenticReference.Parameters);
            method.Body.Emit(OpCodes.Ret);
            method.Body.OptimizeMacros();

            ////async
            //if (method.Module.Import(typeof(IAsyncStateMachine)).IsAssignableFrom(method.DeclaringType) && method.Name == "MoveNext") // TODO : change method name test by reliable IAsyncStateMachine.MoveNext resolution.
            //{
            //    //add static field to intermediate as pointer to produce Tuple<IntPtr, IntPtr, IntPtr, IntPtr>!
            //    //add constructor to state machine to force initialize 4 fantastics by calling intermediate!
            //    //add instance field to state machine <Intermediate>
            //    //pointer to call resume:(AsyncTaskMethodBuilder, int state) / setresult(AsyncTaskMethodBuilder, int state) / setexception(AsyncTaskMethodBuilder, int state) / AwaitUnsafeOnCompleted(AsyncTaskMethodBuilder, int)
            //}
        }
    }
}
