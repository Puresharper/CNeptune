using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    static internal class __Dictionary
    {
        static public TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : class
        {
            TValue _value;
            if (dictionary.TryGetValue(key, out _value)) { return _value; }
            return _value;
        }
    }
}
