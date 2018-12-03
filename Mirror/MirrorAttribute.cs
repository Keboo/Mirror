using System;

namespace Mirror
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    public class MirrorAttribute : Attribute
    {
        public string TargetName { get; }

        public MirrorAttribute(string targetName)
        {
            TargetName = targetName;
        }

        public MirrorAttribute()
        { }
    }
}
