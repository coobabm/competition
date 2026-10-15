using System;
using LingGuangV05.Desktop.YY;
using UnityEngine;

/// <summary>In-memory regressions; never reads or modifies a player's save.</summary>
public static class DesignCrowdPrivacyChecks
{
    static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("CROWD_PRIVACY_FAIL: " + label);
        Debug.Log("CROWD_PRIVACY_CHECK: " + label);
    }

    public static void Run()
    {
        Check(YYGroup.RevealsPrivateLab("[阿杰] 灵光训练包给你准备好了"), "legacy named package");
        Check(YYGroup.RevealsPrivateLab("[阿杰] 你的训练包准备好了"), "unnamed private package");
        Check(YYGroup.RevealsPrivateLab("[小刚] 小星学会新东西了，训练包给它用", "小星"), "custom AI name");
        Check(YYGroup.RevealsPrivateLab("Your training pack is ready"), "English private package");
        Check(!YYGroup.RevealsPrivateLab("摆渡众包有公司发的数据标注兼职，按合格题数结报酬。"), "public paid work retained");
        Check(!YYGroup.RevealsPrivateLab("阿尔法狗都赢李世石了，这新闻够聊一阵了"), "public AlphaGo news retained");
        Check(!YYGroup.RevealsPrivateLab("网上有公开的训练包，可以查查新闻。"), "public datasets retained");
        var state = new YYState();
        var group = new YYConversation { id = "netbar", unread = 3 };
        state.conversations.Add(group);
        group.messages.Add(new YYMessage { from = "netbar", text = "[阿杰] 小星的训练包整理好了", gameSeconds = 42 });
        group.messages.Add(new YYMessage { from = YYChatHub.Me, text = "我的训练包有问题", gameSeconds = 43 });
        group.messages.Add(new YYMessage { from = "netbar", text = "今晚下班开黑？", gameSeconds = 44 });
        var own = new YYConversation { id = "lingguang" };
        own.messages.Add(new YYMessage { from = "lingguang", text = "你的训练包", gameSeconds = 45 });
        state.conversations.Add(own);
        Check(YYGroup.MigratePrivateKnowledge(state, "小星") == 1, "only obsolete NPC claim migrated");
        Check(group.messages.Count == 3 && group.unread == 3 && group.messages[0].gameSeconds == 42, "history structure retained");
        Check(group.messages[1].text == "我的训练包有问题" && group.messages[2].text == "今晚下班开黑？", "player and ordinary chat retained");
        Check(own.messages[0].text == "你的训练包", "private AI conversation retained");
        Check(YYGroup.MigratePrivateKnowledge(state, "小星") == 0, "migration idempotent");
        Debug.Log("CROWD_PRIVACY_PASS");
    }
}
