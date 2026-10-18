#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public static class IntegrationCoreAcceptance
{
    public static void Run()
    {
        var failures=new List<string>();
        RunCheck("SkillTreeCopyChecks",SkillTreeCopyChecks.Run,failures);
        RunCheck("PlayabilityChecks",PlayabilityChecks.Run,failures);
        Directory.CreateDirectory("LocalVerification/IntegrationCore");
        File.WriteAllText("LocalVerification/IntegrationCore/latest.txt",DateTime.UtcNow.ToString("o")+"\n"+(failures.Count==0?"PASS: SkillTreeCopyChecks, PlayabilityChecks":string.Join("\n\n",failures)));
        if(failures.Count>0)throw new Exception(string.Join("\n",failures));
        Debug.Log("INTEGRATION_CORE_ACCEPTANCE_PASS");
    }
    static void RunCheck(string name,Action action,List<string> failures)
    {try{action();}catch(Exception e){failures.Add(name+": "+e);}}
}
#endif
