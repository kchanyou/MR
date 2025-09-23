#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// MusicRingo 프로젝트의 Resources 폴더 구조를 자동 생성하는 도구
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
        GUILayout.Label("MusicRingo Resources 폴더 구조 생성", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        basePath = EditorGUILayout.TextField("Base Path:", basePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("폴더 구조 생성"))
        {
            GenerateResourceFolders();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("더미 파일 생성"))
        {
            GenerateDummyFiles();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("폴더 구조 정리"))
        {
            CleanupResourceFolders();
        }
    }

    private void GenerateResourceFolders()
    {
        string[] folders = {
            // 레벨 데이터
            "LevelData",
            
            // 오디오 폴더들
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
            
            // UI 리소스
            "UI/Icons/Characters",
            "UI/Icons/Instruments",
            "UI/Icons/Achievements",
            "UI/Backgrounds",
            "UI/Sprites/Buttons",
            "UI/Sprites/Effects",
            
            // 데이터 파일들
            "Data/Localization",
            "Data/Settings",
            
            // 프리팹 리소스
            "Prefabs/UI",
            "Prefabs/Effects",
            "Prefabs/Characters",
            
            // 텍스처 및 스프라이트
            "Textures/Characters",
            "Textures/Backgrounds",
            "Textures/UI",
            
            // 폰트
            "Fonts",
            
            // 애니메이션
            "Animations/Characters",
            "Animations/UI"
        };

        foreach (string folder in folders)
        {
            string fullPath = Path.Combine(basePath, folder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"폴더 생성: {fullPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Resources 폴더 구조 생성 완료!");
    }

    private void GenerateDummyFiles()
    {
        // GameDataContainer.json이 이미 있다면 건너뛰기
        if (!File.Exists(Path.Combine(basePath, "LevelData/GameDataContainer.json")))
        {
            CreateDummyJsonFile("LevelData/GameDataContainer.json", "{}");
        }

        // 더미 오디오 파일들 (텍스트 파일로 대체)
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

        // 설정 파일들
        CreateDummyJsonFile("Data/Settings/DefaultSettings.json",
            "{ \"masterVolume\": 1.0, \"bgmVolume\": 0.8, \"sfxVolume\": 1.0, \"hapticEnabled\": true }");

        // 다국어 파일
        CreateDummyJsonFile("Data/Localization/Korean.json",
            "{ \"START_GAME\": \"게임 시작\", \"SETTINGS\": \"설정\", \"ACHIEVEMENTS\": \"업적\" }");

        AssetDatabase.Refresh();
        Debug.Log("더미 파일 생성 완료!");
    }

    private void CleanupResourceFolders()
    {
        // 빈 폴더나 불필요한 파일들 정리
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
        Debug.Log("폴더 정리 완료!");
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
                Debug.Log($"빈 폴더 삭제: {directory}");
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
            Debug.Log($"더미 파일 생성: {fullPath}");
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
            Debug.Log($"JSON 파일 생성: {fullPath}");
        }
    }
}
#endif
