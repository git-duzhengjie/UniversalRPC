using System;
using System.Collections.Generic;
using System.Text.Json;
using UniversalRPC.Serialization;
using UniversalRPC.Services;

namespace UniversalRPC
{
    public class URPC
    {
        public static string Key = "dfsuioer123120sdfs_@$%";
        public static ISerialize Serialize;

        public static Dictionary<string,bool> HubMap=new Dictionary<string, bool>();

        private static readonly Dictionary<(Type,string),object> _URPCClientService=new Dictionary<(Type,string),object>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        public static T GetUURPC<T>(string url,bool isHub=false) where T : class
        {
            HubMap[url] = isHub;
            if(_URPCClientService.TryGetValue((typeof(T),url),out var obj))
            {
                return (T)obj;
            }
            var client = new URPCClient<T>(url);
            _URPCClientService.Add((typeof(T), url), client.Value);
            return client.Value;
        }

        /// <summary>
        /// 获取序列化器
        /// </summary>
        /// <returns></returns>
        public static ISerialize GetSerialize()
        {
            return Serialize??new DefaultSerialize();
        }
    }
}
