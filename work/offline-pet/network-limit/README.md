# 可选整机下载限速组件

此目录中的 ConanBandwidth.cs 与 ../src/DownloadLimiter.cs 为本项目组件源码。WinDivert.dll、WinDivert64.sys来自官方WinDivert 2.2.2-A x64发布包，没有修改。运行通过动态P/Invoke调用；采用LGPLv3选项，完整许可见WinDivert-LICENSE.txt。安全版运行时核对内置哈希；如需使用自行修改的兼容动态库，可从本项目源码调整 build.ps1 中的预期哈希后重新构建主程序及组件，不能仅替换发布包 DLL。上游源码和完整发布包：

- https://github.com/basil00/WinDivert/tree/v2.2.2
- https://github.com/basil00/WinDivert/releases/tag/v2.2.2
- https://github.com/basil00/WinDivert/releases/download/v2.2.2/WinDivert-2.2.2-A.zip

源码检出若缺少被Git忽略的二进制文件，请从上述官方包x64目录复制两个文件到本目录，再按SOURCES.json核验SHA256；执行../build.ps1编译ConanBandwidth.exe并组装dist/network-limit。不要修改或关闭驱动签名验证。

启用需要独立UAC和界面确认；默认关闭，不写开机启动。全机模式只处理非回环TCP/UDP入站负载，共享一个平均速率预算；并非路由器端硬性限速。退出、停止事件或父进程消失会关闭接收并排空队列，再释放句柄。测试模式固定仅筛选127.0.0.1:53111的回环TCP负载，不用于正常全机模式。

组件不修改WinDivert启动项；官方库可能注册/加载自己的驱动服务。结束后关闭驱动句柄；服务项是否残留由驱动管理，不承诺自动删除服务注册。

## v5.20.2 受保护启动

管理员启动目标改为系统 PowerShell；它执行主程序内嵌的校验脚本，将经过 SHA-256 核验的组件复制到 `%ProgramFiles%\ConanDesktopAssistant.Security\<helper-sha256>`。目录与文件只授予 Administrators / SYSTEM 写权限，Users 只能读取执行。启动前再次检查所有者、权限、链接与文件内容，并持有只读句柄至组件退出。可写目录、哈希不符或链接会拒绝启动；不会自动接管已有不可信目录。退出停止限速，受保护副本保留。主程序本身未作商业代码签名，应从可信渠道取得发布包。
