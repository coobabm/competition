# 原创论坛知识配图

2026-10-20 为 2016 年原创论坛讨论和玩家求数据帖补充独立技术示意图。

- `era_lz2_t03_discussion.png`：多来源聊天输入经过审查后进入学习网络。对应讨论信息核验和聊天程序学习风险。
- `era_lz2_t04_discussion.png`：普通深层路径与残差捷径的概念对比。不是论文原图，不代表完整 ResNet 架构。
- `thread_ask_imagenet.png`：猫、植物、车辆的原创几何分类样例与本地存储示意，不是 ImageNet 原始样本。
- `thread_ask_translate.png`：原文与译文逐行匹配的平行语料示意，不复制任何语料正文。

四张图均由本项目原创几何绘制，无外部摄影、品牌图标或人物素材；不是历史现场照片，也不是新闻源的原始配图。字体使用项目既有 Inter Regular（SIL OFL 1.1，沿用随包字体许可）。文本是技术示意图的英文标签，中英文讨论正文保持独立本地化。

源码 `generate.py` 使用 Pillow，可重建920×720 PNG：

```
python generate.py <Inter-Regular.ttf路径> <输出目录>
```

关联讨论的事实来源（只用于讨论内容，不作为图片素材）：
- https://blogs.microsoft.com/blog/2016/03/25/learning-tays-introduction/
- https://arxiv.org/abs/1512.03385
