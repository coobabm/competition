using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's save system: one small JSON file per <see cref="DataCategory"/> holding flat key/value pairs
    /// (settings, app state, favorites, downloads, ...). Files live in "DreamOS_UserData/&lt;profileID&gt;/&lt;Category&gt;.json"
    /// next to the project (editor) or the built game, and are created on first use. Replaces DreamOS's
    /// DreamOSDataManager (2026-10-19): same GUID, file names, JSON layout, category order and public API, so existing
    /// player saves load unchanged.
    ///
    /// <c>dataPath</c> is the profile folder; QA code and tests set it by reflection (a full path ending in a
    /// separator) to redirect the saves, so its name, type and "empty means not initialized yet" meaning must stay.
    /// </summary>
    public class DreamOSDataManager : MonoBehaviour
    {
        public static string profileID;
        static string subPath = "DreamOS_UserData";
        static string jsonExtension = ".json";
        static string dataPath;

        // Serialized as ints elsewhere: keep the order.
        public enum DataCategory { User, System, Apps, Widgets, Network, DateAndTime }

        static void InitializeProfile()
        {
#if UNITY_EDITOR
            string appPath = Application.dataPath;
            appPath = appPath.Replace(GetProjectName() + "/Assets", "");
            dataPath = appPath + GetProjectName() + "/" + subPath + "/" + profileID + "/";
#else
            string appPath = Application.dataPath;
            appPath = appPath.Replace(Application.productName + "_Data", "");
            dataPath = appPath + subPath + "/" + profileID + "/";
#endif
        }

        static string GetProjectName()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            return projectName;
        }

        // Path of the category's JSON file, creating the profile folder and an empty "{}" file if needed.
        static string GetTempDataPath(DataCategory cat)
        {
            if (string.IsNullOrEmpty(dataPath)) { InitializeProfile(); }

            if (!Directory.Exists(dataPath)) { Directory.CreateDirectory(dataPath); }

            string filePath = dataPath + cat.ToString() + jsonExtension;
            if (!File.Exists(filePath)) { File.WriteAllText(filePath, "{}"); }

            // A wiped file gets its braces back.
            if (string.IsNullOrEmpty(File.ReadAllText(filePath))) { File.WriteAllText(filePath, "{}"); }

            return filePath;
        }

        static Dictionary<string, object> ReadAll(string filePath)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
        }

        // Read-modify-write of a whole category file.
        static void Write(DataCategory cat, string key, object value)
        {
            string filePath = GetTempDataPath(cat);

            Dictionary<string, object> data = ReadAll(filePath);
            data[key] = value;

            File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
        }

        static object Read(DataCategory cat, string key)
        {
            return ReadAll(GetTempDataPath(cat))[key];
        }

        public static bool ContainsJsonKey(DataCategory cat, string key)
        {
            string json = File.ReadAllText(GetTempDataPath(cat));
            JObject data = JsonConvert.DeserializeObject<JObject>(json);

            return data.ContainsKey(key);
        }

        public static void WriteStringData(DataCategory cat, string key, string value)
        {
            Write(cat, key, value);
        }

        public static void WriteIntData(DataCategory cat, string key, int value)
        {
            Write(cat, key, value);
        }

        public static void WriteFloatData(DataCategory cat, string key, float value)
        {
            Write(cat, key, value);
        }

        public static void WriteBooleanData(DataCategory cat, string key, bool value)
        {
            Write(cat, key, value);
        }

        public static string ReadStringData(DataCategory cat, string key)
        {
            return (string)Read(cat, key);
        }

        // Json.NET reads whole numbers back as long and decimals as double; the casts rely on that.
        public static int ReadIntData(DataCategory cat, string key)
        {
            return (int)(long)Read(cat, key);
        }

        public static float ReadFloatData(DataCategory cat, string key)
        {
            return (float)(double)Read(cat, key);
        }

        public static bool ReadBooleanData(DataCategory cat, string key)
        {
            return (bool)Read(cat, key);
        }

        public static void DeleteData(DataCategory cat, string key)
        {
            string filePath = GetTempDataPath(cat);

            Dictionary<string, object> data = ReadAll(filePath);
            data.Remove(key);

            File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
        }

        // Uses dataPath as it stands, without initializing it, exactly as before.
        public static void DeleteDataCategory(DataCategory cat)
        {
            File.WriteAllText(dataPath + cat.ToString() + jsonExtension, "{}");
        }
    }
}
