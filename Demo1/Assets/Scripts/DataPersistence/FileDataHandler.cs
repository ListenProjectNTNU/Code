using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";

    public FileDataHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
        
        // 除錯：印出路徑資訊
        Debug.Log($"[FileDataHandler] 建構子 - 資料目錄: {dataDirPath}");
        Debug.Log($"[FileDataHandler] 建構子 - 檔案名稱: {dataFileName}");
    }

    public GameData Load(string profileId)
    {
        // 檢查參數
        if (string.IsNullOrEmpty(profileId))
        {
            Debug.LogError("[FileDataHandler] Load() - profileId 為空！");
            return null;
        }
        
        if (string.IsNullOrEmpty(dataFileName))
        {
            Debug.LogError("[FileDataHandler] Load() - dataFileName 為空！");
            return null;
        }

        string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);
        Debug.Log($"[FileDataHandler] 嘗試讀取檔案: {fullPath}");
        
        GameData loadedData = null;
        
        if(File.Exists(fullPath))
        {
            Debug.Log($"[FileDataHandler] 檔案存在，開始讀取...");
            try
            {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                Debug.Log($"[FileDataHandler] 讀取到的 JSON 資料: {dataToLoad}");
                
                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
                
                if (loadedData != null)
                {
                    Debug.Log($"[FileDataHandler] JSON 解析成功！");
                }
                else
                {
                    Debug.LogError($"[FileDataHandler] JSON 解析失敗，回傳 null");
                }
                
                Debug.Log($"[FileDataHandler] 讀檔成功 → {fullPath} ({dataToLoad.Length} bytes)");
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load data from file: " + fullPath + "\n" + e);
            }
        }
        else
        {
            Debug.LogWarning($"[FileDataHandler] 檔案不存在: {fullPath}");
            
            // 檢查上層目錄是否存在
            string profileDir = Path.Combine(dataDirPath, profileId);
            if (Directory.Exists(profileDir))
            {
                Debug.Log($"[FileDataHandler] Profile 目錄存在: {profileDir}");
                string[] files = Directory.GetFiles(profileDir);
                Debug.Log($"[FileDataHandler] 目錄內檔案: {string.Join(", ", files)}");
            }
            else
            {
                Debug.LogWarning($"[FileDataHandler] Profile 目錄不存在: {profileDir}");
            }
            
            // 檢查根目錄
            if (Directory.Exists(dataDirPath))
            {
                Debug.Log($"[FileDataHandler] 根目錄存在: {dataDirPath}");
                string[] dirs = Directory.GetDirectories(dataDirPath);
                Debug.Log($"[FileDataHandler] 根目錄內的子目錄: {string.Join(", ", dirs)}");
            }
            else
            {
                Debug.LogWarning($"[FileDataHandler] 根目錄不存在: {dataDirPath}");
            }
        }
        
        return loadedData;
    }

    public void Save(GameData data, string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
        {
            Debug.LogError("[FileDataHandler] Save() - profileId 為空！");
            return;
        }
        
        if (string.IsNullOrEmpty(dataFileName))
        {
            Debug.LogError("[FileDataHandler] Save() - dataFileName 為空！");
            return;
        }

        string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);
        Debug.Log($"[FileDataHandler] 嘗試存檔到: {fullPath}");
        
        try
        {
            //放json檔案的位置
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            Debug.Log($"[FileDataHandler] 建立目錄: {Path.GetDirectoryName(fullPath)}");

            string dataToStore = JsonUtility.ToJson(data, true);

            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    Debug.Log("[FileDataHandler] 轉成 JSON：\n" + dataToStore);
                    writer.Write(dataToStore);
                }
                Debug.Log($"[FileDataHandler] 存檔完成 → {fullPath} ({dataToStore.Length} bytes)");
            }
        }
        catch(Exception e)
        {
            Debug.LogError("Error occured when trying to save data to file : " + fullPath + "\n" + e);
        }
    }

    public Dictionary<string, GameData> LoadAllProfiles()
    {
        Dictionary<string, GameData> profileDictionary = new Dictionary<string, GameData>();
        
        Debug.Log($"[FileDataHandler] LoadAllProfiles() - 掃描目錄: {dataDirPath}");

        if (!Directory.Exists(dataDirPath))
        {
            Debug.LogWarning($"[FileDataHandler] 資料目錄不存在: {dataDirPath}");
            return profileDictionary;
        }

        IEnumerable<DirectoryInfo> dirInfos = new DirectoryInfo(dataDirPath).EnumerateDirectories();
        foreach(DirectoryInfo dirInfo in dirInfos)
        {
            string profileId = dirInfo.Name;
            Debug.Log($"[FileDataHandler] 發現 Profile: {profileId}");
            
            string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);
            if(!File.Exists(fullPath))
            {
                Debug.LogWarning("Skipping dir when loading all profiles because it doesnt contain data: " + profileId + " (Expected file: " + fullPath + ")");
                continue;
            }

            GameData profileData = Load(profileId);
            if(profileData != null) // 修正：檢查 profileData 而不是 profileId
            {
                profileDictionary.Add(profileId, profileData);
                Debug.Log($"[FileDataHandler] 成功載入 Profile: {profileId}");
            }
            else
            {
                Debug.LogError("Tried to load profile but something wrong. ProfileId: " + profileId);
            }
        }
        
        Debug.Log($"[FileDataHandler] 總共載入 {profileDictionary.Count} 個 Profile");
        return profileDictionary;
    }
}