using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Channel_Native.Enums;
using Newtonsoft.Json.Linq;
using PInvoke;
using static PInvoke.Kernel32;

namespace Channel_Native
{
    public class DLL_PInvoke
    {
        #region ----dll函数 委托原型声明部分----
        private SafeLibraryHandle hLib;

        private delegate IntPtr Type_AppInfo();
        private delegate int Type_Initialize(int authcode);
        private delegate int Type_PrivateMsg(int subType, int msgId, long fromQQ, IntPtr msg, int font);
        private delegate int Type_GroupMsg(int subType, int msgId, long fromGroup, long fromQQ, string fromAnonymous, IntPtr msg, int font);
        private delegate int Type_Startup();
        private delegate int Type_Exit();
        private delegate int Type_Enable();
        private delegate int Type_Disable();

        private Type_AppInfo AppInfo;
        private Type_Initialize Initialize;
        private Type_PrivateMsg PrivateMsg;
        private Type_GroupMsg GroupMsg;
        private Type_Startup Startup;
        private Type_Exit Exit;
        private Type_Enable Enable;
        private Type_Disable Disable;
        #endregion

        public SafeLibraryHandle Load(string filepath, JObject json)
        {
            SafeLibraryHandle r = LoadLibrary(filepath);
            if (r.IsInvalid)
            {
                Helper.OutError($"插件 {filepath} 载入失败, GetLastError={GetLastError()}");
            }
            return r;
        }
        /// <summary>
        /// 将要执行的函数转换为委托
        /// </summary>
        /// <param name="APIName">函数名称</param>
        /// <param name="t">需要转换成委托的类型</param>
        public Delegate Invoke(string APIName, Type t)
        {
            IntPtr api = GetProcAddress(hLib, APIName);
            if (api == (IntPtr)0)
                return null;
            return Marshal.GetDelegateForFunctionPointer(api, t);
        }
        public int DoInitialize(int authcode)
        {
            CallInitialize(authcode);
            return 0;
        }
        [HandleProcessCorruptedStateExceptions]
        public KeyValuePair<int, string> GetAppInfo()
        {
            string appinfo = Marshal.PtrToStringAnsi(CallAppinfo());
            string[] pair = appinfo.Split(',');
            if (pair.Length != 2)
                throw new Exception("获取AppInfo信息失败");
            KeyValuePair<int, string> valuePair = new(Convert.ToInt32(pair[0]), pair[1]);
            return valuePair;
        }
        public void Init(SafeLibraryHandle hLib,  string jsonStr)
        {
            Init(hLib, JObject.Parse(jsonStr));
        }
        public void Init(SafeLibraryHandle hLib, JObject json)
        {
            Initialize = (Type_Initialize)Invoke(hLib, "Initialize", typeof(Type_Initialize));
            AppInfo = (Type_AppInfo)Invoke(hLib, "AppInfo", typeof(Type_AppInfo));
            foreach (var item in JArray.Parse(json["event"].ToString()))
            {
                switch (item["id"].ToString())
                {
                    case "1":
                        PrivateMsg = (Type_PrivateMsg)Invoke(hLib, item["function"].ToString(), typeof(Type_PrivateMsg));
                        break;
                    case "2":
                        GroupMsg = (Type_GroupMsg)Invoke(hLib, item["function"].ToString(), typeof(Type_GroupMsg));
                        break;
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "10":
                    case "11":
                    case "12":
                        Helper.OutError("委托实例化失败，未定义事件");
                        break;
                    case "1001":
                        Startup = (Type_Startup)Invoke(hLib, item["function"].ToString(), typeof(Type_Startup));
                        break;
                    case "1002":
                        Exit = (Type_Exit)Invoke(hLib, item["function"].ToString(), typeof(Type_Exit));
                        break;
                    case "1003":
                        Enable = (Type_Enable)Invoke(hLib, item["function"].ToString(), typeof(Type_Enable));
                        break;
                    case "1004":
                        Disable = (Type_Disable)Invoke(hLib, item["function"].ToString(), typeof(Type_Disable));
                        break;
                }
            }
        }
        /// <summary>
        /// 将要执行的函数转换为委托
        /// </summary>
        /// <param name="APIName">函数名称</param>
        /// <param name="t">需要转换成委托的类型</param>
        public Delegate Invoke(SafeLibraryHandle hLib, string APIName, Type t)
        {
            Delegate returnValue;
            IntPtr api = GetProcAddress(hLib, APIName);
            if (api == (IntPtr)0)
            {
                return null;
            }
            returnValue = Marshal.GetDelegateForFunctionPointer(api, t);
            return returnValue;
        }
        public int CallFunction(FunctionName ApiName, params object[] args)
        {
            int returnValue = 0;
            switch (ApiName)
            {
                case FunctionName.PrivateMsg:
                    if (PrivateMsg == null)
                    { returnValue = -1; break; }
                    returnValue = PrivateMsg(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), Convert.ToInt64(args[2]), (IntPtr)args[3], 1);
                    break;
                case FunctionName.GroupMsg:
                    if (GroupMsg == null)
                    { returnValue = -1; break; }
                    returnValue = GroupMsg(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), Convert.ToInt64(args[2]), Convert.ToInt64(args[3])
                        , args[4].ToString(), (IntPtr)args[5], 1);
                    break;
                case FunctionName.Upload:
                case FunctionName.AdminChange:
                case FunctionName.GroupMemberDecrease:
                case FunctionName.GroupMemberIncrease:
                case FunctionName.GroupBan:
                case FunctionName.FriendAdded:
                case FunctionName.FriendRequest:
                case FunctionName.GroupAddRequest:
                    returnValue = -1;
                    Helper.OutError("未实现事件");
                    break;
                case FunctionName.StartUp:
                    if (Startup == null)
                    { returnValue = -1; break; }
                    returnValue = Startup();
                    break;
                case FunctionName.Exit:
                    if (Exit == null)
                    { returnValue = -1; break; }
                    returnValue = Exit();
                    break;
                case FunctionName.Enable:
                    if (Enable == null)
                    { returnValue = -1; break; }
                    returnValue = Enable();
                    break;
                case FunctionName.Disable:
                    if (Disable == null)
                    { returnValue = -1; break; }
                    returnValue = Disable();
                    break;
                default:
                    Helper.OutError("未找到或者未实现的事件");
                    returnValue = -1;
                    break;
            }
            return returnValue;
        }
        public IntPtr CallAppinfo()
        {
            IntPtr returnvalue = (IntPtr)0;
            returnvalue = AppInfo();
            return returnvalue;
        }
        public int CallInitialize(int authCode)
        {
            int returnvalue = 0;
            returnvalue = Initialize(authCode);
            return returnvalue;
        }
    }
}
