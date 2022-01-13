## Channel-Native
Channel-Core 插件，用于QQ频道的酷Q插件兼容，能跑就行

## CQP接口
由于相关接口差距过大，只实现了：
- 群消息接口[`CQ_sendGroupMsg`]
- 获取应用目录[`CQ_getAppDirectory`]
- 添加日志[`CQ_addLog`]
- 添加崩溃日志[`CQ_setFatal`]

待实现：
- 下载图片[`CQ_getImage`]
- 撤回消息[`CQ_deleteMsg`]

## CQ码
- AT`[CQ:at]`
- 图片`[CQ:image]`
- 表情`[CQ:face]`