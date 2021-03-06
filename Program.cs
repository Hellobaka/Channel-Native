using Channel_Native.Enums;
using Channel_Native.WebSocketCore;
using Channel_SDK;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Channel_Native
{
    internal class Program
    {

        static void Main(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _ = new PluginManagment();

                HandleStartArgs(args);
                if (MainSave.Role == Role.Plugin)
                    PluginInit();
                else
                    ServerInit();
            }
            catch (Exception ex)
            {

            }

            while (true)
            {
                string order = Console.ReadLine();
                switch (order)
                {
                    case "load":
                        LoadPlugin();
                        break;
                    default:
                        break;
                }
            }
        }
        private static void HandleStartArgs(string[] args)
        {
            //启动参数定义：
            ////-role: 启动身份, 默认是控制端(0), 插件标志为(1)
            //-ws: websocket连接地址
            //-pid: 控制端进程PID, 需监视进程是否存在
            //-name: 需要读取插件的文件名称, *.dll
            if (args.Length == 0)
                return;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-role":
                        MainSave.Role = (Role)Convert.ToInt32(args[i + 1]);
                        break;
                    case "-ws":
                        MainSave.ServerURL = args[i + 1];
                        break;
                    case "-pid":
                        MainSave.ServerPID = Convert.ToInt32(args[i + 1]);
                        break;
                    case "-name":
                        MainSave.PluginName = args[i + 1];
                        break;
                    default:
                        break;
                }
            }
        }

        private static void ServerInit()
        {
            string url = "ws://127.0.0.1:6235/main";
            Channel channel = new(url);
            channel.PluginInfo = new()
            {
                Author = "落花茗",
                Version = "1.0.0",
                Name = "Channel-Native",
                Description = "用于兼容酷Q插件与QQ频道的对话事件"
            };

            channel.Connect();
            channel.OnATMessage += Channel_OnATMessage;

            Helper.OutLog($"插件管理端");
            //创建插件管理服务器
            Helper.OutLog($"创建插件WebSocket服务器...");
            MainSave.ServerPort = 6234;
            var server = new Server(MainSave.ServerPort);
            //启动子进程
            LoadPlugin();
            Helper.OutLog("等待插件端连接...");
        }

        private static void LoadPlugin()
        {
            string path = Path.Combine(Helper.BasePath, "data", "plugins");
            Helper.OutLog($"遍历插件目录 {path}...");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            DirectoryInfo directoryInfo = new(path);
            int selfPID = Environment.ProcessId;
            foreach (var item in directoryInfo.GetFiles().Where(x => x.Extension == ".dll"))
            {
                ProcessStartInfo ps = new(Process.GetCurrentProcess().MainModule.FileName)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = $"-role { (int)Role.Plugin } -ws ws://127.0.0.1:{MainSave.ServerPort}/channel-native -pid { selfPID } -name { item.Name }"
                };
                Process.Start(ps);
            }
        }

        private static void PluginInit()
        {
            Helper.OutLog($"插件端 主进程PID: {MainSave.ServerPID} 启动插件: {MainSave.PluginName} ws网址: {MainSave.ServerURL}");
            //连接插件管理端
            Helper.OutLog($"尝试连接插件服务端...");
            Client wsClient = new(MainSave.ServerURL);
            wsClient.Connect();
            while(!Client.Connected)
            {
                Thread.Sleep(100);
            }
            //读取插件信息
            var flag = PluginManagment.Instance.Load(Path.Combine(Helper.BasePath, "data", "plugins", MainSave.PluginName));
            if (flag)
            {
                var appinfo = PluginManagment.Instance.InstancePlugin.appinfo;
                wsClient.Emit(PluginMessageType.PluginInfo, PluginManagment.Instance?.InstancePlugin.appinfo);
                Helper.OutLog($"插件读取成功: {appinfo.Id}[{appinfo.Name}] {appinfo.Version} - {appinfo.Author}", "插件读取", true);
            }
            else
            {
                Helper.OutError("插件读取失败, 子程序已退出...", "插件读取", true);
                Environment.Exit(1);
            }

            //启动主进程监测线程
            Helper.OutLog($"启动服务端存活监测线程...");
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    if (!Process.GetProcesses().Any(x => x.Id == MainSave.ServerPID))
                    {
                        Helper.OutLog("主进程已退出，子进程退出");
                        Environment.Exit(0);
                    }
                }
            }).Start();
        }

        private static void Channel_OnATMessage(Channel_SDK.Model.Message msg)
        {
            Helper.OutLog("收到AT消息，开始分发");
            Server.Instance.Broadcast(PluginMessageType.ReceiveMessage, msg);
        }
    }
}
