using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Pure rules for the native social games. Content is authored, not external model inference.</summary>
    public static class XgMiniSocialRules
    {
        public static readonly string[] Poems = {
            "床前明月光","举头望明月","明月几时有","海上生明月","月落乌啼霜满天","春花秋月何时了","举杯邀明月","明月松间照",
            "春眠不觉晓","春风又绿江南岸","春江水暖鸭先知","春色满园关不住","春城无处不飞花","野火烧不尽","春风吹又生","春风不度玉门关",
            "夜来风雨声","风吹草低见牛羊","随风潜入夜","风急天高猿啸哀","千里莺啼绿映红","花落知多少","感时花溅泪","人面桃花相映红",
            "牧童遥指杏花村","接天莲叶无穷碧","映日荷花别样红","黄四娘家花满蹊","忽如一夜春风来","千树万树梨花开","孤舟蓑笠翁","独钓寒江雪"
        };
        public static bool PoemValid(string line, string keyword, ISet<string> used) => !string.IsNullOrEmpty(keyword) &&
            !string.IsNullOrEmpty(line) && line.Contains(keyword) && Array.IndexOf(Poems,line)>=0 && (used == null || !used.Contains(line));
        public static bool ExactMask(int selected, int truth, int count) => count>0 && count<31 && truth>0 &&
            (truth >> count)==0 && (selected >> count)==0 && selected==truth;
        public static int SafeLevel(int correct) => correct>=12 ? 12 : correct>=8 ? 8 : correct>=4 ? 4 : 0;
        public static void RepairScore(string[] truth,string[] original,string[] final,out int fixes,out int missed,out int damaged)
        {
            fixes=missed=damaged=0;
            if(truth==null || original==null || final==null || truth.Length!=original.Length || truth.Length!=final.Length)
                throw new ArgumentException("Translation arrays must have equal lengths.");
            for(int i=0;i<truth.Length;i++) {
                bool wasWrong=original[i]!=truth[i], nowWrong=final[i]!=truth[i];
                if(wasWrong) { if(nowWrong) missed++; else fixes++; }
                else if(nowWrong) damaged++;
            }
        }
        // First answer is correct; native UI shuffles all four. Increasing blocks of difficulty.
        public static readonly string[][] Quiz = {
            new[]{"彩虹一般有几种颜色？","七种","五种","六种","八种"},
            new[]{"中国的首都是哪座城市？","北京","上海","广州","南京"},
            new[]{"下列哪种动物是哺乳动物？","鲸鱼","鲨鱼","乌贼","海龟"},
            new[]{"标准大气压下水的沸点？","100 ℃","90 ℃","80 ℃","120 ℃"},
            new[]{"一米等于多少厘米？","100","10","1000","50"},
            new[]{"一支足球队场上含门将共几人？","11","9","10","12"},
            new[]{"《红楼梦》的作者是？","曹雪芹","吴承恩","施耐庵","罗贯中"},
            new[]{"化学符号 Au 代表什么？","金","银","铝","铜"},
            new[]{"国际象棋棋盘共有多少格？","64","81","100","49"},
            new[]{"下列哪项不属于中国古代四大发明？","丝绸","造纸术","火药","指南针"},
            new[]{"真空中的光速约为每秒？","30 万公里","3 万公里","300 万公里","3000 公里"},
            new[]{"《哈利·波特》系列的作者是？","J.K.罗琳","托尔金","乔治·马丁","丹·布朗"},
            new[]{"二进制 1010 对应十进制？","10","8","12","16"},
            new[]{"一个字节通常有多少位？","8","4","16","32"},
            new[]{"三角形内角和（欧氏平面）？","180°","90°","270°","360°"},
            new[]{"概率为 1/2 的独立事件连续发生两次概率？","1/4","1/2","1/3","1"},
            new[]{"哪个不是质数？","21","2","13","17"},
            new[]{"对已排序数组进行二分查找的时间复杂度？","O(log n)","O(n)","O(n²)","O(n log n)"},
            new[]{"过拟合更常表现为？","训练好，验证差","训练差，验证好","两者都完美","两者恒为零"},
            new[]{"计算机堆栈通常遵循？","后进先出","先进先出","随机出队","按值大小出队"},
            new[]{"两个公正骰子点数和为 7 的概率？","1/6","1/12","1/36","1/3"},
            new[]{"等比数列 1,2,4,8 的下一项？","16","12","15","32"},
            new[]{"HTTP 状态码 404 通常表示？","资源未找到","请求成功","内部错误","未授权"},
            new[]{"梯度下降参数更新通常沿着？","负梯度方向","正梯度方向","固定坐标轴","随机常数方向"}
        };

        // English counterpart of Quiz: same order, same shape {question, right, wrong1, wrong2, wrong3}.
        public static readonly string[][] QuizEn = {
            new[]{"How many colors does a rainbow usually have?","Seven","Five","Six","Eight"},
            new[]{"Which city is the capital of China?","Beijing","Shanghai","Guangzhou","Nanjing"},
            new[]{"Which of these animals is a mammal?","Whale","Shark","Squid","Sea turtle"},
            new[]{"Boiling point of water at standard atmospheric pressure?","100 ℃","90 ℃","80 ℃","120 ℃"},
            new[]{"How many centimeters are in one meter?","100","10","1000","50"},
            new[]{"How many players does a football team field, goalkeeper included?","11","9","10","12"},
            new[]{"Who wrote Dream of the Red Chamber?","Cao Xueqin","Wu Cheng'en","Shi Nai'an","Luo Guanzhong"},
            new[]{"What does the chemical symbol Au stand for?","Gold","Silver","Aluminum","Copper"},
            new[]{"How many squares does a chessboard have?","64","81","100","49"},
            new[]{"Which of these is not one of the Four Great Inventions of ancient China?","Silk","Papermaking","Gunpowder","Compass"},
            new[]{"Roughly how fast does light travel in a vacuum, per second?","300,000 km","30,000 km","3,000,000 km","3,000 km"},
            new[]{"Who wrote the Harry Potter series?","J.K. Rowling","Tolkien","George R. R. Martin","Dan Brown"},
            new[]{"Binary 1010 equals what in decimal?","10","8","12","16"},
            new[]{"How many bits are in a byte, usually?","8","4","16","32"},
            new[]{"Sum of the interior angles of a triangle (Euclidean plane)?","180°","90°","270°","360°"},
            new[]{"Probability that an independent event with probability 1/2 happens twice in a row?","1/4","1/2","1/3","1"},
            new[]{"Which one is not a prime number?","21","2","13","17"},
            new[]{"Time complexity of binary search on a sorted array?","O(log n)","O(n)","O(n²)","O(n log n)"},
            new[]{"Overfitting more often shows up as?","Good on training, poor on validation","Poor on training, good on validation","Perfect on both","Always zero on both"},
            new[]{"A computer stack usually follows which order?","Last in, first out","First in, first out","Random removal","Removal by value"},
            new[]{"Probability that two fair dice add up to 7?","1/6","1/12","1/36","1/3"},
            new[]{"Next term of the geometric sequence 1, 2, 4, 8?","16","12","15","32"},
            new[]{"HTTP status code 404 usually means?","Resource not found","Request succeeded","Internal error","Unauthorized"},
            new[]{"Gradient descent usually updates parameters along?","The negative gradient","The positive gradient","A fixed coordinate axis","A random constant direction"}
        };
    }
}
