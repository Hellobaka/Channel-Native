using Channel_SDK;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Channel_Native
{
    public static class Helper
    {
        [DllImport("kernel32.dll", EntryPoint = "lstrlenA", CharSet = CharSet.Ansi)]
        private extern static int LstrlenA(IntPtr ptr);

        public static void OutError(string content, string type = "")
        {
            Console.WriteLine($"[-][{DateTime.Now.ToLongTimeString()}] {(string.IsNullOrWhiteSpace(type) ? "" : $"[{type}]")}{content}");
            //Channel.Log
        }
        public static void OutLog(string content, string type = "")
        {
            Console.WriteLine($"[+][{DateTime.Now.ToLongTimeString()}] {(string.IsNullOrWhiteSpace(type) ? "" : $"[{type}]")}{content}");
            //Channel.Log
        }
        public static long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        public static string ToJson(this object json) => JsonConvert.SerializeObject(json, Formatting.None);

        /// <summary>
        /// 读取指针内所有的字节数组并编码为指定字符串
        /// </summary>
        /// <param name="strPtr">字符串的 <see cref="IntPtr"/> 对象</param>
        /// <param name="encoding">目标编码格式</param>
        /// <returns></returns>
        public static string ToString(this IntPtr strPtr, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }

            int len = LstrlenA(strPtr);   //获取指针中数据的长度
            if (len == 0)
            {
                return string.Empty;
            }
            byte[] buffer = new byte[len];
            Marshal.Copy(strPtr, buffer, 0, len);
            return encoding.GetString(buffer);
        }
        /// <summary>
        /// 读取 <see cref="System.Enum"/> 标记 <see cref="System.ComponentModel.DescriptionAttribute"/> 的值
        /// </summary>
        /// <param name="value">原始 <see cref="System.Enum"/> 值</param>
        /// <returns></returns>
        public static string GetDescription(this System.Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>(false);
            return attribute.Description;
        }
        /// <summary>
        /// 获取字符串副本的转义形式
        /// </summary>
        /// <param name="source">欲转义的原始字符串</param>
        /// <param name="enCodeComma">是否转义逗号, 默认 <code>false</code></param>
        /// <exception cref="ArgumentNullException">参数: source 为 null</exception>
        /// <returns>返回转义后的字符串副本</returns>
        public static string CQEnCode(string source, bool enCodeComma)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            StringBuilder builder = new StringBuilder(source);
            builder = builder.Replace("&", "&amp;");
            builder = builder.Replace("[", "&#91;");
            builder = builder.Replace("]", "&#93;");
            if (enCodeComma)
            {
                builder = builder.Replace(",", "&#44;");
            }
            return builder.ToString();
        }
        /// <summary>
        /// 获取字符串副本的非转义形式
        /// </summary>
        /// <param name="source">欲反转义的原始字符串</param>
        /// <exception cref="ArgumentNullException">参数: source 为 null</exception>
        /// <returns>返回反转义的字符串副本</returns>
        public static string CQDeCode(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            StringBuilder builder = new StringBuilder(source);
            builder = builder.Replace("&#91;", "[");
            builder = builder.Replace("&#93;", "]");
            builder = builder.Replace("&#44;", ",");
            builder = builder.Replace("&amp;", "&");
            return builder.ToString();
        }

    }
}
