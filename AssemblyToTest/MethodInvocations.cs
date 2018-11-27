using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AssemblyToTest
{
    public class MethodInvocation
    {
        private static readonly AsyncLocal<IList<MethodInvocation>> _Invocations = new AsyncLocal<IList<MethodInvocation>>();

        public static IList<MethodInvocation> Invocations
        {
            get
            {
                if (_Invocations.Value == null)
                {
                    _Invocations.Value = new List<MethodInvocation>();
                }
                return _Invocations.Value;
            }
        }

        public static void RegisterCall<T>([CallerMemberName] string methodName = null)
        {
            RegisterCall(typeof(T), methodName);
        }

        public static void RegisterCall<T>(object[] parameters, [CallerMemberName] string methodName = null)
        {
            RegisterCall(typeof(T), parameters, methodName);
        }

        public static void RegisterCall(Type containingType, [CallerMemberName] string methodName = null)
        {
            RegisterCall(containingType, null, methodName);
        }

        public static void RegisterCall(Type containingType, object[] parameters, [CallerMemberName] string methodName = null)
        {
            if (containingType == null) throw new ArgumentNullException(nameof(containingType));

            Invocations.Add(new MethodInvocation(methodName, containingType, parameters));
        }

        private MethodInvocation(string memberName, Type containingType, object[] parameters)
        {
            MemberName = memberName;
            ContainingType = containingType;
            Parameters = parameters ?? Array.Empty<object>();
        }

        public object[] Parameters { get; }
        
        public Type ContainingType { get; }

        public string MemberName { get; }
    }
}