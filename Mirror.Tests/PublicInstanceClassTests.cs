using System.Linq;
using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mirror.Tests
{
    [TestClass]
    public class PublicInstanceClassTests
    {
        [Mirror("AssemblyToTest.PublicInstanceClass")]
        private class PublicInstanceClassMirror
        {
            [Mirror]
            public extern void PrivateMethod();

#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            public extern void PrivateProtectedMethod();
            public extern void ProtectedMethod();
            public extern void InternalMethod();
            public extern void ProtectedInternalMethod();
            public extern void PublicMethod();
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
            var sut = new PublicInstanceClassMirror();

            sut.PrivateMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.PrivateMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokePrivateProtectedMethod()
        {
            var sut = new PublicInstanceClassMirror();

            sut.PrivateProtectedMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.PrivateProtectedMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeProtectedMethod()
        {
            var sut = new PublicInstanceClassMirror();

            sut.ProtectedMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.ProtectedMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeInternalMethod()
        {
            var sut = new PublicInstanceClassMirror();

            sut.InternalMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.InternalMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeProtectedInternalMethod()
        {
            var sut = new PublicInstanceClassMirror();

            sut.ProtectedInternalMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.ProtectedInternalMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokePublicMethod()
        {
            var sut = new PublicInstanceClassMirror();

            sut.PublicMethod();

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PublicInstanceClassMirror.PublicMethod), invocation.MemberName);
            Assert.AreEqual(typeof(PublicInstanceClassMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }
    }
}