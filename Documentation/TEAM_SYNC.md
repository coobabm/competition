# 灵光：团队同步

## 仓库与首次打开

- GitHub 私有仓库：`tianxingguoo-collab/RipplePrototype`，《灵光》协作分支 **`lingguang`**。
- 仓库默认的 `main` 是队友原有涟漪工程，保持不变；它与本工程没有共同提交历史，不要直接互相合并或强制推送。
- Unity Editor：`6000.4.5f1`，由 `ProjectSettings/ProjectVersion.txt` 锁定。
- 在 GitHub Desktop 登录已获仓库权限的账号，通过 File → Clone Repository 克隆；不要用 Download ZIP 代替克隆。
- 克隆完成后，先在 GitHub Desktop 的 Current branch 中切换到 **`lingguang`**，再在 Unity Hub 中 Add project from disk，选择克隆目录。首次启动会重新生成 Library。
- 主要游戏场景：`Assets/LingGuang/Scenes/LingGuang.unity`。
- 图片、音频、模型和字体由 Git LFS 管理。Desktop 正常克隆/拉取会下载素材；若手动使用 Git，先安装 Git LFS，再运行 `git lfs pull`。

## 每次协作

1. 确认 Current branch 为 `lingguang`。开工前 Fetch origin；出现 Pull origin 时先拉取。拉取前保存场景并处理本地改动。
2. 修改前沟通场景/Prefab 分工，同一时间尽量不要多人修改同一个文件。
3. 完成功能后在 Desktop 的 Changes 核对文件，填写说明，Commit，再 Push origin。
4. 队友 Fetch/Pull 后再打开或刷新 Unity。只按保存不会自动上传。
5. 较大功能从 `lingguang` 新建分支，通过 Pull Request 合回 `lingguang`，不要选仓库默认的 `main`；冲突时不要覆盖队友文件或强制推送。

## 本机远程配置

- `origin` 指向队友仓库 `tianxingguoo-collab/RipplePrototype`，日常 Push/Pull 使用它。
- 本机保留的 `personal` 指向原备份仓库 `coobabm/competition`；不会自动双向同步，旧备份提交未改写。

## 必须保留与必须排除

- 必须一起同步 `Assets`（含 `.meta`）、`Packages`、`ProjectSettings`。不要单独删除或重新生成别人的 `.meta`。
- 已忽略 Library、Temp、Logs、构建产物、Evidence、恢复场景、截图，以及本机 AI 配置和凭据文件。
- 原有第三方资源保持工程可运行；队友仍需遵守相应资源授权，私有仓库不等于取得额外许可证。
- UVCS 未启用；不要同时把这个目录连接成另一套云版本控制工作区。

## 权限

私有仓库需要仓库所有者邀请具体队友，并由对方接受邀请。不要把账号密码或令牌发给队友。
