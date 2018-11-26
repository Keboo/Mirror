
using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Mirror.Tests
{
    [TestClass]
    public class PublicStaticClassTests
    {
        [Mirror("AssemblyToTest.PublicStaticClass")]
        private static class PublicStaticClassMirror
        {
            [Mirror]
            public static extern void PrivateMethod();

#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            public static extern void InternalMethod();

            public static extern void PublicMethod();
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        }

        [TestInitialize]
        public void TestInitialize()
        {
            MethodInvocation.Invocations.Clear();
        }

        [TestMethod]
        public void CanInvokePrivateMethod()
        {
            PublicStaticClassMirror.PrivateMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicStaticClassMirror.PrivateMethod), invocation.Method.Name);
            Assert.AreEqual(typeof(PublicStaticClassMirror).GetMirrorClass(), invocation.Method.DeclaringType.FullName);
        }

        [TestMethod]
        public void CanInvokeInternalMethod()
        {
            PublicStaticClassMirror.InternalMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicStaticClassMirror.InternalMethod), invocation.Method.Name);
            Assert.AreEqual(typeof(PublicStaticClassMirror).GetMirrorClass(), invocation.Method.DeclaringType.FullName);
        }

        [TestMethod]
        public void CanInvokePublicMethod()
        {
            PublicStaticClassMirror.PublicMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicStaticClassMirror.PublicMethod), invocation.Method.Name);
            Assert.AreEqual(typeof(PublicStaticClassMirror).GetMirrorClass(), invocation.Method.DeclaringType.FullName);
        }
    }
}
