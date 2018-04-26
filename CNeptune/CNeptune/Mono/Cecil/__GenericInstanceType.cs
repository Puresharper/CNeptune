using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
    static public class __GenericInstanceType
    {
        static public FieldReference Reference(this GenericInstanceType genericInstanceType, FieldReference field)
        {
            var _fieldReference = new FieldReference(field.Name, field.FieldType, genericInstanceType);
            return _fieldReference;
        }
        static public GenericInstanceMethod MakeGenericMethod(this MethodReference method, IEnumerable<TypeReference> genericArguments, IEnumerable<GenericParameter> genericParameters)
        {
            var _method = new GenericInstanceMethod(method);
            var _genericParameters = genericParameters.Array();
            foreach (var _argument in genericArguments)
            {
                //_method.GenericArguments.Add(genericArguments);
                var _newType = ReplaceGenericTypes(method, _argument, _genericParameters);
                if (_newType != null)
                {
                    _method.GenericArguments.Add(_newType);
                }
                else
                {
                    _method.GenericArguments.Add(_argument);
                }
            }
            return _method;
        }

        static public MethodReference Reference(this GenericInstanceType genericInstanceType, MethodReference method, IEnumerable<GenericParameter> genericParameters)
        {
            //todo resolve generic parameters
            var _genericParameters = genericParameters.Array();
            var _returnType = ReplaceGenericTypes(method, method.ReturnType, _genericParameters) ?? method.ReturnType;
            MethodReference _methodReference;
            GenericParameter[] _methodGenericParameters = genericParameters.Skip(method.DeclaringType.GenericParameters.Count).ToArray();
            if (_methodGenericParameters.Length != method.GenericParameters.Count) throw new ArgumentException("Wrong number of generic parameters", nameof(genericParameters));
            //if (!method.HasGenericParameters)
            {
                //new MethodDefinition()
                _methodReference = new MethodReference(method.Name, _returnType, genericInstanceType)
                {
                    HasThis = method.HasThis,
                    ExplicitThis = method.ExplicitThis,
                    CallingConvention = method.CallingConvention
                };
                foreach (var parameter in method.Parameters)
                {
                    var _newType = ReplaceGenericTypes(method, parameter.ParameterType, _genericParameters);
                    if (_newType != null)
                    {
                        _methodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes,
                            _newType));
                    }
                    else
                    {
                        _methodReference.Parameters.Add(parameter);
                    }
                }
            }
            _methodReference.DeclaringType = genericInstanceType;
            ////else
            if (method.HasGenericParameters)
            {
                //_methodReference = method.MakeGenericMethod(_methodGenericParameters);
                //var _method = new GenericInstanceMethod(method);
                var _method = new GenericInstanceMethod(_methodReference);
                foreach (var _parameter in _methodGenericParameters)
                {
                    var _newType = ReplaceGenericTypes(method, _parameter, _genericParameters);
                    if (_newType != null)
                    {
                        _method.GenericArguments.Add(_newType);
                    }
                    else
                    {
                        _method.GenericArguments.Add(_parameter);
                    }
                }

                return _method;
            }

            return _methodReference;
        }

        static private TypeReference ReplaceGenericTypes(MethodReference method, TypeReference type, GenericParameter[] genericParameters)
        {
            if (type is GenericParameter _genericParameter)
            {
                var _index = method.GenericParameters.IndexOf(_genericParameter);
                if (_index >= 0)
                {
                    _index += method.DeclaringType.GenericParameters.Count;
                }
                else
                {
                    _index = method.DeclaringType.GenericParameters.IndexOf(_genericParameter);
                }
                if (_index >= 0)
                {
                    return genericParameters[_index];
                }
                if (genericParameters.Contains(type))
                {
                    return type;
                }
                throw new InvalidOperationException();
            }

            if (type is GenericInstanceType _genericType)
            {
                List<TypeReference> _newArgumentTypes = null;
                for (var _index = 0; _index < _genericType.GenericArguments.Count; _index++)
                {
                    var _newArgumentType = ReplaceGenericTypes(method, _genericType.GenericArguments[_index], genericParameters);
                    if (_newArgumentType != null)
                    {
                        if (_newArgumentTypes == null) _newArgumentTypes = new List<TypeReference>();
                        for (int i = _newArgumentTypes.Count; i < _index; i++)
                        {
                            _newArgumentTypes.Add(_genericType.GenericArguments[i]);
                        }
                        _newArgumentTypes.Add(_newArgumentType);
                    }
                }

                if (_newArgumentTypes != null)
                {
                    for (int i = _newArgumentTypes.Count; i < _genericType.GenericArguments.Count; i++)
                    {
                        _newArgumentTypes.Add(_genericType.GenericArguments[i]);
                    }

                    return _genericType.ElementType.MakeGenericType(_newArgumentTypes);
                }
            }

            return null;
        }
    }
}
