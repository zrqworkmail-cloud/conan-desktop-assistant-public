# 插件目录

构建脚本生成随程序分发的 `plugin-packages/catalog.json` 和独立插件包，并把目录摘要嵌入主程序。新增随附包时，在 `work/offline-pet/plugins/build.ps1` 添加打包步骤和目录项目，更新功能包版本，再运行构建与插件测试。更新后可重新启用；已加载的旧程序集会保留在进程中直到程序退出。

插件中心也可以手动加载 HTTPS 外部目录。格式示例（摘要为对应插件文件的 SHA-256）：

```json
{"Schema":1,"Plugins":[{"Id":"example-feature","Name":"示例功能","Version":"1.0.0","Description":"功能说明","Package":"https://example.com/example-feature.conanplugin","Sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}
```

外部目录与下载包只验证格式、摘要和插件标识；它们没有发布者签名，摘要不证明来源可信。只从用户信任的来源加载并在安装时核对说明。程序不会自动启用新安装的代码。默认目录托管于 https://github.com/zrqworkmail-cloud/conan-desktop-plugins ，用户也可以手动指定外部目录；手动加载的外部目录仅在当前运行期间保留。
