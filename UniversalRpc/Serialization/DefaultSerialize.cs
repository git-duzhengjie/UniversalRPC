namespace UniversalRPC.Serialization
{
    public class DefaultSerialize : ISerialize
    {
        public T Deserialize<T>(string str)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(str);
        }

        public string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}
