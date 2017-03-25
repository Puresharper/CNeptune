using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mono.Cecil
{
    static internal class __MethodDefinition
    {
        static public CustomAttribute Attribute<T>(this MethodDefinition method)
            where T : Attribute
        {
            var _attribute = new CustomAttribute(method.Module.Import(typeof(T).GetConstructor(System.Type.EmptyTypes)));
            method.CustomAttributes.Add(_attribute);
            return _attribute;
        }

        static public CustomAttribute Attribute<T>(this MethodDefinition method, Expression<Func<T>> expression)
            where T : Attribute
        {
            var _constructor = (expression.Body as NewExpression).Constructor;
            var _attribute = new CustomAttribute(method.Module.Import(_constructor));
            foreach (var _argument in (expression.Body as NewExpression).Arguments) { _attribute.ConstructorArguments.Add(new CustomAttributeArgument(method.Module.Import(_argument.Type), Expression.Lambda<Func<object>>(Expression.Convert(_argument, Metadata<object>.Type)).Compile()())); }
            method.CustomAttributes.Add(_attribute);
            return _attribute;
        }

        static public void Parameter<T>(this MethodDefinition method)
        {
            method.Parameters.Add(new ParameterDefinition(string.Concat("<", typeof(T).Name, ">"), ParameterAttributes.None, method.Module.Import(typeof(T))));
        }

        static public void Parameter<T>(this MethodDefinition method, string name)
        {
            method.Parameters.Add(new ParameterDefinition(name, ParameterAttributes.None, method.Module.Import(typeof(T))));
        } 
    }
}
