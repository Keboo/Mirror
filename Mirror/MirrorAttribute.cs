using System;
using System.Linq;
using System.Reflection;

namespace Mirror
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MirrorAttribute : Attribute
    {
        public string TargetType { get; }

        public MirrorAttribute(string targetType)
        {
            TargetType = targetType;
        }
    }

    public static class MirrorExtensions
    {
        public static string GetMirrorClass(this Type mirrorType)
        {
            return mirrorType.GetCustomAttributes<MirrorAttribute>().SingleOrDefault()?.TargetType;
        }
    }
}
