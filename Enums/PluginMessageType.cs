namespace Channel_Native.Enums
{
    public enum PluginMessageType
    {
        /// <summary>
        /// 插件_发送普通文本
        /// </summary>
        SendMsg,
        /// <summary>
        /// 插件/管理端_发送/接收插件信息
        /// </summary>
        PluginInfo,
        /// <summary>
        /// 
        /// </summary>
        HeartBeat,
        /// <summary>
        /// 管理端_要求插件断开连接
        /// </summary>
        Diconnect,
        /// <summary>
        /// 插件_插件执行异常
        /// </summary>
        Error,
        /// <summary>
        /// 插件_结束处理消息
        /// </summary>
        FinMessage,
        /// <summary>
        /// 管理端_要求启用插件
        /// </summary>
        Enable,
        /// <summary>
        /// 管理端_要求禁用插件
        /// </summary>
        Disable,
        /// <summary>
        /// 插件_写日志
        /// </summary>
        Log,
        /// <summary>
        /// 
        /// </summary>
        ReceiveMessage
    }
}
