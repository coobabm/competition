// dotnet-only stand-in for the few UnityEngine APIs the EditMode tests touch (JsonUtility round trips, Debug.Log).
namespace UnityEngine
{
    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions Opt = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object o) { return System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), Opt); }
        public static T FromJson<T>(string json) { return System.Text.Json.JsonSerializer.Deserialize<T>(json, Opt); }
    }
    public static class Debug
    {
        public static void Log(object o) { NUnit.Framework.TestContext.Progress.WriteLine(o == null ? "" : o.ToString()); }
    }
}
