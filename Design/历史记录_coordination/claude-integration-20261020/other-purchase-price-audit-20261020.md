# 其他购买入口价格核对（2026-10-20）

仅静态核对，未启动 Unity。

## 事件购买白名单
- Assets/LingGuangV05/Resources/LingGuangV05/Life/life_events_100.json：12 项报价通过；其余88条事件完全一致。
- Design/data/life_events_100.json：12 项报价通过；其余88条事件完全一致。
- e003：¥300
- e010：¥100
- e013：¥500
- e021：¥500
- e028：¥990
- e030：¥450
- e058：¥400
- e061：¥680
- e081：¥800
- e091：¥990
- e096：¥690
- e099：¥500

## 展示与扣款
- 事件 LifeDataView、LifeEffectDetail 与 ApplyLifeDataChoice 同读 effects.money；对白中显式价格同步修正（硅脂、迅雷、宽带、泡沫）。
- 科技数据后续批次先将原价归一，再乘 techCut 折扣并向上取整；折后95元保留。
- 下载加速与宽带首次升级统一报价；爬虫按次8元调整至800元；持续众包逐条费、分包工资、续费账单、赌场筹码未改。
- 免费安装软件无扣款，保持免费。
