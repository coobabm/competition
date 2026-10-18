using System.IO;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// Optional chat history file for the chat app (moved from DreamOS 2026-10-19; same GUID, fields and file format):
    /// each sent line is appended as a "MessageID: …{ [Type] [Author] [Message] [Time] }" block and read back on start.
    /// </summary>
    public class MessageStoring : MonoBehaviour
    {
        [Header("Resources")]
        public MessagingApp messagingManager;

        [Header("Settings")]
        public string subPath = "DreamOS_Data";
        public string fileName = "StoredMessages";
        public string fileExtension = ".data";

        string fullPath;

        public void CheckForDataFile()
        {
#if UNITY_EDITOR
            fullPath = Application.dataPath + "/" + subPath + "/" + fileName + fileExtension;
#else
            string appPath = Application.dataPath.Replace(Application.productName + "_Data", "");
            fullPath = appPath + subPath + "/" + "/" + fileName + fileExtension;
#endif
            if (File.Exists(fullPath)) return;
            new FileInfo(fullPath).Directory.Create();
            File.WriteAllText(fullPath, "MSG_DATA");
        }

        public void ReadMessageData()
        {
            if (messagingManager == null)
            {
                Debug.LogError("<b>[Message Storing]</b> 'Messaging Manager' is missing.", this);
                return;
            }

            CheckForDataFile();

            string id = null, type = null, author = null, message = null, time = null;
            bool inMessage = false;
            foreach (string line in File.ReadLines(fullPath))
            {
                if (line.Contains("MessageID: ")) id = line.Replace("MessageID: ", "");
                else if (line.Contains("[Type]")) type = line.Replace("[Type] ", "");
                else if (line.Contains("[Author]")) author = line.Replace("[Author] ", "");
                else if (line.Contains("[Message]")) { message = line.Replace("[Message] ", ""); inMessage = true; }
                else if (line.Contains("[Time]")) time = line.Replace("[Time] ", "");
                else if (line == "}")
                {
                    inMessage = false;
                    if (type == "standard" && (author == "self" || author == "individual"))
                        messagingManager.CreateStoredMessage(id, message, time, author == "self");
                }
                // A message's later lines.
                else if (inMessage) message = message + "\n" + line;
            }
        }

        public void ApplyMessageData(string msgID, string msgType, string author, string message, string msgTime)
        {
            File.AppendAllText(fullPath, "\n\nMessageID: " + msgID +
                "\n{" +
                "\n[Type] " + msgType +
                "\n[Author] " + author +
                "\n[Message] " + message +
                "\n[Time] " + msgTime +
                "\n}");
        }

        public void ResetData() => File.WriteAllText(fullPath, "MSG_DATA");
    }
}
