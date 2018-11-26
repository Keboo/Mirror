using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static void RegisterCall(Type containingType, [CallerMemberName] string methodName = null)
        {
            if (containingType == null) throw new ArgumentNullException(nameof(containingType));
            MethodInfo method = containingType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Single(x => x.Name == methodName);

            Invocations.Add(new MethodInvocation(method));
        }

        private MethodInvocation(MethodInfo method)
        {
            Method = method;
            Parameters = Array.Empty<object>();
        }

        public object[] Parameters { get; }

        public MethodInfo Method { get; }
    }
}