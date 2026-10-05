using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
static class Program
{
    static void Main(string[] args)
    {
        int per = args.Length > 0 ? int.Parse(args[0]) : 1;
        var seen = new HashSet<string>();
        for (int level = 1; level <= 5; level++)
            for (int seed = 0; seed < 400; seed++)
            {
                var q = XgLogic.Generate(seed * 31 + level * 1000, level);
                string key = level + q.category;
                if (!seen.Add(key)) continue;
                Console.WriteLine($"L{level} [{q.category}] {q.text}  → {(q.truth ? "是" : "否")}  （{q.why}）");
            }
    }
}
