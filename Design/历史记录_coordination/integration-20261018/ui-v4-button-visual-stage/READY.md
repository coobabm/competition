# 整按钮视觉按压候选交付

补丁：`button-visual.patch`。仅 staging 写入，未运行 Unity。

实现：深色 XgUi.Button 的根保留透明 Image 命中框、Button、布局、业务 Knock 和输入组件；新增拉伸 Visual 子节点承载真实可见 Image、Outline 和 Label。XgBtn.image 指真实 face，Button.targetGraphic 同步；XgBtn.Visual 提供新增装饰挂载点。XgButtonPressFeedback.PressTarget 仅用于 V4Press，原浅色仍只使用 Label。按压、回弹、取消、减特效均针对同一个 Visual，由独立 pressFx 管理。

显式迁移训练副标题/电费、生活事件详情、Cortex状态、Assembly卡片纯装饰；Assembly嵌套删除按钮仍在原根，未移动交互控件。Contracts、Assembly新增/读取边框改为实际 face的Outline，避免透明Hit描边；浅色face仍等于根所以保持行为。没有运行时自动重挂孩子，没有每帧扫描或颜色镜像。

测试：UiV4PressChecks现在使用真实深色/浅色工厂与额外装饰、独立嵌套按钮。验证背景+Label+Sub共用Visual、固定透明命中、.95持续/重复按下不累积、松开归一、cancel/disable/reduced、键盘一次业务、根Knock在取消按压时继续。沿用原检查入口与调用路径；本次只完成静态编译。

验证结果：
- game-compile.log：游戏程序集候选编译退出 0。
- editor-compile.log：Editor使用候选游戏引用编译退出 0。
- git apply --check --ignore-space-change：通过。

边界：未将侧栏/科技等直接构造的裸Button自动转换；ZhongbaoSkin.Outlined外框不是按钮内部装饰，未在此任务中更改，避免和教页主题代理冲突。其深色新主题如需整框跟随，请使用XgBtn.Visual构造可见边框；不要搬动外层布局Frame。父业务Knock自然乘上Visual按压，但两个栈各自恢复不覆盖。

合并请优先patch小hunk，不整文件覆盖正在修改的主题。8个文件全部按当时主工作区生成，最终Unity实测仍由根串行安排。
