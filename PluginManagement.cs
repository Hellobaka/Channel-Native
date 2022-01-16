using Channel_Native.Enums;
using Channel_Native.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using static PInvoke.Kernel32;


namespace Channel_Native
{
    public class PluginManagment
    {
        public static PluginManagment Instance { get; private set; }
        public Plugin InstancePlugin { get; set; }
        public bool Loading { get; set; } = false;
        public PluginManagment()
        {
            Instance = this;
        }
        /// <summary>
        /// 以绝对路径路径载入拥有同名json的dll插件
        /// </summary>
        /// <param name="filepath">插件dll的绝对路径</param>
        /// <returns>载入是否成功</returns>
        public bool Load(string filepath)
        {
            FileInfo plugininfo = new (filepath);
            if (!File.Exists(plugininfo.FullName.Replace(".dll", ".json")))
            {
                Helper.OutError($"插件 {plugininfo.Name} 加载失败,原因:缺少json文件", "插件读取", true);
                return false;
            }

            JObject json = JObject.Parse(File.ReadAllText(plugininfo.FullName.Replace(".dll", ".json")));
            int authcode = new Random().Next();
            DLL_PInvoke dll = new();
            if (!Directory.Exists(@"data\tmp"))
                Directory.CreateDirectory(@"data\tmp");
            //复制需要载入的插件至临时文件夹,可直接覆盖原dll便于插件重载
            string destpath = @"data\tmp\" + plugininfo.Name;
            File.Copy(plugininfo.FullName, destpath, true);
            //复制它的json
            File.Copy(plugininfo.FullName.Replace(".dll", ".json"), destpath.Replace(".dll", ".json"), true);

            var iLib = dll.Load(destpath, json);//将dll插件LoadLibrary,并进行函数委托的实例化;

            if (iLib.IsInvalid)
            {
                Helper.OutError($"插件 {plugininfo.Name} 加载失败,返回句柄为空,GetLastError={GetLastError()}", "插件读取", true);
                return false;
            }
            dll.Init(iLib, json);

            //执行插件的init,分配一个authcode
            dll.DoInitialize(authcode);
            //获取插件的appinfo,返回示例 9,me.cqp.luohuaming.Sign,分别为ApiVer以及AppID
            KeyValuePair<int, string> appInfotext = dll.GetAppInfo();
            AppInfo appInfo = new(appInfotext.Value, 0, appInfotext.Key
                , json["name"].ToString(), json["version"].ToString(), Convert.ToInt32(json["version_id"].ToString())
                , json["author"].ToString(), json["description"].ToString(), authcode);
            bool enabled = true;//获取插件启用状态
            //保存至插件列表
            InstancePlugin = new Plugin(iLib, appInfo, json.ToString(), dll, enabled);

            LogHelper.WriteLog(LogLevel.InfoSuccess, "插件载入", $"插件 {appInfo.Name} 加载成功");
            var r = new { appinfo = JsonConvert.SerializeObject(appInfo), wsurl = MainSave.ServerURL }.ToJson();
            cq_start(Marshal.StringToHGlobalAnsi(r), authcode);

            //将它的窗口写入托盘右键菜单
            //TODO: fix windows
            //NotifyIconHelper.LoadMenu(json);
            return true;
        }
        /// <summary>
        /// 卸载插件，执行被卸载事件，从菜单移除此插件的菜单
        /// </summary>
        /// <param name="plugin"></param>
        public void UnLoad()
        {
            try
            {
                Loading = true;
                LogHelper.WriteLog("开始卸载插件...");
                CallFunction(FunctionName.Exit);
                CallFunction(FunctionName.Disable);
                LogHelper.WriteLog(LogLevel.InfoSuccess, "插件卸载", $"插件 {InstancePlugin.appinfo.Name} 卸载成功");
                //TODO: 通知插件已卸载
                GC.Collect();
                Loading = false;
                LogHelper.WriteLog("插件卸载完毕");
            }
            catch (Exception e)
            {
                Helper.OutError("插件卸载" + e.Message + e.StackTrace, "插件卸载", true);
            }
        }
        //写在构造函数是不是还好点?
        /// <summary>
        /// 成员初始化，用于删除上次运行的临时目录、加载插件以及执行启动事件
        /// </summary>
        public void Init()
        {
            if (Directory.Exists(@"data\tmp"))
                Directory.Delete(@"data\tmp", true);
            LoadLibrary("CQP.dll");
            LogHelper.WriteLog("遍历启动事件……");
            CallFunction(FunctionName.StartUp);
            CallFunction(FunctionName.Enable);
            Loading = false;
        }
        [DllImport("CQP.dll", EntryPoint = "cq_start")]
        private static extern bool cq_start(IntPtr path, int authcode);
        [DllImport("CQP.dll", EntryPoint = "CQ_addLog")]
        private static extern int CQ_addLog(int authCode, int priority, IntPtr type, IntPtr msg);
        [DllImport("CQP.dll", EntryPoint = "init")]
        private static extern int init(int a, int b);
        /// <summary>
        /// 核心方法调用，将前端处理的数据传递给插件对应事件处理，尝试捕获非托管插件的异常
        /// </summary>
        /// <param name="ApiName">调用的事件名称，前端统一名称，或许应该写成枚举</param>
        /// <param name="args">参数表</param>
        [HandleProcessCorruptedStateExceptions]
        public int CallFunction(FunctionName ApiName, params object[] args)
        {
            JObject json = new()
            {
                new JProperty("ApiName",JsonConvert.SerializeObject(ApiName)),
                new JProperty("Args",JsonConvert.SerializeObject(args))
            };
            if ((ApiName != FunctionName.Disable && ApiName != FunctionName.Exit &&
                ApiName != FunctionName.Enable && ApiName != FunctionName.StartUp)
                && Loading)
            {
                LogHelper.WriteLog(LogLevel.Warning, "OPQBot框架", "插件逻辑处理", "插件模块处理中...", "x 不处理");
                return -1;
            }
            DLL_PInvoke dll = InstancePlugin.dll;
            //先看此插件是否已禁用
            if (InstancePlugin.Enable is false) return 0;
            try
            {
                return dll.CallFunction(ApiName, args);
            }
            catch (Exception e)
            {
                Helper.OutError($"函数执行异常 插件 {InstancePlugin.appinfo.Name} {ApiName} 函数发生错误，错误信息:{e.Message} {e.StackTrace}", "插件事件", true);
                new Thread(() =>
                {
                    //TODO: 报错通知
                    //报错误但仍继续执行
                }).Start();
            }
            return 0;
        }
        /// <summary>
        /// 重载应用
        /// </summary>
        public void ReLoad()
        {
            UnLoad();
            Environment.Exit(0);
        }
    }
}
