using System;
using System.Collections.Generic;
namespace LingGuangV05.Core.Games
{
    [Serializable] public sealed class MonopolyState
    {
        public int[] position={0,0},cash={1500,1500},turns={0,0},owners;
        public int turn,moves,winner=-1,pending=-1,die;
        public List<string> log=new List<string>();
    }
    /// <summary>Two players, twenty squares, eight turns each. All money is local play money.</summary>
    public sealed class MonopolyGame
    {
        public static readonly string[] Names={"起点","华强北","机会","中关村","网吧","西湖","机会","鼓浪屿","淘货仓库","外滩","免费停车","三里屯","机会","陆家嘴","电脑城","武康路","机会","国贸","电费单","深南大道"};
        public static readonly string[] NamesEn={"Start","Huaqiangbei","Chance","Zhongguancun","Internet café","West Lake","Chance","Gulangyu","Bargain warehouse","The Bund","Free parking","Sanlitun","Chance","Lujiazui","Computer mall","Wukang Road","Chance","Guomao","Electric bill","Shennan Avenue"};
        public static readonly string[] Chance={"双十一剁手，付 ¥200","抢到 iPhone 7，转手赚 ¥300","网吧通宵，付 ¥50","公司发年中奖 ¥250","显卡烧了，付 ¥300","群里抢到大红包 ¥88","房东涨房租，付 ¥150","看女排夺冠太激动，前进 3 格"};
        public static readonly string[] ChanceEn={"Singles' Day splurge: pay ¥200","Scored an iPhone 7, resold for ¥300","All-nighter at the café: pay ¥50","Mid-year bonus: ¥250","GPU burned out: pay ¥300","Grabbed a big red envelope: ¥88","Landlord raised the rent: pay ¥150","Too excited by the volleyball final: move 3"};
        static readonly int[] ChanceDelta={-200,300,-50,250,-300,88,-150,0};
        public readonly MonopolyState S;
        public bool Over=>S.winner>=0;
        public int LastChance {get;private set;}=-1;
        public MonopolyGame(MonopolyState state=null)
        {
            S=state??new MonopolyState();
            if(S.owners==null){S.owners=new int[20];for(int i=0;i<20;i++)S.owners[i]=-1;}
            if(S.position==null||S.position.Length!=2||S.cash==null||S.cash.Length!=2||S.turns==null||S.turns.Length!=2||S.owners.Length!=20||S.turn<0||S.turn>1||S.pending < -1||S.pending>19||S.winner < -1||S.winner>2)throw new ArgumentException("Invalid property-game state");
            for(int i=0;i<2;i++)if(S.position[i]<0||S.position[i]>=20||S.turns[i]<0||S.turns[i]>8)throw new ArgumentException("Invalid player state");
            for(int i=0;i<20;i++)if(S.owners[i] < -1||S.owners[i]>1||!Property(i)&&S.owners[i]!=-1)throw new ArgumentException("Invalid ownership");
            if(S.pending>=0&&(!Property(S.pending)||S.position[S.turn]!=S.pending||S.owners[S.pending]!=-1))throw new ArgumentException("Invalid purchase");
            if(S.log==null)S.log=new List<string>();
        }
        public static bool Property(int square)=>square>0&&square<20&&square!=2&&square!=6&&square!=10&&square!=12&&square!=16&&square!=18;
        // Authored map omitted prices: use a visible linear price schedule and rent at 20% of price.
        public static int Price(int square)=>Property(square)?200+20*square:0;
        public static int Rent(int square)=>Price(square)/5;
        public int PropertyValue(int player){int n=0;for(int i=0;i<20;i++)if(S.owners[i]==player)n+=Price(i);return n;}
        public int Worth(int player)=>S.cash[player]+PropertyValue(player);
        public static List<int> EligibleChance(int today)
        {
            var cards=new List<int>();for(int i=0;i<8;i++)
            {if(i==0&&today<20161111||i==1&&today<20160916||i==7&&today<20160821)continue;cards.Add(i);}return cards;
        }
        public bool Roll(int die,int today,int chanceChoice)
        {
            if(Over||S.pending>=0||die<1||die>6)return false;
            var cards=EligibleChance(today);if(chanceChoice<0||chanceChoice>=cards.Count)return false;
            LastChance=-1;S.die=die;S.log.Add("roll:"+S.turn+":"+die);Advance(die);Resolve(today,cards[chanceChoice]);
            if(S.pending<0&&!Over)EndTurn();return true;
        }
        void Advance(int amount)
        {
            int player=S.turn,total=S.position[player]+amount;
            if(total>=20){S.cash[player]+=200;S.log.Add("start:"+player+":200");}
            S.position[player]=total%20;S.log.Add("land:"+player+":"+S.position[player]);
        }
        void Resolve(int today,int card)
        {
            int at=S.position[S.turn];
            if(at==2||at==6||at==12||at==16)
            {
                LastChance=card;S.log.Add("chance:"+S.turn+":"+card+":date:"+today);
                if(card==7){Advance(3);Resolve(today,card);}
                else {S.cash[S.turn]+=ChanceDelta[card];Bankruptcy();}
            }
            else if(at==18){S.cash[S.turn]-=100;S.log.Add("electric:"+S.turn+":100");Bankruptcy();}
            else if(Property(at))
            {
                int owner=S.owners[at];
                if(owner<0)S.pending=at;
                else if(owner!=S.turn){int fee=Rent(at);S.cash[S.turn]-=fee;S.cash[owner]+=fee;S.log.Add("rent:"+S.turn+":"+owner+":"+at+":"+fee);Bankruptcy();}
            }
        }
        void Bankruptcy(){if(S.cash[S.turn]<0){S.winner=1-S.turn;S.pending=-1;S.turns[S.turn]++;S.moves++;S.log.Add("bankrupt:"+S.turn);}}
        public bool ChoosePurchase(bool buy)
        {
            if(Over||S.pending<0)return false;int at=S.pending,price=Price(at);
            if(buy&&S.cash[S.turn]<price)return false;
            if(buy){S.cash[S.turn]-=price;S.owners[at]=S.turn;}
            S.log.Add((buy?"buy:":"skip:")+S.turn+":"+at+":"+(buy?price:0));S.pending=-1;EndTurn();return true;
        }
        void EndTurn()
        {
            S.turns[S.turn]++;S.moves++;
            if(S.turns[0]>=8&&S.turns[1]>=8)S.winner=Worth(0)>Worth(1)?0:Worth(1)>Worth(0)?1:2;
            S.turn=1-S.turn;
        }
        public bool ShouldBuy(int level,int learnedStyle=1)
        {
            if(Over||S.pending<0||S.cash[S.turn]<Price(S.pending))return false;
            if(level<3)return true;
            int player=S.turn,opponent=1-player;double risk=0,potential=0;
            // Exact die distributions for the next three rolls on this actual ownership map.
            var ours=new double[20];var theirs=new double[20];ours[S.position[player]]=1;theirs[S.position[opponent]]=1;
            for(int horizon=0;horizon<3;horizon++)
            {
                ours=Next(ours);theirs=Next(theirs);
                for(int at=0;at<20;at++){if(S.owners[at]==opponent)risk+=ours[at]*Rent(at);if(at==18)risk+=ours[at]*100;}
                potential+=theirs[S.pending]*Rent(S.pending);
            }
            int remaining=S.cash[player]-Price(S.pending);
            int reserve=level>=4?(learnedStyle==0?75:learnedStyle==2?250:150):150;
            return remaining>=Math.Max(reserve,risk+reserve)&& (potential>=8||remaining>=500);
        }
        static double[] Next(double[] positions)
        {var next=new double[20];for(int i=0;i<20;i++)for(int d=1;d<=6;d++)next[(i+d)%20]+=positions[i]/6;return next;}
    }
}
