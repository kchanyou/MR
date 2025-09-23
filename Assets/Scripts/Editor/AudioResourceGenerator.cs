#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ����� ���ҽ� �ڵ� ���� �� ���� ����
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
        GUILayout.Label("����� ���ҽ� ���� ����", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        basePath = EditorGUILayout.TextField("�⺻ ���:", basePath);

        EditorGUILayout.Space();

        generateNotes = EditorGUILayout.Toggle("��ǥ ���� ����", generateNotes);
        generateInstruments = EditorGUILayout.Toggle("�Ǳ� ���� ����", generateInstruments);
        generateCharacterVoices = EditorGUILayout.Toggle("ĳ���� ���� ���� ����", generateCharacterVoices);

        EditorGUILayout.Space();

        if (GUILayout.Button("���� ���� ����"))
        {
            GenerateAudioFolders();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("�⺻ ����� ���� ���� (����)"))
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
                Debug.Log($"���� ����: {fullPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("����� ���� ���� ���� �Ϸ�!");
    }

    private void GenerateDummyAudioFiles()
    {
        // �⺻ ��ǥ ���ϵ�
        string[] noteNames = { "C4", "D4", "E4", "F4", "G4", "A4", "B4", "C5", "D5", "E5", "F5", "G5", "A5", "B5" };

        foreach (string note in noteNames)
        {
            CreateDummyAudioFile($"Audio/Notes/{note}.wav");
        }

        // �Ǳ� ���ϵ�
        string[] instruments = { "piano", "guitar", "violin", "flute", "trumpet" };

        foreach (string instrument in instruments)
        {
            CreateDummyAudioFile($"Audio/Instruments/Strings/{instrument}_C4.wav");
        }

        // ĳ���� ���� ���ϵ�
        string[] characters = { "Dolphin", "Penguin", "Otamatone" };

        foreach (string character in characters)
        {
            for (int i = 1; i <= 8; i++)
            {
                CreateDummyAudioFile($"Audio/Character/{character}/{character.ToLower()}_stage{i}_intro.wav");
            }
        }

        // ȿ���� ���ϵ�
        string[] sfxNames = { "success_sound", "wrong_sound", "click_sound", "metronome_tick" };

        foreach (string sfx in sfxNames)
        {
            CreateDummyAudioFile($"Audio/SFX/{sfx}.wav");
        }

        AssetDatabase.Refresh();
        Debug.Log("���� ����� ���� ���� �Ϸ�!");
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
            // ���� ����� ���� ���� (�����δ� ������ ����� �����͸� ��� ��)
            File.WriteAllBytes(fullPath, new byte[1024]); // �ӽ� ����Ʈ �迭
            Debug.Log($"���� ���� ����: {fullPath}");
        }
    }
}
*/
#endif