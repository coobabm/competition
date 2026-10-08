namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The 常识判断 desk: short plain statements with a definite 是 / 否 answer, the first thing a blank 灵光 can be taught
    /// (it is open from the first minute, next to 逻辑题 and 垃圾短信). No tricks and no opinions: every line is something
    /// a 2016 player knows without looking it up, and nothing in the bank is dated after 2016. Level 1 is the nursery, level 2
    /// school facts, level 3 a little more to know; the level rises with the desk's own label count like every text desk.
    /// The bank is balanced (as many 是 as 否) and every item carries an English wording.
    /// </summary>
    public static partial class XgMemes
    {
        /// <summary>The desk and dataset id of the common-sense yes/no labelling desk.</summary>
        public const string SenseDesk = "sense";

        static XgPhrase S(int level, bool yes, string zh, string en, string why, string whyEn)
        { return new XgPhrase { text = zh, textEn = en, yes = yes, why = why, whyEn = whyEn, source = "常识", level = level }; }

        /// <summary>The whole bank (read-only; tests check it is balanced and fully paired).</summary>
        public static XgPhrase[] SenseBank { get { return Sense; } }

        static readonly XgPhrase[] Sense =
        {
            // ───────────── 级别 1 · 一看就知道 · 是 ─────────────
            S(1, true, "太阳从东边升起。", "The sun rises in the east.", "太阳每天从东边升起，从西边落下。", "The sun rises in the east and sets in the west."),
            S(1, true, "水在 0°C 会结冰。", "Water freezes at 0°C.", "0°C 是水结冰的温度。", "0°C is where water freezes."),
            S(1, true, "一年有十二个月。", "A year has twelve months.", "一月到十二月，一共十二个。", "January to December: twelve."),
            S(1, true, "猫会喵喵叫。", "Cats meow.", "猫叫「喵」，狗才叫「汪」。", "Cats say meow; dogs say woof."),
            S(1, true, "鱼生活在水里。", "Fish live in water.", "鱼离了水活不了多久。", "A fish does not last long out of water."),
            S(1, true, "冰是凉的。", "Ice is cold.", "冰摸上去冰凉。", "Ice feels cold."),
            S(1, true, "一个星期有七天。", "A week has seven days.", "周一到周日，一共七天。", "Monday to Sunday: seven days."),
            S(1, true, "鸟有翅膀。", "Birds have wings.", "翅膀是鸟的特征。", "Wings are what a bird has."),
            S(1, true, "夏天比冬天热。", "Summer is hotter than winter.", "在中国，夏天热、冬天冷。", "In China, summer is hot and winter is cold."),
            S(1, true, "苹果是一种水果。", "An apple is a fruit.", "苹果、香蕉、西瓜都是水果。", "Apples, bananas and watermelons are all fruit."),
            S(1, true, "人用眼睛看东西。", "People see with their eyes.", "看靠眼睛，听靠耳朵。", "We see with eyes and hear with ears."),
            S(1, true, "火是烫的。", "Fire is hot.", "碰到火会被烫伤。", "Touch fire and you get burned."),
            S(1, true, "狗是一种动物。", "A dog is an animal.", "狗、猫、鸟都是动物。", "Dogs, cats and birds are all animals."),
            S(1, true, "汽车有轮子。", "Cars have wheels.", "汽车靠轮子在路上跑。", "A car rolls along the road on wheels."),
            S(1, true, "北京是中国的首都。", "Beijing is the capital of China.", "首都是北京。", "The capital is Beijing."),
            S(1, true, "雨天，路面会变湿。", "The road gets wet when it rains.", "下了雨，地面就是湿的。", "Rain makes the ground wet."),
            S(1, true, "牛会产奶。", "Cows give milk.", "牛奶就是奶牛产的。", "Milk comes from dairy cows."),
            S(1, true, "手机可以用来打电话。", "A phone can be used to make calls.", "打电话是手机最基本的用处。", "Calling is the most basic thing a phone does."),

            // ───────────── 级别 1 · 否 ─────────────
            S(1, false, "鱼会爬树。", "Fish climb trees.", "鱼生活在水里，不会爬树。", "Fish live in water; they do not climb trees."),
            S(1, false, "一年有 13 个月。", "A year has 13 months.", "一年只有十二个月。", "A year has only twelve months."),
            S(1, false, "冰是烫的。", "Ice is hot.", "冰是凉的。", "Ice is cold."),
            S(1, false, "狗会下蛋。", "Dogs lay eggs.", "狗是生小狗的，下蛋的是鸡。", "Dogs have puppies; hens lay eggs."),
            S(1, false, "一个星期有十天。", "A week has ten days.", "一个星期只有七天。", "A week has only seven days."),
            S(1, false, "鸟没有翅膀。", "Birds have no wings.", "鸟都有翅膀。", "Every bird has wings."),
            S(1, false, "冬天比夏天热。", "Winter is hotter than summer.", "在中国，冬天冷、夏天热。", "In China, winter is cold and summer is hot."),
            S(1, false, "西瓜是一种动物。", "A watermelon is an animal.", "西瓜是水果。", "A watermelon is a fruit."),
            S(1, false, "人用耳朵看东西。", "People see with their ears.", "看东西靠眼睛。", "We see with our eyes."),
            S(1, false, "猫会汪汪叫。", "Cats bark.", "猫喵喵叫，汪汪叫的是狗。", "Cats meow; dogs are the ones that bark."),
            S(1, false, "水会自己往高处流。", "Water flows uphill on its own.", "水往低处流。", "Water flows downhill."),
            S(1, false, "上海是中国的首都。", "Shanghai is the capital of China.", "首都是北京。", "The capital is Beijing."),
            S(1, false, "兔子有三只耳朵。", "Rabbits have three ears.", "兔子有两只耳朵。", "Rabbits have two ears."),
            S(1, false, "雪是热的。", "Snow is hot.", "雪是冷的。", "Snow is cold."),
            S(1, false, "一天有四十八个小时。", "A day has forty-eight hours.", "一天二十四小时。", "A day has twenty-four hours."),
            S(1, false, "自行车有六个轮子。", "A bicycle has six wheels.", "自行车有两个轮子。", "A bicycle has two wheels."),
            S(1, false, "太阳每天从西边升起。", "The sun rises in the west every day.", "太阳从东边升起。", "The sun rises in the east."),
            S(1, false, "手机没电了也能一直用。", "A phone with a flat battery keeps working.", "没电的手机开不了机。", "A phone with no charge will not even switch on."),

            // ───────────── 级别 2 · 学校里学过的 · 是 ─────────────
            S(2, true, "地球绕着太阳转。", "The Earth goes around the sun.", "地球绕太阳转一圈就是一年。", "One trip of the Earth around the sun is a year."),
            S(2, true, "水烧开了会冒热气。", "Boiling water gives off steam.", "水烧开了变成水蒸气。", "Boiling water turns to steam."),
            S(2, true, "蜘蛛有八条腿。", "Spiders have eight legs.", "蜘蛛有八条腿。", "A spider has eight legs."),
            S(2, true, "植物需要阳光才能长得好。", "Plants need sunlight to grow well.", "没有光，植物会枯黄。", "Without light a plant turns yellow and dies."),
            S(2, true, "一小时有六十分钟。", "An hour has sixty minutes.", "一小时等于六十分钟。", "An hour is sixty minutes."),
            S(2, true, "铁放在潮湿的地方会生锈。", "Iron rusts in a damp place.", "铁遇水和空气会生锈。", "Iron rusts with water and air."),
            S(2, true, "长江是中国最长的河。", "The Yangtze is the longest river in China.", "长江全长六千多公里。", "The Yangtze runs over six thousand kilometres."),
            S(2, true, "冰块放进热水里会化掉。", "An ice cube melts in hot water.", "遇热，冰就化成水。", "Heat turns ice back into water."),
            S(2, true, "一米等于一百厘米。", "One metre is a hundred centimetres.", "1 米 = 100 厘米。", "1 m = 100 cm."),
            S(2, true, "磁铁能吸住铁钉。", "A magnet picks up an iron nail.", "磁铁吸铁。", "Magnets attract iron."),
            S(2, true, "春节在冬天。", "Chinese New Year falls in winter.", "春节在一月底或二月。", "It falls in late January or February."),
            S(2, true, "蜜蜂会采花蜜。", "Bees gather nectar from flowers.", "蜜蜂靠花蜜酿蜜。", "Bees make honey from nectar."),
            S(2, true, "月亮绕着地球转。", "The moon goes around the Earth.", "月亮是地球的卫星。", "The moon is the Earth's satellite."),
            S(2, true, "红色和黄色颜料混在一起会变成橙色。", "Red and yellow paint mixed together make orange.", "红加黄是橙。", "Red plus yellow gives orange."),
            S(2, true, "声音可以在空气中传播。", "Sound can travel through air.", "我们靠空气听到声音。", "We hear sounds through the air."),
            S(2, true, "高铁比自行车快得多。", "A high-speed train is much faster than a bicycle.", "高铁每小时三百公里左右。", "A high-speed train does about three hundred km an hour."),
            S(2, true, "手机要充电才有电。", "A phone needs charging to have power.", "没电就得插上充电器。", "When it is flat you plug it in."),
            S(2, true, "微信是一种聊天软件。", "WeChat is a chat app.", "微信用来发消息、打电话。", "WeChat is for messages and calls."),

            // ───────────── 级别 2 · 否 ─────────────
            S(2, false, "太阳绕着地球转。", "The sun goes around the Earth.", "是地球绕着太阳转。", "It is the Earth that goes around the sun."),
            S(2, false, "一小时有一百分钟。", "An hour has a hundred minutes.", "一小时是六十分钟。", "An hour is sixty minutes."),
            S(2, false, "铁永远不会生锈。", "Iron never rusts.", "铁放在潮湿的地方会生锈。", "Iron rusts in a damp place."),
            S(2, false, "长城在日本。", "The Great Wall is in Japan.", "长城在中国。", "The Great Wall is in China."),
            S(2, false, "一米等于十厘米。", "One metre is ten centimetres.", "1 米是 100 厘米。", "1 m is 100 cm."),
            S(2, false, "磁铁能吸住木头。", "A magnet picks up wood.", "磁铁只吸铁一类的东西。", "A magnet only picks up iron and the like."),
            S(2, false, "植物不要水也能长得很好。", "Plants grow fine with no water at all.", "植物缺水会枯萎。", "A plant without water withers."),
            S(2, false, "冰块放进热水里会越变越大。", "An ice cube in hot water grows bigger and bigger.", "冰块遇热会化小。", "Heat makes ice shrink and melt."),
            S(2, false, "春节在夏天。", "Chinese New Year falls in summer.", "春节在冬天。", "It falls in winter."),
            S(2, false, "老虎只吃草。", "Tigers eat only grass.", "老虎吃肉。", "Tigers eat meat."),
            S(2, false, "兔子主要吃肉。", "Rabbits mostly eat meat.", "兔子吃草和蔬菜。", "Rabbits eat grass and vegetables."),
            S(2, false, "蜜蜂生活在海底。", "Bees live at the bottom of the sea.", "蜜蜂在花丛和蜂巢里。", "Bees live among flowers and in hives."),
            S(2, false, "地球是一个正方体。", "The Earth is a cube.", "地球近似一个球。", "The Earth is roughly a ball."),
            S(2, false, "水是由铁组成的。", "Water is made of iron.", "水是氢和氧组成的。", "Water is made of hydrogen and oxygen."),
            S(2, false, "北京在海南岛上。", "Beijing is on Hainan Island.", "北京在北方，海南岛在南方。", "Beijing is in the north; Hainan Island is far south."),
            S(2, false, "三角形有四条边。", "A triangle has four sides.", "三角形有三条边。", "A triangle has three sides."),
            S(2, false, "月亮比太阳大得多。", "The moon is much bigger than the sun.", "太阳比月亮大得多，只是离得远。", "The sun is far bigger; it is just far away."),
            S(2, false, "冰淇淋放在太阳底下会越来越硬。", "Ice cream left in the sun gets harder and harder.", "冰淇淋晒了会化。", "Ice cream melts in the sun."),

            // ───────────── 级别 3 · 再多知道一点 · 是 ─────────────
            S(3, true, "光比声音传得快。", "Light travels faster than sound.", "先看到闪电，后听到雷声。", "You see the lightning before you hear the thunder."),
            S(3, true, "地球表面大部分被海洋覆盖。", "Most of the Earth's surface is covered by ocean.", "海洋约占地球表面的七成。", "Oceans cover about seven tenths of it."),
            S(3, true, "黄金不容易生锈。", "Gold hardly ever rusts.", "金子放多久都亮闪闪的。", "Gold stays bright however long it sits."),
            S(3, true, "企鹅是鸟，但是不会飞。", "Penguins are birds, but they cannot fly.", "企鹅的翅膀用来游泳。", "A penguin's wings are for swimming."),
            S(3, true, "月亮自己不发光，我们看到的是它反射的太阳光。", "The moon makes no light of its own; we see sunlight bouncing off it.", "月光其实是太阳光。", "Moonlight is really sunlight."),
            S(3, true, "水是由氢和氧组成的。", "Water is made of hydrogen and oxygen.", "水的化学式是 H₂O。", "Its chemical formula is H₂O."),
            S(3, true, "北半球的夏天在六月到八月。", "Summer in the northern hemisphere is June to August.", "中国的夏天是六月到八月。", "China's summer runs June to August."),
            S(3, true, "珠穆朗玛峰是世界最高的山峰。", "Mount Everest is the highest mountain in the world.", "它高八千八百多米。", "It stands over 8,800 metres."),
            S(3, true, "大熊猫主要吃竹子。", "Giant pandas mostly eat bamboo.", "竹子是大熊猫的主食。", "Bamboo is the panda's main food."),
            S(3, true, "闰年的二月有二十九天。", "February has twenty-nine days in a leap year.", "今年 2016 就是闰年。", "This year, 2016, is a leap year."),
            S(3, true, "沙漠里很少下雨。", "It rarely rains in a desert.", "少雨是沙漠的特点。", "Little rain is what makes a desert."),
            S(3, true, "海水是咸的。", "Seawater is salty.", "海水里溶着很多盐。", "Seawater has a lot of salt in it."),
            S(3, true, "电脑运行需要用电。", "A computer needs electricity to run.", "断了电，电脑就停了。", "Cut the power and the computer stops."),
            S(3, true, "圆没有角。", "A circle has no corners.", "圆是一条封闭的曲线。", "A circle is a closed curve."),
            S(3, true, "鸡蛋掉在水泥地上容易碎。", "An egg dropped on a concrete floor will probably break.", "蛋壳很脆。", "Eggshell is brittle."),
            S(3, true, "蝴蝶小时候是毛毛虫。", "A butterfly starts out as a caterpillar.", "毛毛虫变成蛹，再变成蝴蝶。", "A caterpillar becomes a chrysalis, then a butterfly."),

            // ───────────── 级别 3 · 否 ─────────────
            S(3, false, "声音比光传得快。", "Sound travels faster than light.", "光快得多，所以先闪电后雷声。", "Light is far faster: lightning first, thunder after."),
            S(3, false, "地球表面大部分是沙漠。", "Most of the Earth's surface is desert.", "大部分是海洋。", "Most of it is ocean."),
            S(3, false, "金子放久了会长满铁锈。", "Gold left out for years gets covered in rust.", "金子不容易生锈。", "Gold hardly ever rusts."),
            S(3, false, "企鹅能飞到很高的地方。", "Penguins can fly very high.", "企鹅不会飞。", "Penguins cannot fly."),
            S(3, false, "月亮是一个自己发光的火球。", "The moon is a ball of fire that makes its own light.", "月亮反射的是太阳光。", "The moon only reflects sunlight."),
            S(3, false, "北半球的夏天在十二月到二月。", "Summer in the northern hemisphere is December to February.", "那是冬天。", "That is winter."),
            S(3, false, "珠穆朗玛峰是世界上最矮的山。", "Mount Everest is the lowest mountain in the world.", "它是最高的山峰。", "It is the highest peak."),
            S(3, false, "大熊猫主要吃肉。", "Giant pandas mostly eat meat.", "大熊猫主要吃竹子。", "Pandas mostly eat bamboo."),
            S(3, false, "平年的二月有三十天。", "February has thirty days in an ordinary year.", "二月最多二十九天。", "February has twenty-nine days at most."),
            S(3, false, "沙漠里天天下大雨。", "It pours down every day in a desert.", "沙漠很干，很少下雨。", "A desert is dry and rarely sees rain."),
            S(3, false, "海水是甜的。", "Seawater is sweet.", "海水是咸的。", "Seawater is salty."),
            S(3, false, "台式电脑拔掉电源还能一直运行。", "A desktop computer keeps running forever with the plug pulled.", "没电，台式电脑就停了。", "No power and a desktop computer stops."),
            S(3, false, "圆有四个直角。", "A circle has four right angles.", "圆没有角。", "A circle has no corners."),
            S(3, false, "鸡蛋掉在地上会弹起来。", "An egg dropped on the floor bounces back up.", "鸡蛋摔在地上会碎。", "An egg dropped on the floor breaks."),
            S(3, false, "毛毛虫长大后变成青蛙。", "A caterpillar grows up into a frog.", "毛毛虫长大变成蝴蝶或蛾子。", "A caterpillar grows into a butterfly or a moth."),
            S(3, false, "网吧里没有电脑。", "An internet cafe has no computers.", "网吧里满是电脑。", "An internet cafe is full of computers."),
        };
    }
}
