﻿using System;

namespace Fody. AssemblyGenerator
{
    public class AssemblyInvocationExcetion : Exception
    {
        public AssemblyInvocationExcetion(string message) : base(message)
        { }
    }
}