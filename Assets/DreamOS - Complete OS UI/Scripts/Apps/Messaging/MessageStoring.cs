using UnityEngine;
using System.IO;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Apps/Messaging/Message Storing")]
    public class MessageStoring : MonoBehaviour
    {
        [Header("Resources")]
        public MessagingManager messagingManager;

        [Header("Settings")]
        public string subPath = "DreamOS_Data";
        public string fileName = "StoredMessages";
        public string fileExtension = ".data";
       
        // Helpers
        string fullPath;

        public void CheckForDataFile()
        {
#if UNITY_EDITOR
            fullPath = Application.dataPath + "/" + subPath + "/" + fileName + fileExtension;
#else
            string appPath = Application.dataPath;
            appPath = appPath.Replace(Application.productName + "_Data", "");
            fullPath = appPath + subPath + "/" + "/" + fileName + fileExtension;
#endif

            if (!File.Exists(fullPath))
            {
                FileInfo dataFile = new FileInfo(fullPath);
                dataFile.Directory.Create();
                File.WriteAllText(fullPath, "MSG_DATA");
            }
        }

        public void ReadMessageData()
        {
            if (messagingManager == null)
            {
                Debug.LogError("<b>[Message Storing]</b> 'Messaging Manager' is missing.", this);
                return;
            }

            CheckForDataFile();

            string tempID = null;
            string tempType = null;
            string tempAuthor = null;
            string tempMsg = null;
            string tempTime = null;
            bool contentCatched = false;

            foreach (string option in File.ReadLines(fullPath))
            {
                if (option.Contains("MessageID: ")) { tempID = option.Replace("MessageID: ", ""); }
                else if (option.Contains("[Type]")) { tempType = option.Replace("[Type] ", ""); }
                else if (option.Contains("[Author]")) { tempAuthor = option.Replace("[Author] ", ""); }
                else if (option.Contains("[Message]")) { tempMsg = option.Replace("[Message] ", ""); contentCatched = true; }
                else if (option.Contains("[Time]")) { tempTime = option.Replace("[Time] ", ""); }
                else if (option == "}") 
                {
                    contentCatched = false;

                    if (tempAuthor == "self" && tempType == "standard") { messagingManager.CreateStoredMessage(tempID, tempMsg, tempTime, true); }
                    else if (tempAuthor == "individual" && tempType == "standard") { messagingManager.CreateStoredMessage(tempID, tempMsg, tempTime, false); }
                }
                else if (contentCatched) { tempMsg = tempMsg + "\n" + option; }
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

        public void ResetData()
        {
            File.WriteAllText(fullPath, "MSG_DATA");
        }
    }
}