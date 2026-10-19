using TMPro;
using System.Text;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    // Explicit opt-in for the .exe view. Do not use as a global desktop/TMP replacement.
    public static class XgUiV4Fonts
    {
        static TMP_FontAsset sans,sansMedium,sansSemibold,reading,numbers;
        const string Root="LingGuangV05/Fonts/UiV4/";
        public static TMP_FontAsset Sans=>sans!=null?sans:(sans=Resources.Load<TMP_FontAsset>(Root+"UiSans"));
        // Real MiSans weights. UiSans' weight table already maps Bold/<b> to Semibold;
        // use these only where a whole label should be set in that face (titles, boot logo).
        public static TMP_FontAsset SansMedium=>sansMedium!=null?sansMedium:(sansMedium=Resources.Load<TMP_FontAsset>(Root+"UiSansMedium"));
        public static TMP_FontAsset SansSemibold=>sansSemibold!=null?sansSemibold:(sansSemibold=Resources.Load<TMP_FontAsset>(Root+"UiSansSemibold"));
        public static TMP_FontAsset Reading=>reading!=null?reading:(reading=Resources.Load<TMP_FontAsset>(Root+"ReadingSerif"));
        public static TMP_FontAsset Numbers=>numbers!=null?numbers:(numbers=Resources.Load<TMP_FontAsset>(Root+"Numeric"));
        // Numeric labels may contain units. Space only number runs; leave units and rich text intact.
        public static string FixedDigits(string value)
        {
            if(string.IsNullOrEmpty(value))return value;
            if(value.IndexOf('<')>=0||value.IndexOf('>')>=0)return value;
            var text=new StringBuilder(value.Length+40);bool open=false;
            foreach(char c in value)
            {
                bool numeric=c>='0'&&c<='9'||".,:+-−/".IndexOf(c)>=0;
                if(numeric&&!open){text.Append("<mspace=0.65em>");open=true;}
                if(!numeric&&open){text.Append("</mspace>");open=false;}
                text.Append(c);
            }
            if(open)text.Append("</mspace>");return text.ToString();
        }
    }
}
