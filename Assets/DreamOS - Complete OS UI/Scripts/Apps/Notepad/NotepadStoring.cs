using UnityEngine;
using System.IO;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Apps/Notepad/Notepad Storing")]
    public class NotepadStoring : MonoBehaviour
    {
        [Header("Resources")]
        public NotepadManager notepadManager;

        [Header("Settings")]
        public string subPath = "DreamOS_Data";
        public string fileName = "StoredNotes";
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
                File.WriteAllText(fullPath, "NOTE_DATA");
            }
        }

        public void UpdateData()
        {
            if (notepadManager == null)
            {
                Debug.LogError("<b>[Notepad Storing]</b> 'Notepad Manager' is missing.", this);
                return;
            }

            File.WriteAllText(fullPath, "NOTE_DATA");

            for (int i = 0; i < notepadManager.noteItems.Count; ++i)
            {
                if (!notepadManager.noteItems[i].isCustom) { continue; }
                WriteNoteData(i);
            }
        }

        public void WriteNoteData(int tempIndex)
        {
            File.AppendAllText(fullPath, "\n\nNoteID: " + notepadManager.noteItems[tempIndex].noteID +
              "\n{" +
              "\n[Title] " + notepadManager.noteItems[tempIndex].noteTitle +
              "\n[Content] " + notepadManager.noteItems[tempIndex].noteContent +
              "\n}");
        }

        public void ReadNoteData()
        {
            if (notepadManager == null)
            {
                Debug.LogError("<b>[Notepad Storing]</b> 'Notepad Manager' is missing.", this);
                return;
            }

            CheckForDataFile();

            string tempID = null;
            string tempTitle = null;
            string tempContent = null;
            bool contentCatched = false;

            foreach (string option in File.ReadLines(fullPath))
            {
                if (option.Contains("NoteID: ")) { tempID = option.Replace("NoteID: ", ""); }
                else if (option.Contains("[Title] ")) { tempTitle = option.Replace("[Title] ", ""); }
                else if (option.Contains("[Content] ")) { tempContent = option.Replace("[Content] ", ""); contentCatched = true; }
                else if (option == "}") { notepadManager.CreateNote(tempID, tempTitle, tempContent); contentCatched = false; }
                else if (contentCatched) { tempContent = tempContent + "\n" + option; }
            }
        }
    }
}