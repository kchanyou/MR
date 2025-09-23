#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// MusicRingo ������Ʈ�� Resources ���� ������ �ڵ� �����ϴ� ����
/// </summary>
public class ResourceFolderGenerator : EditorWindow
{
    private string basePath = "Assets/Resources/";

    [MenuItem("MusicRingo/Generate Resource Folders")]
    public static void ShowWindow()
    {
        GetWindow<ResourceFolderGenerator>("Resource Folder Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("MusicRingo Resources ���� ���� ����", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        basePath = EditorGUILayout.TextField("Base Path:", basePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("���� ���� ����"))
        {
            GenerateResourceFolders();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("���� ���� ����"))
        {
            GenerateDummyFiles();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("���� ���� ����"))
        {
            CleanupResourceFolders();
        }
    }

    private void GenerateResourceFolders()
    {
        string[] folders = {
            // ���� ������
            "LevelData",
            
            // ����� ������
            "Audio/Character/Dolphin",
            "Audio/Character/Penguin",
            "Audio/Character/Otamatone",
            "Audio/Notes",
            "Audio/Instruments/Strings",
            "Audio/Instruments/Winds",
            "Audio/Instruments/Brass",
            "Audio/Instruments/Percussion",
            "Audio/Instruments/Traditional",
            "Audio/Rhythm",
            "Audio/Melody",
            "Audio/BGM",
            "Audio/SFX",
            
            // UI ���ҽ�
            "UI/Icons/Characters",
            "UI/Icons/Instruments",
            "UI/Icons/Achievements",
            "UI/Backgrounds",
            "UI/Sprites/Buttons",
            "UI/Sprites/Effects",
            
            // ������ ���ϵ�
            "Data/Localization",
            "Data/Settings",
            
            // ������ ���ҽ�
            "Prefabs/UI",
            "Prefabs/Effects",
            "Prefabs/Characters",
            
            // �ؽ�ó �� ��������Ʈ
            "Textures/Characters",
            "Textures/Backgrounds",
            "Textures/UI",
            
            // ��Ʈ
            "Fonts",
            
            // �ִϸ��̼�
            "Animations/Characters",
            "Animations/UI"
        };

        foreach (string folder in folders)
        {
            string fullPath = Path.Combine(basePath, folder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"���� ����: {fullPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Resources ���� ���� ���� �Ϸ�!");
    }

    private void GenerateDummyFiles()
    {
        // GameDataContainer.json�� �̹� �ִٸ� �ǳʶٱ�
        if (!File.Exists(Path.Combine(basePath, "LevelData/GameDataContainer.json")))
        {
            CreateDummyJsonFile("LevelData/GameDataContainer.json", "{}");
        }

        // ���� ����� ���ϵ� (�ؽ�Ʈ ���Ϸ� ��ü)
        string[] dummyAudioFiles = {
            "Audio/Character/Dolphin/dolphin_stage1_intro.txt",
            "Audio/Character/Penguin/penguin_stage1_intro.txt",
            "Audio/Character/Otamatone/otamatone_stage1_intro.txt",
            "Audio/SFX/success_sound.txt",
            "Audio/SFX/wrong_sound.txt",
            "Audio/SFX/click_sound.txt",
            "Audio/SFX/metronome_tick.txt",
            "Audio/BGM/main_theme.txt"
        };

        foreach (string file in dummyAudioFiles)
        {
            CreateDummyTextFile(file, $"Dummy audio file: {Path.GetFileName(file)}");
        }

        // ���� ���ϵ�
        CreateDummyJsonFile("Data/Settings/DefaultSettings.json",
            "{ \"masterVolume\": 1.0, \"bgmVolume\": 0.8, \"sfxVolume\": 1.0, \"hapticEnabled\": true }");

        // �ٱ��� ����
        CreateDummyJsonFile("Data/Localization/Korean.json",
            "{ \"START_GAME\": \"���� ����\", \"SETTINGS\": \"����\", \"ACHIEVEMENTS\": \"����\" }");

        AssetDatabase.Refresh();
        Debug.Log("���� ���� ���� �Ϸ�!");
    }

    private void CleanupResourceFolders()
    {
        // �� ������ ���ʿ��� ���ϵ� ����
        string[] foldersToCheck = {
            basePath + "Audio",
            basePath + "UI",
            basePath + "Data",
            basePath + "Prefabs"
        };

        foreach (string folder in foldersToCheck)
        {
            if (Directory.Exists(folder))
            {
                CleanupEmptyFolders(folder);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("���� ���� �Ϸ�!");
    }

    private void CleanupEmptyFolders(string path)
    {
        foreach (string directory in Directory.GetDirectories(path))
        {
            CleanupEmptyFolders(directory);

            if (Directory.GetFiles(directory).Length == 0 &&
                Directory.GetDirectories(directory).Length == 0)
            {
                Directory.Delete(directory);
                Debug.Log($"�� ���� ����: {directory}");
            }
        }
    }

    private void CreateDummyTextFile(string relativePath, string content)
    {
        string fullPath = Path.Combine(basePath, relativePath);
        string directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, content);
            Debug.Log($"���� ���� ����: {fullPath}");
        }
    }

    private void CreateDummyJsonFile(string relativePath, string jsonContent)
    {
        string fullPath = Path.Combine(basePath, relativePath);
        string directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, jsonContent);
            Debug.Log($"JSON ���� ����: {fullPath}");
        }
    }
}
#endif
