using System;
using System.IO;
using UnityEngine;

namespace Game.Scripts.Data.UserData.Shared
{
    [Serializable]
    public abstract class UserData<TData> where TData : UserData<TData>
    {
        protected abstract string FileName { get; }

        private const string EditorSaveDirectoryPath = "Assets/EditorData/UserData";

        private string SaveDirectoryPath
        {
            get
            {
#if UNITY_EDITOR
                return EditorSaveDirectoryPath;
#else
                return Application.persistentDataPath;
#endif
            }
        }

        private string SaveFilePath => Path.Combine(SaveDirectoryPath, FileName);

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(SaveDirectoryPath);

                var json = JsonUtility.ToJson(this);
                File.WriteAllText(SaveFilePath, json);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save {typeof(TData).Name} to '{SaveFilePath}'.\n{exception}");

                return false;
            }
        }

        public virtual bool TryRead(out TData data)
        {
            data = default;

            if (!File.Exists(SaveFilePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(SaveFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<TData>(json);

                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to read {typeof(TData).Name} from '{SaveFilePath}'.\n{exception}");
                
                return false;
            }
        }

        public bool Reset()
        {
            if (!File.Exists(SaveFilePath))
            {
                return true;
            }

            try
            {
                File.Delete(SaveFilePath);
                
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to delete {typeof(TData).Name} at '{SaveFilePath}'.\n{exception}");
                
                return false;
            }
        }
    }
}