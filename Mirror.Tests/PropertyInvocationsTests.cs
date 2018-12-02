using AssemblyToTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Mirror.Tests
{
    [TestClass]
    public class PropertyInvocationsTests
    {
        [Mirror("AssemblyToTest.PropertyInvocations")]
        private class PropertyInvocationsMirror
        {
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            [Mirror("_PublicProperty")]
            public extern int PublicPropertyField { get; set; }
            public extern int PublicProperty { get; set; }
            [Mirror("_InternalProperty")]
            public extern int InternalPropertyField { get; set; }
            public extern int InternalProperty { get; set; }
            [Mirror("_PrivateProperty")]
            public extern PropertyValueMirror PrivatePropertyField { get; set; }
            public extern PropertyValueMirror PrivateProperty { get; set; }
            public extern PropertyValueMirror PrivateReadonlyProperty { get; }
            [Mirror("_PublicStaticProperty")]
            public static extern int PublicStaticPropertyField { get; set; }
            public static extern int PublicStaticProperty { get; set; }
            [Mirror("_InternalStaticProperty")]
            public static extern int InternalStaticPropertyField { get; set; }
            public static extern int InternalStaticProperty { get; set; }
            [Mirror("_PrivateStaticProperty")]
            public static extern PropertyValueMirror PrivateStaticPropertyField { get; set; }
            public static extern PropertyValueMirror PrivateStaticProperty { get; set; }
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        }

        [Mirror("AssemblyToTest.PropertyValue")]
        private class PropertyValueMirror
        {
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
            public extern int Value { get; set; }
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        }
        
        //[ClassInitialize]
        //public static void Initialize(TestContext context)
        //{
        //    var weaver = Weaver.FindWeaver("Mirror");
        //    using (var ms = new MemoryStream(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location)))
        //    {
        //        ms.Position = 0;
        //        weaver.ApplyToAssembly(ms);
        //    }
        //}

        [TestInitialize]
        public void TestInitialize()
        {
            MethodInvocation.Invocations.Clear();
        }

        [TestMethod]
        public void CanSetPublicProperty()
        {
            var sut = new PropertyInvocationsMirror();

            sut.PublicProperty = 42;
            int fieldValue = sut.PublicPropertyField;

            Assert.AreEqual(42, fieldValue);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PublicProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetPublicProperty()
        {
            var sut = new PropertyInvocationsMirror();

            sut.PublicPropertyField = 42;
            int rv = sut.PublicProperty;

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PublicProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanSetInternalProperty()
        {
            var sut = new PropertyInvocationsMirror();

            sut.InternalProperty = 42;
            int fieldValue = sut.InternalPropertyField;

            Assert.AreEqual(42, fieldValue);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.InternalProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetInternalProperty()
        {
            var sut = new PropertyInvocationsMirror();

            sut.PublicPropertyField = 42;
            int rv = sut.PublicProperty;

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PublicProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanSetPrivateProperty()
        {
            var sut = new PropertyInvocationsMirror();
            var value = new PropertyValueMirror { Value = 42 };

            sut.PrivateProperty = value;
            PropertyValueMirror fieldValue = sut.PrivatePropertyField;

            Assert.AreEqual(42, fieldValue.Value);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PrivateProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetPrivateProperty()
        {
            var sut = new PropertyInvocationsMirror();
            var value = new PropertyValueMirror { Value = 42 };

            sut.PrivatePropertyField = value;
            PropertyValueMirror rv = sut.PrivateProperty;

            Assert.AreEqual(42, rv.Value);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PrivateProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetReadonlyProperty()
        {
            var sut = new PropertyInvocationsMirror();

            PropertyValueMirror rv = sut.PrivateReadonlyProperty;

            Assert.IsNotNull(rv);
            Assert.AreEqual(24, rv.Value);
        }

        [TestMethod]
        public void CanSetPublicStaticProperty()
        {
            PropertyInvocationsMirror.PublicStaticProperty = 42;
            int fieldValue = PropertyInvocationsMirror.PublicStaticPropertyField;

            Assert.AreEqual(42, fieldValue);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PublicStaticProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetPublicStaticProperty()
        {
            PropertyInvocationsMirror.PublicStaticPropertyField = 42;
            int rv = PropertyInvocationsMirror.PublicStaticProperty;

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PublicStaticProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanSetInternalStaticProperty()
        {
            PropertyInvocationsMirror.InternalStaticProperty = 42;
            int fieldValue = PropertyInvocationsMirror.InternalStaticPropertyField;

            Assert.AreEqual(42, fieldValue);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.InternalStaticProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetInternalStaticProperty()
        {
            PropertyInvocationsMirror.InternalStaticPropertyField = 42;
            int rv = PropertyInvocationsMirror.InternalStaticProperty;

            Assert.AreEqual(42, rv);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.InternalStaticProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanSetPrivateStaticProperty()
        {
            var value = new PropertyValueMirror { Value = 42 };

            PropertyInvocationsMirror.PrivateStaticProperty = value;
            PropertyValueMirror fieldValue = PropertyInvocationsMirror.PrivateStaticPropertyField;

            Assert.AreEqual(42, fieldValue.Value);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PrivateStaticProperty) + "_set", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }

        [TestMethod]
        public void CanGetPrivateStaticProperty()
        {
            var value = new PropertyValueMirror { Value = 42 };

            PropertyInvocationsMirror.PrivateStaticPropertyField = value;
            PropertyValueMirror rv = PropertyInvocationsMirror.PrivateStaticProperty;

            Assert.AreEqual(42, rv.Value);
            var invocation = MethodInvocation.Invocations.Single();
            Assert.AreEqual(nameof(PropertyInvocationsMirror.PrivateStaticProperty) + "_get", invocation.MemberName);
            Assert.AreEqual(typeof(PropertyInvocationsMirror).GetMirrorClass(), invocation.ContainingType.FullName);
        }
    }
}