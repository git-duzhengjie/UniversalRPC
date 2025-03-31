namespace UniversalRpc.Abstracts
{
    public interface IObject
    {
        /// <summary>
        /// 对象名
        /// </summary>
        string ObjectName { get; }

        /// <summary>
        /// 对象类型
        /// </summary>
        int ObjectType { get; }
    }
}
