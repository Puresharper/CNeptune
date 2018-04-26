﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Mono.Cecil
{
    static internal class __TypeDefinition
    {
        static public FieldDefinition Field(this TypeDefinition type, string name, FieldAttributes attributes, TypeReference @return)
        {
            var _field = new FieldDefinition(name, attributes, @return);
            type.Fields.Add(_field);
            _field.Attribute<CompilerGeneratedAttribute>();
            _field.Attribute(() => new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never));
            return _field;
        }

        static public FieldDefinition Field(this TypeDefinition type, string name, FieldAttributes attributes, Type @return)
        {
            var _field = new FieldDefinition(name, attributes, type.Module.Import(@return));
            type.Fields.Add(_field);
            _field.Attribute<CompilerGeneratedAttribute>();
            _field.Attribute(() => new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never));
            return _field;
        }

        static public FieldDefinition Field<T>(this TypeDefinition type, FieldAttributes attributes)
        {
            var _field = new FieldDefinition(string.Concat("<", typeof(T).Name, ">"), attributes, type.Module.Import(typeof(T)));
            type.Fields.Add(_field);
            _field.Attribute<CompilerGeneratedAttribute>();
            _field.Attribute(() => new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never));
            return _field;
        }

        static public FieldDefinition Field<T>(this TypeDefinition type, string name, FieldAttributes attributes)
        {
            var _field = new FieldDefinition(name, attributes, type.Module.Import(typeof(T)));
            type.Fields.Add(_field);
            _field.Attribute<CompilerGeneratedAttribute>();
            _field.Attribute(() => new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never));
            return _field;
        }

        static public MethodDefinition Method(this TypeDefinition type, string name, MethodAttributes attributes, TypeReference @return)
        {
            var _method = new MethodDefinition(name, attributes, @return);
            type.Methods.Add(_method);
            //_method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public MethodDefinition Method(this TypeDefinition type, string name, MethodAttributes attributes, Type @return)
        {
            var _method = new MethodDefinition(name, attributes, type.Module.Import(@return));
            type.Methods.Add(_method);
            //_method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public MethodDefinition Method(this TypeDefinition type, string name, MethodAttributes attributes)
        {
            var _method = new MethodDefinition(name, attributes, type.Module.TypeSystem.Void);
            type.Methods.Add(_method);
            //_method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public MethodDefinition Method<T>(this TypeDefinition type, string name, MethodAttributes attributes)
        {
            var _method = new MethodDefinition(name, attributes, type.Module.Import(typeof(T)));
            type.Methods.Add(_method);
            //_method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public MethodDefinition Method<T>(this TypeDefinition type, MethodAttributes attributes)
        {
            var _method = new MethodDefinition(string.Concat("<", typeof(T).Name, ">"), attributes, type.Module.Import(typeof(T)));
            type.Methods.Add(_method);
            //_method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public TypeDefinition Type(this TypeDefinition type, string name, TypeAttributes attributes)
        {
            var _type = new TypeDefinition(null, name, attributes, type.Module.TypeSystem.Object);
            type.NestedTypes.Add(_type);
            _type.Attribute<CompilerGeneratedAttribute>();
            _type.Attribute<SerializableAttribute>();
            foreach (var parameter in type.GenericParameters)
                _type.GenericParameters.Add(parameter.Copy(_type));
            return _type;
        }

        static public CustomAttribute Attribute<T>(this TypeDefinition type)
            where T : Attribute
        {
            var _attribute = new CustomAttribute(type.Module.Import(typeof(T).GetConstructor(System.Type.EmptyTypes)));
            type.CustomAttributes.Add(_attribute);
            return _attribute;
        }

        static public CustomAttribute Attribute<T>(this TypeDefinition type, Expression<Func<T>> expression)
            where T : Attribute
        {
            var _constructor = (expression.Body as NewExpression).Constructor;
            var _attribute = new CustomAttribute(type.Module.Import(_constructor));
            foreach (var _argument in (expression.Body as NewExpression).Arguments) { _attribute.ConstructorArguments.Add(new CustomAttributeArgument(type.Module.Import(_argument.Type), Expression.Lambda<Func<object>>(Expression.Convert(_argument, Metadata<object>.Type)).Compile()())); }
            type.CustomAttributes.Add(_attribute);
            return _attribute;
        }

        static public MethodDefinition Constructor(this TypeDefinition type)
        {
            var _method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, type.Module.TypeSystem.Void);
            type.Methods.Add(_method);
            _method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public MethodDefinition Initializer(this TypeDefinition type)
        {
            var _method = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, type.Module.TypeSystem.Void);
            type.Methods.Add(_method);
            _method.Attribute<CompilerGeneratedAttribute>();
            //_method.Attribute<DebuggerHiddenAttribute>();
            return _method;
        }

        static public GenericInstanceType MakeGenericType(this TypeDefinition type, IEnumerable<GenericParameter> arguments)
        {
            var _type = new GenericInstanceType(type);
            foreach (var _argument in arguments) { _type.GenericArguments.Add(_argument); }
            return _type;
        }
    }
}
