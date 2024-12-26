using System;

namespace UniversalRpc.Attibutes
{
    public class ServiceNameAttribute:Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }
        public ServiceNameAttribute(string name) { 
            Name = name;
        }
    }
}
