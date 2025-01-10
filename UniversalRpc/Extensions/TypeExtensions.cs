using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniversalRpc.Attibutes;

namespace UniversalRPC.Extensions
{
    public static class TypeExtensions
    {
        public static string GetServiceName(this Type type)
        {
            var serviceNameAttribute = type.GetCustomAttribute<ServiceNameAttribute>();
            string serviceName;
            if (serviceNameAttribute != null)
            {
                serviceName = serviceNameAttribute.Name;
            }
            else
            {
                serviceName = type.FullName.Split('.')[0];
            }
            return serviceName;
        }
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
