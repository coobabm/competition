# LessonTaught 生命周期接入合同

本文件为整合片段，不是已实施的生产修改。`XgLessonSubscription.cs` 可加入桌面程序集；`SubscriptionChecks.cs` 是含同名模拟类型的独立测试夹具，绝不可放入 Assets。所有调用在 Unity 主线程执行。

## 已有接口与缺少的接点

- 已有：`XgSim.LessonTaught : Action<XgCard, XgAnswer>`。辅助类原样转交参数，不重新取当前卡，不筛选 corrected，不补发历史事件。
- 缺少：`XingGuangController` 在完整替换 `Sim` 后同步通知订阅方的接口。当前仅通过 runtime.Changed 内部调用 BindSave；不能只轮询 Update，否则新实例在下一帧前发生的第一条事件会漏掉。
- 缺少：壁纸实际的 `OnLessonTaught(XgCard, XgAnswer)` 视觉接收器，由壁纸整合方提供。是否仅亮 corrected、怎样定位节点不在本任务范围。

## 控制器的最小接口合同

在 `XingGuangController.cs` 的 `public XgSim Sim { get; private set; }` 后新增：

```csharp
// Main-thread notification; null means publisher lifetime has ended.
public event Action<XgSim> SimRebound;
```

在 `BindSave` 的现有上下文插入一行（完成新 Sim 初始化后、视图可能恢复输入前）：

```csharp
if (state != null) RaiseProgress(silent: true);
SimRebound?.Invoke(Sim);
if (View != null) View.Bind(this);
```

`OnDestroy` 开头先通知 null 并清理这一新事件，然后保留原有的 runtime 和 GameText 解绑：

```csharp
SimRebound?.Invoke(null);
SimRebound = null;
```

若以后引入不经过 BindSave 的 Sim 替换/清空入口，也必须同步发此通知；不得仅按存档槽编号、卡片 id 或 ChapterOneSim 判断订阅身份。真正事件发布者是 XgSim 引用。该接口只用于呈现，不应在通知回调中回答卡片、切存档或抛异常。

## 壁纸订阅方片段

合并到承载实际视觉回调的 MonoBehaviour；若届时已有同名生命周期方法，合并其方法体，不重复声明。

```csharp
XgLessonSubscription lessonSubscription;
XingGuangController lessonController;

void StartLessonListening()
{
    if (!isActiveAndEnabled) return;
    if (lessonSubscription == null)
        lessonSubscription = new XgLessonSubscription(OnLessonTaught);

    // Detach even if the controller object was destroyed (Unity fake-null).
    if (!ReferenceEquals(lessonController, null))
        lessonController.SimRebound -= RebindLesson;
    lessonController = controller;
    if (lessonController != null)
        lessonController.SimRebound += RebindLesson;
    RebindLesson(lessonController != null ? lessonController.Sim : null);
}

void RebindLesson(XgSim sim)
{
    lessonSubscription?.Bind(isActiveAndEnabled ? sim : null);
    // Integration hook: cancel/reset any previous publisher's visual pulse here
    // when its identity changes. The binding helper owns subscriptions only.
}

void StopLessonListening()
{
    if (!ReferenceEquals(lessonController, null))
        lessonController.SimRebound -= RebindLesson;
    lessonController = null;
    lessonSubscription?.Bind(null);
    // Integration hook: cancel/reset the active pulse here (no delayed replay).
}

void OnEnable() { StartLessonListening(); }
void OnDisable() { StopLessonListening(); }
void OnDestroy()
{
    StopLessonListening();
    lessonSubscription?.Dispose();
    lessonSubscription = null;
}
```

当前 `Install` 是先 AddComponent，再赋 controller，再创建 group/firstPoints。AddComponent 会提前触发 OnEnable。因此 `OnLessonTaught` 不得假定这些对象已就绪；**在现有 `view.Observe();` 之后显式调用 `view.StartLessonListening();`**，此时 UI 与 controller 均已赋值。最初 OnEnable 的 null 绑定合法且无回调。完整视觉处理器只在可见对象就绪后接入。

换 controller 时亦调用 StartLessonListening，先解除旧 controller 通知，再跟随新 controller.Sim。重复调用不增加 LessonTaught 订阅。若以后 controller 可以在不销毁对象的情况下结束服务，须发 SimRebound(null)，恢复时再发当前 Sim。这里的 OnDisable 合同指承载壁纸反馈的订阅组件禁用。

## 必須保持的边界

| 转换 | 同步行为 |
| --- | --- |
| 首次可用 | 绑定当前 XgSim 一次；不补发旧教学 |
| A → B 换档或重新开始 | 先从 A 移除自己的委托，再订阅 B；B 第一条事件即时可见 |
| 同一实例重复通知 | 不添加重复委托 |
| 禁用或暂时无发布者 | Bind(null)；旧事件与禁用期间事件不播放、不积压 |
| 禁用期间换档、随后启用 | 从 controller.Sim 重新读取；直接绑定最新实例 |
| 销毁 | 清除 controller 通知与 LessonTaught，Dispose 可重复；不能复用已 Dispose 的辅助类 |
| 旧事件已捕获委托，期间切换/禁用/销毁 | 旧 ticket 失效，即使 A → B → A 也不恢复旧回调 |

不要全局清空 LessonTaught、不要用新的匿名 lambda 做 -=、不要只在 OnDestroy 解绑、不要在关闭普通应用窗口时误清理仍然活跃的桌面壁纸。

## 主聊天待验证

用内存模拟实例驱动上述状态转换，确认切换后旧实例订阅数归零（其他合法订阅保留）；禁用期间切到 B、启用后第一条事件只触发一次。再统一运行 Unity 编译及 `DesignFirstLessonChecks.Run`。真实场景应检查禁用/销毁后没有残留闪光，重新开始后第一张成功纠正立即闪一次；截图/视觉检查由主聊天负责。本任务不调用存档读写入口。
