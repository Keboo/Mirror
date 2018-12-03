// ReSharper disable All

namespace AssemblyToTest
{
    internal class GenericMethods
    {
        public GenericClass<int> GenericReturnValue()
        {
            return new GenericClass<int>(42);
        }

        public GenericStruct<int> GenericStructReturnValue()
        {
            return new GenericStruct<int>(24);
        }

        public GenericClass<GenericStruct<int>> NestedGenericReturnValue()
        {
            return new GenericClass<GenericStruct<int>>(new GenericStruct<int>(72));
        }
    }

    internal sealed class GenericClass<T>
    {
        private T Value { get; set; }

        public GenericClass()
        {
            
        }

        public GenericClass(T value)
        {
            Value = value;
        }
    }

    internal struct GenericStruct<T>
    {
        private T Value { get; set; }

        public GenericStruct(T value)
        {
            Value = value;
        }
    }

    internal struct FortyTwo
    {
        private int Value => 42;
    }
}