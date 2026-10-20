namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    static class XgPackGames
    {
        public static XgMiniGame Create(string id,XgMiniContext c)
        {
            switch(id)
            {
                case "board_nextmove": return new XgNextMoveQuestions(c);
                case "daily_probability": case "daily_affairs": return new XgDailyQuestions(c);
                case "stage_sentence": return new XgSentenceLesson(c);
                case "stage_translation": return new XgTranslationLesson(c);
                case "stage_compare": return new XgBetterAnswerLesson(c);
                case "spam_swipe": case "headline_swipe": case "review_swipe": return new XgPackSwipe(c);
                case "arith_mole": return new XgActionMoles(c);
                case "minesweeper_yn": return new XgMinePack(c);
                case "make24": return new XgTwentyFourPack(c);
                case "odd_one": return new XgOddPack(c);
                case "sequence": return new XgSequencePack(c);
                case "danmu_mood": return new XgPackDanmu(c);
                case "meme_word": return new XgMemeCaptionPack(c);
                case "stage_photo": case "mnist_write": case "captcha": return new XgPackPhotos(c);
                case "chess_mate1": return new XgMatePack(c,1);
                case "chess_mate2": return new XgMatePack(c,2);
                case "gomoku_win": return new XgGomokuPack(c);
                case "poem_next": return new XgPackPoem(c);
                case "idiom_chain": return new XgIdiomPack(c);
                case "spy": return new XgUndercoverGame(c);
                case "chinglish": return new XgPackEnglish(c);
                default:return null;
            }
        }
    }
}
