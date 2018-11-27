using System.Linq;
using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mirror.Tests
{
    [TestClass]
    public class MethodReturnValuesTests
    {
        [Mirror("AssemblyToTest.MethodReturnValues")]
        private class MethodReturnValuesMirror
        {
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            public extern string Get42();
            public static extern int Get43();
            public static extern MethodReturnValuesMirror CreateInstance();
            [Mirror("GetHashCode")]
            public extern int RealGetHashCode();
            public override extern int GetHashCode();
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        }

        [TestInitialize]
        public void TestInitialize()
        {
            MethodInvocation.Invocations.Clear();
        }

        [TestMethod]
        public void CanGetStringReturnValue()
        {
            var sut = new MethodReturnValuesMirror();

            string rv = sut.Get42();

            Assert.AreEqual("42", rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(MethodReturnValuesMirror.Get42), invocation.MemberName);
            Assert.AreEqual(typeof(MethodReturnValuesMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetIntReturnValue()
        {
            int rv = MethodReturnValuesMirror.Get43();

            Assert.AreEqual(43, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(MethodReturnValuesMirror.Get43), invocation.MemberName);
            Assert.AreEqual(typeof(MethodReturnValuesMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanOverrideGetHashCode()
        {
            var sut = new MethodReturnValuesMirror();

            int rv = sut.RealGetHashCode();

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(GetHashCode), invocation.MemberName);
            Assert.AreEqual(typeof(MethodReturnValuesMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeOverriddenGetHashCode()
        {
            var sut = new MethodReturnValuesMirror();

            int rv = sut.GetHashCode();

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(GetHashCode), invocation.MemberName);
            Assert.AreEqual(typeof(MethodReturnValuesMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetInstanceOfInternalClass()
        {
            MethodReturnValuesMirror instance = MethodReturnValuesMirror.CreateInstance();

            Assert.IsNotNull(instance);
            Assert.AreEqual(42, instance.RealGetHashCode());
            var invocation = MethodInvocation.Invocations.First();
            Assert.AreEqual(nameof(MethodReturnValuesMirror.CreateInstance), invocation.MemberName);
            Assert.AreEqual(typeof(MethodReturnValuesMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }
    }
}