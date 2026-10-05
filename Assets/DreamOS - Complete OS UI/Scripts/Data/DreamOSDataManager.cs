using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Michsky.DreamOS
{
    public class DreamOSDataManager : MonoBehaviour
    {
        public static string profileID;
        static string subPath = "DreamOS_UserData";
        static string jsonExtension = ".json";
        static string dataPath;

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

        static string GetTempDataPath(DataCategory cat)
        {
            // Force initialize check
            if (string.IsNullOrEmpty(dataPath)) { InitializeProfile(); }

            // Creating the data profile directory
            if (!Directory.Exists(dataPath)) { Directory.CreateDirectory(dataPath); }

            // Checking for JSON file and creating if it doesn't exist
            if (!File.Exists(dataPath + cat.ToString() + jsonExtension)) { File.WriteAllText(dataPath + cat.ToString() + jsonExtension, "{}"); }

            // Making sure that JSON file has brackets if it's wiped
            if (string.IsNullOrEmpty(File.ReadAllText(dataPath + cat.ToString() + jsonExtension))) { File.WriteAllText(dataPath + cat.ToString() + jsonExtension, "{}"); }

            // Populate tempPath
            string tempPath = dataPath + cat.ToString() + jsonExtension;

            // Give tempPath as a result
            return tempPath;
        }

        public static bool ContainsJsonKey(DataCategory cat, string key)
        {
            string tempPath = GetTempDataPath(cat);

            string json = File.ReadAllText(tempPath);
            JObject data = JsonConvert.DeserializeObject<JObject>(json);

            return data.ContainsKey(key);
        }

        public static void WriteStringData(DataCategory cat, string key, string value)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Create a dictionary to hold the variable values
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            data[key] = value;

            // Serialize the dictionary to JSON
            json = JsonConvert.SerializeObject(data);

            // Save the JSON to a file
            File.WriteAllText(tempPath, json);
        }

        public static void WriteIntData(DataCategory cat, string key, int value)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Create a dictionary to hold the variable values
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            data[key] = value;

            // Serialize the dictionary to JSON
            json = JsonConvert.SerializeObject(data);

            // Save the JSON to a file
            File.WriteAllText(tempPath, json);
        }

        public static void WriteFloatData(DataCategory cat, string key, float value)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Create a dictionary to hold the variable values
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            data[key] = value;

            // Serialize the dictionary to JSON
            json = JsonConvert.SerializeObject(data);

            // Save the JSON to a file
            File.WriteAllText(tempPath, json);
        }

        public static void WriteBooleanData(DataCategory cat, string key, bool value)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Create a dictionary to hold the variable values
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            data[key] = value;

            // Serialize the dictionary to JSON
            json = JsonConvert.SerializeObject(data);

            // Save the JSON to a file
            File.WriteAllText(tempPath, json);
        }

        public static string ReadStringData(DataCategory cat, string key)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Deserialize the JSON to a dictionary
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Access the variable values using the keys
            return (string)data[key];
        }

        public static int ReadIntData(DataCategory cat, string key)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Deserialize the JSON to a dictionary
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Access the variable values using the keys
            return (int)(long)data[key];
        }

        public static float ReadFloatData(DataCategory cat, string key)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Deserialize the JSON to a dictionary
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Access the variable values using the keys
            return (float)(double)data[key];
        }

        public static bool ReadBooleanData(DataCategory cat, string key)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Deserialize the JSON to a dictionary
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Access the variable values using the keys
            return (bool)data[key];
        }

        public static void DeleteData(DataCategory cat, string key)
        {
            // Check for path
            string tempPath = GetTempDataPath(cat);

            // Read json first
            string json = File.ReadAllText(tempPath);

            // Delete data from dictionary
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            data.Remove(key);

            // Serialize the dictionary to JSON
            json = JsonConvert.SerializeObject(data);

            // Save the JSON to a file
            File.WriteAllText(tempPath, json);
        }

        public static void DeleteDataCategory(DataCategory cat)
        {
            File.WriteAllText(dataPath + cat.ToString() + jsonExtension, "{}");
        }
    }
}