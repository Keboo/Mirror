using System.IO;
using System.Reflection;
using Fody.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mirror.Tests
{
    [TestClass]
    public class GenericMethodsTests
    {
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
        [Mirror("AssemblyToTest.GenericMethods")]
        public class GenericMethodsMirror
        {
#pragma warning disable CS0824 // Constructor is marked external
            public extern GenericMethodsMirror();
#pragma warning restore CS0824 // Constructor is marked external

            public extern GenericClassMirror<int> GenericReturnValue();
            public extern GenericStructMirror<int> GenericStructReturnValue();
            public extern GenericClassMirror<GenericStructMirror<int>> NestedGenericReturnValue();
        }

        [Mirror("AssemblyToTest.GenericClass")]
        public class GenericClassMirror<T>
        {
            public extern T Value { get; }
        }

        [Mirror("AssemblyToTest.GenericStruct")]
        public struct GenericStructMirror<T>
        {
            public extern T Value { get; }
        }
#pragma warning restore CS0626 // Method, operator, or accessor is marked external and has no attributes on it

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var weaver = Weaver.FindWeaver("Mirror");
            string current = Assembly.GetExecutingAssembly().Location;
            string newPath = Path.Combine(Path.GetDirectoryName(current),
                $"{Path.GetFileNameWithoutExtension(current)}_modified{Path.GetExtension(current)}");
            File.Copy(current, newPath, true);
            weaver.ApplyToAssembly(newPath);
        }

        [TestMethod]
        public void CanGetGenericReturnValue()
        {
            var sut = new GenericMethodsMirror();

            GenericClassMirror<int> rv = sut.GenericReturnValue();

            Assert.IsInstanceOfType(rv, typeof(GenericClassMirror<int>));
            Assert.AreEqual(42, rv.Value);
        }

        [TestMethod]
        public void CanGetGenericStructReturnValue()
        {
            var sut = new GenericMethodsMirror();

            GenericStructMirror<int> rv = sut.GenericStructReturnValue();

            Assert.IsInstanceOfType(rv, typeof(GenericStructMirror<int>));
            Assert.AreEqual(24, rv.Value);
        }

        [TestMethod]
        public void CanGetNestedGenericReturnValue()
        {
            var sut = new GenericMethodsMirror();

            GenericClassMirror<GenericStructMirror<int>> rv = sut.NestedGenericReturnValue();

            Assert.IsInstanceOfType(rv, typeof(GenericClassMirror<GenericStructMirror<int>>));
            var @struct = rv.Value;
            Assert.IsInstanceOfType(@struct, typeof(GenericStructMirror<int>));
            Assert.AreEqual(72, @struct.Value);
        }
    }
}