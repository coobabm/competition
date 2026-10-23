增量应用 title-regression.patch（以已应用的首轮标题补丁为基准），入口仍为 ShutdownReturnSmoke.RunAndExit，图形Editor，无 -quit/-nographics。三轮原始开关机与逆动画检查保留。前两轮加真实鼠标设备事件点击标题按钮：保护状态退出被拒、读档开窗、选第1槽、确认按钮出现、取消返回；分别保存中文/英文archive截图0-01b-archives-zh.png、1-01b-archives-en.png。只操作既有smoke的隔离SaveRoot与临时设备，不碰玩家存档。

修复生产层风险：HitButton不再读取全局上一帧IsPointerOverGameObject；由HUD判断传入屏幕点在自身按钮/模态内，减少预加载桌面raycaster误挡物理电源键。已选中UI按钮时方向键不再同时切换机箱按钮，Tab清选中返回物理按钮循环。

验收边界：该增加项验证真实鼠标及键盘导航结构，不宣称已执行真实键盘事件；真实读取并开机在此前TitleArchivesQa以UI回调+隔离真实读盘检查，这轮为避免破坏原动画轨迹基线选槽后取消。OS成功退出仍需独立Player验证。编译UI/Editor成功，不等于Unity运行通过。截图需主会话查看。
