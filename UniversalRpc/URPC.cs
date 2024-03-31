using System;
using System.Collections.Generic;
using System.Text.Json;
using UniversalRPC.Services;

namespace UniversalRPC
{
    public class URPC
    {
        public static JsonSerializerOptions JsonSerializerOptions {  get; set; }

        private static readonly Dictionary<(Type,string),object> _URPCClientService=new Dictionary<(Type,string),object>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public static T GetUURPC<T>(string url) where T : class
        {
            if(_URPCClientService.TryGetValue((typeof(T),url),out var obj))
            {
                return (T)obj;
            }
            var client = new URPCClient<T>(url);
            _URPCClientService.Add((typeof(T), url), client.Value);
            return client.Value;
        }
    }
}
