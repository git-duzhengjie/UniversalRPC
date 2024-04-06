namespace UniversalRPC.Serialization
{
    public interface ISerialize
    {
        /// <summary>
        /// 对象序列化为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        string Serialize(object obj);

        /// <summary>
        /// 字符串序列化对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        T Deserialize<T>(string str);
    }
}
