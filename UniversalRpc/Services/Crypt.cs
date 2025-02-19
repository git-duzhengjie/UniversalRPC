using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UniversalRPC.Services
{
    public static class Crypt
    {

        public static string Encrypt(this string str, string key64 = null, string iv64 = null)
        {
            try
            {
                byte[] byteKey = //将密钥字符串转换为字节序列
                    Convert.FromBase64String(key64??URPC.Key);
                byte[] iv = Convert.FromBase64String(iv64??URPC.IV);
                byte[] data = //将字符串转换为字节序列
                    Encoding.Unicode.GetBytes(str);
                //创建内存流对象
                MemoryStream stream = new MemoryStream();
                using (CryptoStream cryptStream = new CryptoStream(stream, Aes.Create().CreateEncryptor(byteKey, iv), CryptoStreamMode.Write))
                {
                    cryptStream.Write(data, 0, data.Length);//向加密流中写入字节序列
                    cryptStream.FlushFinalBlock();//将数据压入基础流
                    byte[] tmp = stream.ToArray();//从内存流中获取字节序列
                    return Convert.ToBase64String(tmp);
                }
            }
            catch (CryptographicException ce)
            {
                throw new Exception(ce.Message);
            }
        }
        public static string Decrypt(this string str,string key64=null,string iv64=null)
        {
            try
            {
                byte[] byteKey = //将密钥字符串转换为字节序列
                    Convert.FromBase64String(key64??URPC.Key);
                byte[] iv = Convert.FromBase64String(iv64??URPC.IV);
                byte[] data = //将加密后的字符串转换为字节序列
                    Convert.FromBase64String(str);
                MemoryStream stream =//创建内存流对象并写入数据
                    new MemoryStream(data);
                CryptoStream cryptStream = //创建加密流对象
                    new CryptoStream(stream, Aes.Create().
                    CreateDecryptor(byteKey, iv), CryptoStreamMode.Read);
                byte[] temp = new byte[200];//创建字节序列对象
                MemoryStream tempStream = new MemoryStream();//创建内存流对象
                int i = 0;//创建记数器
                while ((i = cryptStream.Read(//使用while循环得到解密数据
                    temp, 0, temp.Length)) > 0)
                {
                    tempStream.Write(//将解密后的数据放入内存流
                        temp, 0, i);
                }
                return //方法返回解密后的字符串
                    Encoding.Unicode.GetString(tempStream.ToArray());
            }
            catch (CryptographicException ce)
            {
                throw new Exception(ce.Message);
            }
        }
    }
}