using UnityEngine;

public static class CharacterSaveManager
{
    private const string SAVE_KEY = "SelectedCharacter";
    public static void Save(CharacterType type)
    {
        PlayerPrefs.SetInt(SAVE_KEY, (int)type);
        PlayerPrefs.Save();
        Debug.Log($"[CharacterSaveManager] Đã lưu nhân vật: {type}");
    }
    public static CharacterType Load(CharacterType defaultType = CharacterType.Mage)
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log($"[CharacterSaveManager] Chưa có dữ liệu, dùng mặc định: {defaultType}");
            return defaultType;
        }

        int saved = PlayerPrefs.GetInt(SAVE_KEY);
        CharacterType type = (CharacterType)saved;
        Debug.Log($"[CharacterSaveManager] Load nhân vật đã lưu: {type}");
        return type;
    }

    public static bool HasSaved()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("[CharacterSaveManager] Đã xóa dữ liệu nhân vật.");
    }
}
