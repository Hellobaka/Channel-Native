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

## 关于发送图片
由于找不到本地图片直接上传的方法，QQ频道只允许使用网址来发送图片，并且我不能直接给你们放个图床供公共上传，所以只能用CQ码，请写`[CQ:image,url=http://xx/x.jpg]`，旧插件请等待后续更新，后续开放自定义smms图床