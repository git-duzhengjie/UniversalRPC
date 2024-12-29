using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalRPC.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsTask(this Type type,out Type ret)
        {
            if (type == typeof(Task))
            {
                ret = null;
                return true;
            }
            if(type.BaseType!=null&&type.BaseType.Name.StartsWith("Task")){
                ret = type.GetGenericArguments().FirstOrDefault();
                return true;
            };
            ret = type;
            return false;
        }
    }
}
