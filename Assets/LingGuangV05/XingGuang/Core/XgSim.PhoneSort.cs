using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One of the folders 阿杰 wants his phone pictures in.</summary>
    public sealed class XgPhotoFolder
    {
        public string id = "", zh = "", en = "";
        /// <summary>Share of the 4,312 pictures that really belong here.</summary>
        public double share;
    }

    /// <summary>
    /// A picture of the backup the player sees on its own. <see cref="photo"/> is a licensed photo under Media/Photos;
    /// empty means the desktop draws it (<see cref="draw"/>: a chat screenshot, a game screenshot, a selfie, a meme,
    /// an ID card). Every other picture of the 4,312 only shows as a blur in the count.
    /// </summary>
    public sealed class XgPhotoShot
    {
        public string id = "", folder = "", photo = "", draw = "", file = "";
        /// <summary>Taken sideways: the desktop shows it turned a quarter.</summary>
        public bool sideways;
        /// <summary>Scripted pick ("" = the readiness decides): the moments the story needs, whatever the training.</summary>
        public string fixedPick = "";
    }

    public sealed partial class XgState
    {
        /// <summary>阿杰的手机备份: 0 not yet, 1 U 盘 on the desk, 2 sorted (checking it), 3 the next morning, 4 done.</summary>
        public int psStep;
        /// <summary>Its pick per shown picture, "shot:folder"; and the shots the player moved to the right folder.</summary>
        public List<string> psPicks = new List<string>();
        public List<string> psFixed = new List<string>();
        /// <summary>The pictures it put right, of all 4,312.</summary>
        public int psRight;
        /// <summary>The ID-card photos in 其他: 0 not looked at, 1 tell 阿杰 to delete them, 2 not my business.</summary>
        public int psIds;
        /// <summary>What he told 阿杰 it was: 0 not asked yet, 1 a little script, 2 by hand.</summary>
        public int psAlibi;
    }

    /// <summary>
    /// 阿杰的手机备份 (2026-10-18), the end of stage 2. 阿杰 changed to an OPPO R9 and left a U 盘 with four years of
    /// phone pictures, 4,312 of them, to be sorted into eight folders by morning. Eight folders is a choice of eight:
    /// what the multilayer perceptron can do. It sorts them at the exam's own accuracy; the player can move the wrong
    /// ones it shows (each move is a label for its seeing side). Scripted moments: the panda meme and the Overwatch
    /// screenshot go right, 阿杰's cat-ear selfie lands in 表情包, the beach shot sideways lands in 其他 (it knows
    /// the squares, not the thing: the reason for convolution), and 其他 holds 31 photos of the net café customers' ID
    /// cards. 阿杰 never learns what sorted them. Pure; the desktop (AjiePhoneSort) plays it.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int PhoneTotal = 4312, PhoneIdCards = 31;
        public const string PhoneChronicle = "phonesort";
        /// <summary>The morning is played in YY's 网吧 group, where 阿杰 asks.</summary>
        public const string PhoneConversation = "netbar";

        static XgPhotoFolder F(string id, string zh, string en, double share) => new XgPhotoFolder { id = id, zh = zh, en = en, share = share };

        public static readonly XgPhotoFolder[] PhoneFolders =
        {
            F("meme", "表情包", "Memes", .30),
            F("game", "游戏截图", "Game screenshots", .14),
            F("chat", "聊天截图", "Chat screenshots", .12),
            F("selfie", "自拍", "Selfies", .08),
            F("food", "吃的", "Food", .10),
            F("scenery", "风景", "Places", .11),
            F("pet", "猫狗", "Cats & dogs", .06),
            F("other", "其他", "Other", .09),
        };

        static XgPhotoShot P(string id, string folder, string photo, string file, string fixedPick = "") => new XgPhotoShot { id = id, folder = folder, photo = photo, file = file, fixedPick = fixedPick };
        static XgPhotoShot D(string id, string folder, string draw, string file, string fixedPick = "") => new XgPhotoShot { id = id, folder = folder, draw = draw, file = file, fixedPick = fixedPick };

        /// <summary>The pictures shown one by one, in the order they come off the stick.</summary>
        public static readonly XgPhotoShot[] PhoneShots =
        {
            D("panda", "meme", "panda", "IMG_20150311_熊猫头.jpg", "meme"),
            P("geyou", "meme", "chat_geyou", "葛优躺.png"),
            D("ow1", "game", "game", "Screenshot_20160525_守望先锋.png", "game"),
            P("dinner", "food", "avatar_family_group", "IMG_20151231_年夜饭.jpg"),
            D("chat1", "chat", "chat", "Screenshot_20140806_QQ.png"),
            P("cat", "pet", "avatar_guaji", "IMG_20140421.jpg"),
            D("selfie1", "selfie", "selfie", "IMG_20130910_自拍.jpg"),
            P("melon", "food", "avatar_chigua", "IMG_20150718.jpg"),
            P("tulip", "scenery", "avatar_cousin", "IMG_20160403_公园.jpg"),
            D("ow2", "game", "game", "Screenshot_20160601_守望先锋.png"),
            P("dog", "pet", "avatar_xiaogang", "IMG_20151002_表哥家的狗.jpg"),
            D("meme2", "meme", "meme", "稳住我们能赢.jpg"),
            P("gpu", "other", "gpu_gtx980ti", "IMG_20160520_网吧新卡.jpg"),
            D("chat2", "chat", "chat", "Screenshot_20151111_微信.png"),
            P("airport", "scenery", "era_escape", "IMG_20140201_首都机场.jpg"),
            D("catear", "selfie", "catear", "IMG_20160612_猫耳.jpg", "meme"),
            P("milktea", "food", "gift_milktea", "IMG_20160430.jpg"),
            P("rabbit", "other", "avatar_xiaobai", "IMG_20150405_兔子.jpg"),
            D("game3", "game", "game", "Screenshot_20150820_英雄联盟.png"),
            P("street", "scenery", "avatar_netbar", "IMG_20160218_网吧门口.jpg"),
            P("ginger", "pet", "chat_qingwen_cat", "IMG_20160315_楼下的猫.jpg"),
            P("keyboard", "other", "avatar_me", "IMG_20160222_键盘.jpg"),
            new XgPhotoShot { id = "beach", folder = "scenery", photo = "avatar_haixing", file = "IMG_20140715_海边.jpg", sideways = true, fixedPick = "other" },
            P("tea", "food", "avatar_wang", "IMG_20151120.jpg"),
            P("lotus", "scenery", "avatar_xiaolu", "IMG_20150806_荷花.jpg"),
            D("selfie2", "selfie", "selfie", "IMG_20160101_跨年.jpg"),
            P("stones", "other", "avatar_xiezhi", "IMG_20160315_围棋.jpg"),
            D("idcard", "other", "idcard", "IMG_20160409_登记.jpg", "other"),
        };

        public static XgPhotoFolder PhoneFolder(string id) => Array.Find(PhoneFolders, f => f.id == id);
        public static XgPhotoShot PhoneShot(string id) => Array.Find(PhoneShots, s => s.id == id);

        /// <summary>阿杰 left the U 盘: stage 2, the 「看图说词」 exam open (or passed and not claimed), not played on this save.</summary>
        public bool PhoneSortDue => S.stage == 2 && S.psStep == 0 && (ExamReady || ExamPassPending);

        /// <summary>Stage 2 does not end before the pictures are sorted and 阿杰 has them back.</summary>
        public bool PhoneSortHolds => DesktopHosted && S.stage == 2 && NextAbility == 3 && S.psStep < 4;

        public void PhoneSortStart() { if (PhoneSortDue) S.psStep = 1; }

        /// <summary>How many of the pictures it puts right: 55% with an empty sample bar, 95% once the bar is well over its line.</summary>
        public double PhoneAccuracy => .55 + .4 * ExamChance(ExamSamplesReady);

        /// <summary>Its folder for a shown picture: the scripted moments, else right by the accuracy, the same every time for this save.</summary>
        string PhonePickFor(XgPhotoShot shot)
        {
            if (shot.fixedPick.Length > 0) return shot.fixedPick;
            var rng = new Random(StableHash("ps." + shot.id) ^ S.dataSalt * 131);
            if (rng.NextDouble() < PhoneAccuracy) return shot.folder;
            // Wrong: a neighbour it confuses it with (a selfie for a meme, a cat for a dog…), else the next folder.
            switch (shot.folder)
            {
                case "selfie": return "meme";
                case "meme": return "chat";
                case "pet": return shot.id == "dog" ? "scenery" : "other";
                case "other": return shot.id == "rabbit" ? "pet" : "game";
                case "food": return "scenery";
                case "scenery": return "food";
                case "chat": return "game";
                default: return "other";
            }
        }

        /// <summary>
        /// 「让它来分」: every picture is sorted now (the desktop plays it afterwards). Returns the folder each shown picture
        /// went to, in <see cref="PhoneShots"/> order. Moves on to checking it.
        /// </summary>
        public List<string> PhoneSortRun()
        {
            var picks = new List<string>();
            if (S.psStep != 1) { foreach (var shot in PhoneShots) picks.Add(PhonePick(shot.id)); return picks; }
            if (S.psPicks == null) S.psPicks = new List<string>();
            S.psPicks.Clear();
            int right = 0;
            foreach (var shot in PhoneShots)
            {
                string pick = PhonePickFor(shot);
                S.psPicks.Add(shot.id + ":" + pick);
                picks.Add(pick);
                if (pick == shot.folder) right++;
            }
            int rest = PhoneTotal - PhoneShots.Length;
            S.psRight = right + (int)Math.Round(rest * PhoneAccuracy);
            S.psStep = 2;
            return picks;
        }

        /// <summary>Where it put a shown picture (after a move by the player, the right folder). "" before sorting.</summary>
        public string PhonePick(string shotId)
        {
            if (S.psFixed != null && S.psFixed.Contains(shotId)) { var s = PhoneShot(shotId); return s != null ? s.folder : ""; }
            if (S.psPicks == null) return "";
            foreach (var e in S.psPicks) if (e.StartsWith(shotId + ":", StringComparison.Ordinal)) return e.Substring(shotId.Length + 1);
            return "";
        }

        /// <summary>The shown pictures in the wrong folder now: what the player can still move.</summary>
        public List<XgPhotoShot> PhoneWrong()
        {
            var list = new List<XgPhotoShot>();
            foreach (var shot in PhoneShots) { string pick = PhonePick(shot.id); if (pick.Length > 0 && pick != shot.folder) list.Add(shot); }
            return list;
        }

        /// <summary>
        /// The player moved a picture to <paramref name="folder"/>. Right: it stays there, counts as one more right and as a
        /// label for its seeing side (one picture it is taught by hand). Wrong folder: nothing changes (false).
        /// </summary>
        public bool PhoneMove(string shotId, string folder)
        {
            var shot = PhoneShot(shotId);
            if (S.psStep != 2 || shot == null || folder != shot.folder || PhonePick(shotId) == shot.folder) return false;
            if (S.psFixed == null) S.psFixed = new List<string>();
            S.psFixed.Add(shotId);
            S.psRight = Math.Min(PhoneTotal, S.psRight + 1);
            var d = HandDataset(XgTrack.Vision);
            if (d != null) AddLabel(d.id);
            return true;
        }

        /// <summary>How many pictures each folder holds now (its sorting, with the player's moves).</summary>
        public int PhoneCount(string folderId)
        {
            if (S.psStep < 2) return 0;
            double acc = Math.Max(0, Math.Min(1, (S.psRight - Shown(true)) / Math.Max(1.0, PhoneTotal - PhoneShots.Length)));
            int rest = PhoneTotal - PhoneShots.Length, n = 0;
            // The pictures not shown: each folder keeps its right share and takes the others' wrong share by size.
            double kept = 0, taken = 0;
            foreach (var f in PhoneFolders)
            {
                double size = rest * f.share;
                if (f.id == folderId) kept = size * acc;
                else taken += size * (1 - acc) * PhoneFolder(folderId).share / (1 - f.share);
            }
            n = (int)Math.Round(kept + taken);
            foreach (var shot in PhoneShots) if (PhonePick(shot.id) == folderId) n++;
            return n;
        }

        int Shown(bool right)
        {
            int n = 0;
            foreach (var shot in PhoneShots) if ((PhonePick(shot.id) == shot.folder) == right) n++;
            return n;
        }

        /// <summary>The ID cards in 其他: tell 阿杰 to delete them (true) or leave it.</summary>
        public void PhoneIds(bool tell) { if (S.psStep == 2 && S.psIds == 0) S.psIds = tell ? 1 : 2; }

        /// <summary>Done checking: the U 盘 goes back in the morning.</summary>
        public bool PhoneHandBack()
        {
            if (S.psStep != 2 || S.psIds == 0) return false;
            S.psStep = 3;
            return true;
        }

        /// <summary>
        /// The morning: what he says sorted them (a little script, or by hand). 阿杰's answer, zh/en pairs; the scene is
        /// over and the chapter may turn.
        /// </summary>
        public string[] PhoneAlibi(bool script)
        {
            if (S.psStep != 3) return new string[0];
            S.psAlibi = script ? 1 : 2;
            S.psStep = 4;
            NoteChronicle(PhoneChronicle, S.psRight);
            return script
                ? new[] { "脚本？你啥时候会写这个了，回头也给我整一个。", "A script? Since when do you write those? Make me one too." }
                : new[] { "手分的？四千多张？你这手速，职业选手啊。", "By hand? Four thousand? With hands like that you should go pro." };
        }

        /// <summary>The share it put right, 0–100.</summary>
        public int PhoneRightPct => (int)Math.Round(100.0 * S.psRight / PhoneTotal);

        /// <summary>For 《灵光记》.</summary>
        string PhoneChronicleBody(bool english, string self, string you)
        {
            if (english)
                return "Ajie left his old phone's pictures on a stick, " + PhoneTotal.ToString("#,0") + " of them, eight folders by morning. I put " + PhoneRightPct + "% where they belonged. His cat-ear selfie I filed under memes."
                    + (S.psIds == 1 ? " Among them were customers' ID cards; you told him to delete them, and he did." : "")
                    + " He still thinks " + (S.psAlibi == 2 ? "you sorted them by hand." : "it was a little script.");
            return "阿杰换了手机，把四年的图倒在一个 U 盘里，" + PhoneTotal.ToString("#,0") + " 张，要分成八个文件夹。" + self + "分对了 " + PhoneRightPct + "%，他那张猫耳自拍，" + self + "放进了表情包。"
                + (S.psIds == 1 ? "里面有网吧客人的身份证照片，" + you + "让他删了，他删了。" : "")
                + "他到现在还以为" + (S.psAlibi == 2 ? "是" + you + "手分的。" : "是个小脚本。");
        }
    }
}
