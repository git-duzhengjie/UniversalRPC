using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UniversalRPC.RPC.Services
{
    /// <summary>
    /// RPCMethod 的摘要说明
    /// </summary>
    public class RPCMethod
    {
        public static Dictionary<string, Dictionary<string, Type>> ReturnTypeMap = new Dictionary<string, Dictionary<string, Type>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static object SendMessageViaHttp(object[] objects, string typeName, string methodName, string url)
        {
            HttpClient httpClient = new HttpClient();
            int version = 0;
#if NET6_0_OR_GREATER
version=2;
#else
            version = 1;
#endif
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
            };
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Version = new Version(version, 0),
                Content = new StringContent(JsonConvert.SerializeObject(request, RPC.JsonSerializerSettings), Encoding.UTF8, "application/json")
            };
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.ToString());
            }
            Type returnType = ReturnTypeMap[typeName][methodName];
            var result = response.Content.ReadAsStringAsync().Result;
            return DeserializeObject(result, returnType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SendMessageViaHttpVoid(object[] objects, string typeName, string methodName, string url)
        {
            HttpClient httpClient = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Version = new Version(2, 0),
                Content = new StringContent(JsonConvert.SerializeObject(new Model.Request
                {
                    ServiceName = typeName,
                    MethodName = methodName,
                    Parameters = objects,
                }, RPC.JsonSerializerSettings), Encoding.UTF8, "application/json")
            };
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.ToString());
            }
            return;
        }

        private static object DeserializeObject(string str, Type returnType)
        {
            var retType = returnType;
            bool isTask = false;
            if (returnType.BaseType?.Name == "Task")
            {
                retType = returnType.GetGenericArguments().First();
                isTask = true;
            }
            var type = typeof(JsonDeserializeObject<>).MakeGenericType(retType);
            var method = type.GetMethod("DeserializeObject");
            if (method == null)
            {
                return null;
            }
            var result = method.Invoke(null, new object[] { str });
            if (isTask)
            {
                return Task.FromResult(result);
            }
            else
            {
                return result;
            }
        }
    }
    class JsonDeserializeObject<T>
    {
        public static T DeserializeObject(string str)
        {
            return JsonConvert.DeserializeObject<T>(str, RPC.JsonSerializerSettings);
        }
    }
}
