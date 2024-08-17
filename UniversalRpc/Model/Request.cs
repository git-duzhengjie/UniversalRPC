using System;

namespace UniversalRPC.Model
{
    [Serializable]
    public class Request
    {
        /// <summary>
        /// 请求id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 请求的方法名
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 请求的参数
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// 请求的参数类型
        /// </summary>
        public string[] ParameterTypeNames { get; set; }

        /// <summary>
        /// 校验码
        /// </summary>
        public string Code { get; set; }
    }
}
