# 注册表补全审查与替代补丁

使用 `identity-download-registry-complete.patch` **替代**原 identity-download-registry.patch；基于生产尚未应用原补丁生成，不要两个叠加。

修复范围 3 文件：
- AiDesktopIdentity：每秒读缓存注册表，跳过Unity-null条目，保持文件稳定键不改、只更新显示名。
- WebBrowserDownloadItem：HashSet维护生命周期，返回按集合变更重建的缓存数组。连续枚举无变化时复用同一个数组，不每秒ToArray。Awake/OnEnable注册、OnDestroy注销；getter用缓存Predicate剔除未启用对象销毁后的Unity-null引用。
- WebBrowserManager：下载创建与历史恢复两处GetComponent后显式RegisterIdentityRow，覆盖inactive prefab/父级让Awake未执行的情况；ProcessDownload/ProcessComplete入口也注册，覆盖其他合法初始化入口。

生命周期边界：
- 初始inactive场景行首次seed可见。
- 场景加载事件令seed失效，下次访问补扫新增inactive场景行。
- SubsystemRegistration清空/重置并先移除后添加sceneLoaded监听，兼容关闭domain reload的重复会话，不累加监听。
- 禁用完成行不注销，仍能随玩家名字变更。
- 原代码直接返回HashSet，枚举中重入增删会抛异常；现在返回稳定快照，修改仅标记下次重建，不影响当前enumerator。
- Unity不会为任意外部代码凭空创建且始终inactive的对象调用生命周期；此类新创建方必须调用RegisterIdentityRow。已搜索当前生产下载行创建，仅WebBrowserManager两处downloadPreset实例化，均覆盖。没有为了发现未知外部创建恢复每秒全场景扫描。

检查：`DownloadRegistryLifecycleChecks.cs` 提供PlayMode协程 `Frames()`，由根隔离runner yield执行。只创建自己的行/文字/加法场景，绝不调用会写DreamOS数据的ProcessDownload/ProcessComplete；通过显式注册模拟两处真实factory边界。覆盖初始inactive、运行时active、运行时inactive clone、完成禁用行、更名不改稳定key、从未启用对象销毁、active销毁、枚举中创建、缓存数组复用、Subsystem reset后存活对象重新发现、sceneLoaded回调重新发现。

注意：sceneLoaded和SubsystemRegistration直接调用实际回调验证状态机，不等同于真的改Editor选项进出PlayMode。真正无domain reload/整场景加载仍需根runner覆盖；测试scope已注明。输出系统临时GUID目录report.json。

补丁apply --check通过。遵守要求：没有生产编辑、没有Unity、没有编译，因此本次不宣称C#编译或运行通过。
