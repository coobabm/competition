# Unity MCP 连接

已安装 CoplayDev 的 MCP for Unity 10.0.0，并为本机 Codex 配置 `unityMCP`。

## 使用

1. 保持 RipplePrototype 项目在 Unity 中打开。
2. 修改 MCP 配置后，完全退出并重新打开 Codex，使新工具生效。
3. 在 Codex 设置的 MCP 服务器列表中确认 `unityMCP` 已启用。
4. 可以让 Codex「读取当前 Unity 场景」或「检查 Unity 报错」。

Unity 中的连接面板位于 `Window → MCP for Unity → Toggle MCP Window`。
本机使用 stdio 方式，由 Codex 启动服务，Unity 本地桥接端口由插件管理。

实例标识：`RipplePrototype@b89b950f`。如果同时打开多个 Unity 工程，先查询实例列表再选择目标。

## 本机安装位置

- Unity 插件：项目内 `Packages/com.coplaydev.unity-mcp`。
- Codex 配置：`C:\Users\xomem\.codex\config.toml` 的 `mcp_servers.unityMCP`。
- 服务及独立 Python 环境：`C:\Users\xomem\Documents\ChatGPT\games\.unity-mcp-tools\unity-mcp\Server`。

`.unity-mcp-tools` 是正在使用的服务目录，请保留。
客户端使用已安装的本地服务；插件自动改写客户端配置的选项已关闭。

此连接用于 AI 操作本机 Unity，不提供朋友之间的项目同步；多人协作仍需另外完成版本管理仓库和邀请。
