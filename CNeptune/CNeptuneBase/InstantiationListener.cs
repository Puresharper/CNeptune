using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using CNeptune;
using CNeptuneBase.ImplementationDetails;

namespace CNeptuneBase
{
    static public class InstantiationListener
    {
        static private readonly object m_AddLock = new object();
        static private readonly ConcurrentQueue<Type> m_Types = new ConcurrentQueue<Type>();
        static private TypeInstantiatedListener m_listener;
        static private bool m_Instantiating;
        static private bool m_Instantiated;

        public delegate void TypeInstantiatedListener(Type type);
        static public TypeInstantiatedListener Listener
        {
            get
            {
                return m_listener;
            }
            set
            {
                lock (m_AddLock)
                {
                    if (m_listener != null) throw new NotImplementedException();
                    if (value == null) throw new NotImplementedException();
                    m_listener = value;
                }

                while (m_Types.TryDequeue(out var _type))
                {
                    m_listener(_type);
                }

                lock (m_AddLock)
                {
                    m_Instantiated = true;
                }

                while (m_Types.TryDequeue(out var _type))
                {
                    m_listener(_type);
                }
            }
        }

        static public MethodBase MethodInstantiating(Type intermediateType, Type authenticType)
        {
            var _neptuneMethodIndex = (int)intermediateType.GetField("<NeptuneMethodIndex>").GetValue(null);
            var _found = false;
            var _foundMembers = authenticType.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Static, (_Member, _Criteria) =>
            {
                if (_found) { return false; }
                if (((MethodInfo) _Member).IsGenericMethod)
                {
                    var _neptuneMethodIndexAttribute = _Member.GetCustomAttributes(typeof(NeptuneMethodIndexAttribute), true);
                    if (_neptuneMethodIndexAttribute.Length > 1) throw new InvalidOperationException("CNeptune Internal error");
                    _found = _neptuneMethodIndexAttribute.Length > 0 && _neptuneMethodIndex == ((NeptuneMethodIndexAttribute) _neptuneMethodIndexAttribute[0]).NeptuneMethodIndex;
                }

                return _found;
            }, null);
            var _intermediateArguments = intermediateType.GetGenericArguments();
            var _authenticArguments = authenticType.GetGenericArguments();
            var _methodArguments = new Type[_intermediateArguments.Length - _authenticArguments.Length];
            Array.Copy(_intermediateArguments, _authenticArguments.Length, _methodArguments, 0, _methodArguments.Length);
            return ((MethodInfo)_foundMembers[0]).MakeGenericMethod(_methodArguments);
        }

        static public void MethodInstantiated(Type intermediateType)
        {
            Instantiated(intermediateType);
        }

        static public void TypeInstantiating(Type type)
        {
            Instantiated(type);
        }

        static private void Instantiated(Type type)
        {
            if (!m_Instantiated)
            {
                lock (m_AddLock)
                {
                    if (!m_Instantiating)
                    {
                        m_Types.Enqueue(type);
                        return;
                    }
                }
            }
            Listener.Invoke(type);
        }
    }
}
