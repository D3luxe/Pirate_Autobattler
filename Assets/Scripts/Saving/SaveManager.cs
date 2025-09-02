using UnityEngine;
using System.IO;

namespace PirateRoguelike.Saving
{
    public static class SaveManager
    {
        private static readonly string saveFileName = "run.json";

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        public static void SaveRun(RunState state)
        {
            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(GetSavePath(), json);
            Debug.Log("Run saved to " + GetSavePath());
        }

        public static RunState LoadRun()
        {
            string path = GetSavePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                RunState state = JsonUtility.FromJson<RunState>(json);
                Debug.Log("Run loaded from " + path);
                return state;
            }
            return null;
        }

        public static bool SaveFileExists()
        {
            return File.Exists(GetSavePath());
        }

        public static void DeleteSave()
        {
            if (SaveFileExists())
            {
                File.Delete(GetSavePath());
            }
        }
    }
}
