using System;
using static PInvoke.Kernel32;

namespace Channel_Native.Model
{
    public class Plugin
    {
        /// <summary>
        /// 内存句柄
        /// </summary>
        public SafeLibraryHandle iLib;
        /// <summary>
        /// 插件的AppInfo
        /// </summary>
        public AppInfo appinfo;
        /// <summary>
        /// 插件的json部分,包含名称、描述、函数入口以及窗口名称部分
        /// </summary>
        public string json;
        public DLL_PInvoke dll;
        /// <summary>
        /// 标记插件是否启用
        /// </summary>
        public bool Enable;
        public Plugin(SafeLibraryHandle iLib, AppInfo appinfo, string json, DLL_PInvoke dll, bool enable)
        {
            this.iLib = iLib;
            this.appinfo = appinfo;
            this.json = json;
            this.dll = dll;
            this.Enable = enable;
        }
    }
}
