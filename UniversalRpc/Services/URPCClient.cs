
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace UniversalRPC.Services
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class URPCClient<T> where T : class
    {
        /// <summary>
        /// 注入的URPC对象
        /// </summary>
        public T Value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public URPCClient(string url)
        {
            Value=new URPCClients(url).GetOrCreate().OfType<T>().FirstOrDefault();
        }
        
    }



}
