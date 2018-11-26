using System;
using System.Linq;
using System.Reflection;

namespace Mirror
{
    public static class MirrorExtensions
    {
        public static string GetMirrorClass(this Type mirrorType)
        {
            return mirrorType.GetCustomAttributes<MirrorAttribute>().SingleOrDefault()?.TargetName;
        }
    }
}