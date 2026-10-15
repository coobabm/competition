# secret.6 可直接采用的中英文文案

日期：2026-10-13（Asia/Singapore）。仅适用于已按现有可见性规则显示的节点。

## 中文

名称：预训练配置指南

解释：查看预训练所需的参数、数据和道具倍数如何配合。

效果：购买后立即显示配置提示，无需装备或等待训练；本指南不提供训练加成或规模倍数。若在跑通预训练前购买，将放弃自悟奖金 ¥11250（本节点价格 ×1.5）。

## English

Name: Pre-training configuration guide

Explanation: Read how parameters, data and item multipliers combine for pre-training.

Effect: Buying immediately shows setup hints; no equipping or training is needed. This guide adds no training bonus or scale multiplier. Buying before completing pre-training forfeits a self-discovery bonus of ¥11250 (this item's price ×1.5).

## 接入约定

- 名称与解释保留现有字符串；只替换 Effect 的 secret.6 分支。
- 补丁中的奖金读取 `sim.NodeCost(node) * XgSim.SelfInsightBonus`，不写死 11250。¥ 为游戏经费单位；×1.5 是自悟奖金与节点购买价的比例，不是训练增益。
- 当前购买价 ¥7500。发现节点不收费也不触发购买提示；成功购买后立即显示提示。没有装备步骤、训练生效延迟、等级或重复叠加。
- 不在隐藏节点上提前展示这份文案，也不在解释中免费展开指南内全部配方。
