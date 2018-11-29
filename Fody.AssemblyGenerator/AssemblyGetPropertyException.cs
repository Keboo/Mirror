using System;

namespace Fody.AssemblyGenerator
{
    public class AssemblyGetPropertyException : Exception
    {
        public AssemblyGetPropertyException(string message) : base(message)
        { }
    }
}