# 灵光：团队同步

## 仓库与首次打开

- GitHub 私有仓库：`coobabm/competition`，默认分支 `main`。
- Unity Editor：`6000.4.5f1`，由 `ProjectSettings/ProjectVersion.txt` 锁定。
- 在 GitHub Desktop 登录已获仓库权限的账号，通过 File → Clone Repository 克隆；不要用 Download ZIP 代替克隆。
- 克隆完成后，在 Unity Hub 中 Add project from disk，选择克隆目录。首次启动会重新生成 Library。
- 主要游戏场景：`Assets/LingGuang/Scenes/LingGuang.unity`。
- 图片、音频、模型和字体由 Git LFS 管理。Desktop 正常克隆/拉取会下载素材；若手动使用 Git，先安装 Git LFS，再运行 `git lfs pull`。

## 每次协作

1. 开工前 Fetch origin；出现 Pull origin 时先拉取。拉取前保存场景并处理本地改动。
2. 修改前沟通场景/Prefab 分工，同一时间尽量不要多人修改同一个文件。
3. 完成功能后在 Desktop 的 Changes 核对文件，填写说明，Commit，再 Push origin。
4. 队友 Fetch/Pull 后再打开或刷新 Unity。只按保存不会自动上传。
5. 较大功能在独立分支开发，通过 Pull Request 合并；冲突时不要覆盖队友文件或强制推送。

## 必须保留与必须排除

- 必须一起同步 `Assets`（含 `.meta`）、`Packages`、`ProjectSettings`。不要单独删除或重新生成别人的 `.meta`。
- 已忽略 Library、Temp、Logs、构建产物、Evidence、恢复场景、截图，以及本机 AI 配置和凭据文件。
- 原有第三方资源保持工程可运行；队友仍需遵守相应资源授权，私有仓库不等于取得额外许可证。
- UVCS 未启用；不要同时把这个目录连接成另一套云版本控制工作区。

## 权限

私有仓库需要仓库所有者邀请具体队友，并由对方接受邀请。不要把账号密码或令牌发给队友。
