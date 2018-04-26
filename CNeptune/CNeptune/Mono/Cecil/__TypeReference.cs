using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Cecil
{
    static internal class __TypeReference
    {
        static public bool IsAssignableFrom(this TypeReference type, TypeDefinition speculative)
        {
            var _type = type.Resolve();
            if (speculative.BaseType != null && speculative.BaseType.Resolve() == _type) { return true; }
            if (speculative.Interfaces.Any(_Type => _Type.Resolve() == _type)) { return true; }
            return false;
        }

        static public GenericInstanceType MakeGenericType(this TypeReference type, IEnumerable<TypeReference> arguments)
        {
            var _type = new GenericInstanceType(type);
            foreach (var _argument in arguments) { _type.GenericArguments.Add(_argument); }
            return _type;
        }
    }
}
