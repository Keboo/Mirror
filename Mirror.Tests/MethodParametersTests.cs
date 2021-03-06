﻿using System.Linq;
using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mirror.Tests
{
    [TestClass]
    public class MethodParametersTests
    {

        [Mirror("AssemblyToTest.MethodParameters")]
        private class MethodParametersMirror
        {
#pragma warning disable CS0824 // Constructor is marked external
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            public extern MethodParametersMirror();
            public extern MethodParametersMirror(int intValue);
            public extern MethodParametersMirror(string stringValue);
            public extern void DoSomething(uint uintValue);
            public extern void DoSomething(string optionalString);
            public extern ReturnValueMirror DoSomething(ParameterMirror parameter);
#pragma warning restore CS0824 // Constructor is marked external
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        }

        [Mirror("AssemblyToTest.Parameter")]
        private class ParameterMirror
        {
#pragma warning disable CS0824 // Constructor is marked external
            public extern ParameterMirror();
#pragma warning restore CS0824 // Constructor is marked external
        }
        
        [Mirror("AssemblyToTest.ReturnValue")]
        private class ReturnValueMirror { }

        [TestInitialize]
        public void TestInitialize()
        {
            MethodInvocation.Invocations.Clear();
        }

        [TestMethod]
        public void CanCreateWithIntParameter()
        {
            var sut = new MethodParametersMirror(42);

            Assert.IsInstanceOfType(sut, typeof(MethodParametersMirror));
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(".ctor", invocation.MemberName);
            Assert.AreEqual(42, invocation.Parameters.Single());
            Assert.AreEqual(typeof(MethodParametersMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanCreateWithStringParameter()
        {
            var sut = new MethodParametersMirror("42");

            Assert.IsInstanceOfType(sut, typeof(MethodParametersMirror));
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(".ctor", invocation.MemberName);
            Assert.AreEqual("42", invocation.Parameters.Single());
            Assert.AreEqual(typeof(MethodParametersMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeMethodWithStringParameter()
        {
            var sut = new MethodParametersMirror();

            sut.DoSomething("Some string");
            
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(MethodParametersMirror.DoSomething), invocation.MemberName);
            Assert.AreEqual("Some string", invocation.Parameters.Single());
            Assert.AreEqual(typeof(MethodParametersMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeMethodWithUInt32Parameter()
        {
            var sut = new MethodParametersMirror();

            sut.DoSomething(32);

            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(MethodParametersMirror.DoSomething), invocation.MemberName);
            Assert.AreEqual(32U, invocation.Parameters.Single());
            Assert.AreEqual(typeof(MethodParametersMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanInvokeMethodWithInternalParameterTypes()
        {
            var sut = new MethodParametersMirror();

            ReturnValueMirror returnValue = sut.DoSomething(new ParameterMirror());
            
            Assert.IsInstanceOfType(returnValue, typeof(ReturnValueMirror));
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(MethodParametersMirror.DoSomething), invocation.MemberName);
            Assert.AreEqual(typeof(ParameterMirror).GetMirrorClass(), invocation.Parameters.Single().GetType().FullName);
            Assert.AreEqual(typeof(MethodParametersMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }
    }
}