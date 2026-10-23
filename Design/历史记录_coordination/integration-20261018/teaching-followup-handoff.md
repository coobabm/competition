# 教页后续小单元（2026-10-18）

所有下列功能补丁仍在 coordination，未写主工程。唯一直接改main的是授权修正的 TeachingPackInteractionChecks.cs；root已继续修改其fixture，禁止用我早期副本覆盖。

## 顺序
1. teaching-traces/question-traces.patch：6文件，记录真实操作与图片/棋盘快照。
2. teaching-ai/more-ai-answers.patch：4文件，补扫雷/24/五子棋/九宫格/象棋代答。
3. stage-lessons/stage-lessons.patch：7文件，新增阶段4/5/6免费练习桌。19个数据包manifest保持不变。
4. teaching-moles/mole-ai-and-drawing-return.patch：2文件，地鼠每洞AI+真实操作日志，手写返回。

Core与Desktop单独Bee/Roslyn编译均通过；需root逐单元Unity验收。后续moles末尾的隐藏洞文字同步关闭为编译后的一行修正，需随Unity确认。

## 功能边界
- stage4：6道短句含正确句、错字词，可选没错；正确反馈给修正词。
- stage5：6道中英翻译含准/偏/错；偏的要再点错误词，记录真实等级+词。
- stage6：5道择优，答案比较基于准确、帮助性。
- 这些是第一批可玩题，不等于题量/首次进场动效打磨已完成。
- 代答可无限显示；棋盘建议只在局面变动算，不每帧算。地鼠逐洞显示；买了Sigmoid的新增辅助行显示把握。
- 原版spy用AI虚拟参赛者输出，尚未补完整选择/投票日志；手写页自身的识别建议未统一至新代答布局。
- Bias门槛拖动、疑似错题托盘、难例复核角标、Sigmoid按把握排序仍未做。
- 新图片快照数组已存并深拷贝，消费者重建XgCard须同步复制 teachingImageKeys/teachingPosition（已通知训练代理）；棋盘/九宫格缩略图渲染尚未做。

## 视觉规格缺口必须保留
- 当前数字为现有8×8模拟笔画放大，不是28×28原图→224加网格；未满足文档规格。
- 没有4位扭曲验证码+干扰线完整题面。
- 九宫格只有现有6张带来源照片，不满足8类×10张。
- 照片每类5张尚未实现；不能把不相干照片塞入分类来凑数量。
- 原生表情绘制/引用图片来源、Lichess制作名单仍需最终署名核对。

## 验收入口
Main下真实教页逐包构建/点击，19包逐项report；全解锁是显式QA fixture，不冒称买过。
root已把fixture改为SetSimulationForTests+完整stage6日期与租金结算日，以免阶段跳转触发无关剧情抢屏。
已捕获的生产错误由root修正：隐藏Taskbar启动协程、隐藏TrayPopups启动协程。只有纯UnityEditor.SearchDatabase异常分开记，生产异常不能过滤。
