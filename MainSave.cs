using Channel_Native.Enums;
using System;
using System.Collections.Generic;

namespace Channel_Native
{
    public static class MainSave
    {
        public static Role Role { get; set; } = Role.Management;
        public static string ServerURL { get; set; }
        public static string PluginName { get; set; }
        public static int ServerPID { get; set; }

        public static int PluginID { get; set; }
        public static long CurentQQ { get; set; }
        public static int BoardCastPort { get; set; }
        public static int LogerMaxCount { get; set; }
    }
}
