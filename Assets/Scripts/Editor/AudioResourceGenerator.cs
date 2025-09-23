#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 오디오 리소스 자동 생성 및 관리 도구
/// </summary>
/*{
public class AudioResourceGenerator : EditorWindow
    private string basePath = "Assets/Resources/Audio/";
    private bool generateNotes = true;
    private bool generateInstruments = true;
    private bool generateCharacterVoices = true;

    [MenuItem("MusicRingo/Audio Resource Generator")]
    public static void ShowWindow()
    {
        GetWindow<AudioResourceGenerator>("Audio Resource Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("오디오 리소스 생성 도구", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        basePath = EditorGUILayout.TextField("기본 경로:", basePath);

        EditorGUILayout.Space();

        generateNotes = EditorGUILayout.Toggle("음표 폴더 생성", generateNotes);
        generateInstruments = EditorGUILayout.Toggle("악기 폴더 생성", generateInstruments);
        generateCharacterVoices = EditorGUILayout.Toggle("캐릭터 음성 폴더 생성", generateCharacterVoices);

        EditorGUILayout.Space();

        if (GUILayout.Button("폴더 구조 생성"))
        {
            GenerateAudioFolders();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("기본 오디오 파일 생성 (더미)"))
        {
            GenerateDummyAudioFiles();
        }
    }

    private void GenerateAudioFolders()
    {
        string[] folders = {
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
            "Audio/SFX"
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
        Debug.Log("오디오 폴더 구조 생성 완료!");
    }

    private void GenerateDummyAudioFiles()
    {
        // 기본 음표 파일들
        string[] noteNames = { "C4", "D4", "E4", "F4", "G4", "A4", "B4", "C5", "D5", "E5", "F5", "G5", "A5", "B5" };

        foreach (string note in noteNames)
        {
            CreateDummyAudioFile($"Audio/Notes/{note}.wav");
        }

        // 악기 파일들
        string[] instruments = { "piano", "guitar", "violin", "flute", "trumpet" };

        foreach (string instrument in instruments)
        {
            CreateDummyAudioFile($"Audio/Instruments/Strings/{instrument}_C4.wav");
        }

        // 캐릭터 음성 파일들
        string[] characters = { "Dolphin", "Penguin", "Otamatone" };

        foreach (string character in characters)
        {
            for (int i = 1; i <= 8; i++)
            {
                CreateDummyAudioFile($"Audio/Character/{character}/{character.ToLower()}_stage{i}_intro.wav");
            }
        }

        // 효과음 파일들
        string[] sfxNames = { "success_sound", "wrong_sound", "click_sound", "metronome_tick" };

        foreach (string sfx in sfxNames)
        {
            CreateDummyAudioFile($"Audio/SFX/{sfx}.wav");
        }

        AssetDatabase.Refresh();
        Debug.Log("더미 오디오 파일 생성 완료!");
    }

    private void CreateDummyAudioFile(string relativePath)
    {
        string fullPath = Path.Combine(basePath, relativePath);
        string directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(fullPath))
        {
            // 더미 오디오 파일 생성 (실제로는 적절한 오디오 데이터를 써야 함)
            File.WriteAllBytes(fullPath, new byte[1024]); // 임시 바이트 배열
            Debug.Log($"더미 파일 생성: {fullPath}");
        }
    }
}
*/
#endif