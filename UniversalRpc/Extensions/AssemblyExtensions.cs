using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UniversalRpc.Extensions
{
    public static class AssemblyExtensions
    {
        public static bool IsNotOut(this Assembly assembly)
        {
            return !assembly.IsSystem();
        }

        public static bool IsSystem(this Assembly assembly)
        {
            return assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Microsoft");
        }
    }
}
