
using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute2 : Attribute
    {
        public IgnoresAccessChecksToAttribute2(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}

namespace Mirror.Tests
{
    [TestClass]
    public class StaticMethodTests
    {
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        [Mirror("AssemblyToTest.PublicStaticClass")]
        private class PublicStaticClassMirror
        {
            public static extern void PrivateMethod();
        }
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        
        [TestMethod]
        public void CanInvokePrivateMethod()
        {
            //Do stuff
            PublicStaticClassMirror.PrivateMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicStaticClassMirror.PrivateMethod), invocation.Method.Name);
            Assert.AreEqual(typeof(PublicStaticClassMirror).GetMirrorClass(), invocation.Method.DeclaringType.FullName);
        }
    }
}
