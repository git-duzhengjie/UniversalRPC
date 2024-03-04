using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniversalRPC.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsTask(this Type type,out Type ret)
        {
            if( type.BaseType.Name.StartsWith("Task")){
                ret = type.GetGenericArguments().First();
                return true;
            };
            ret = type;
            return false;
        }
    }
}
