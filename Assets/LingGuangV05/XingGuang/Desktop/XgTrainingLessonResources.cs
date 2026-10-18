using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Authored questions are loaded before the catalogue builds its atlas entries.</summary>
    public static class XgTrainingLessonResources
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            var asset=Resources.Load<TextAsset>("LingGuangV05/Training/stage_quiz");
            if(asset==null){Debug.LogWarning("Training quizzes resource missing; using bundled questions.");return;}
            try
            {
                var file=JsonUtility.FromJson<XgStageQuizFile>(asset.text);
                if(file==null||file.quizzes==null||file.quizzes.Length==0)throw new FormatException("Empty quiz file");
                var ids=new HashSet<string>();
                foreach(var q in file.quizzes)
                    if(q==null||string.IsNullOrEmpty(q.id)||!ids.Add(q.id)||q.options==null||q.optionsEn==null||q.options.Length!=q.optionsEn.Length||q.options.Length<2||q.options.Length>4||q.answer<0||q.answer>=q.options.Length||q.stage<1||q.stage>6||string.IsNullOrEmpty(q.q)||string.IsNullOrEmpty(q.qEn))
                        throw new FormatException("Invalid or duplicate bilingual question");
                XgTrainingLessons.Quizzes=file.quizzes;
            }
            catch(Exception ex){Debug.LogWarning("Training quizzes: "+ex.Message+"; using bundled questions.");}
        }
    }
}
