using UnityEngine;

/// <summary>
/// ���� ��ü���� ����ϴ� ������� �����ϴ� Ŭ����
/// </summary>
public static class GameConstants
{
    // ���� ����
    public const string GAME_VERSION = "1.0.0";

    // �� �̸���
    public const string SCENE_MAIN_MENU = "MainMenu";
    public const string SCENE_GAME_SELECT = "GameSelect";
    public const string SCENE_STAGE_SELECT = "2_Stage";
    public const string SCENE_GAMEPLAY = "3_GameScene";

    // ĳ���� Ÿ��
    public const string CHARACTER_DOLPHIN = "Dolphin";
    public const string CHARACTER_PENGUIN = "Penguin";
    public const string CHARACTER_OTAMATONE = "Otamatone";

    // ����� ����
    public const float DEFAULT_MASTER_VOLUME = 1.0f;
    public const float DEFAULT_BGM_VOLUME = 0.8f;
    public const float DEFAULT_SFX_VOLUME = 1.0f;
    public const float DEFAULT_FREQUENCY_MAX = 20000f;

    // �����÷��� ����
    public const int QUESTIONS_PER_STAGE = 10;
    public const int STAGES_PER_MODE = 4;
    public const int MODES_PER_CHARACTER = 2;
    public const int TOTAL_STAGES_PER_CHARACTER = 8;

    // ���� ����
    public const int SCORE_PER_CORRECT_ANSWER = 10;
    public const int PERFECT_SCORE_PER_STAGE = 100;

    // Ÿ�̹� ���� (��� ���ӿ�)
    public const float PERFECT_TIMING = 0.1f;
    public const float GOOD_TIMING = 0.2f;
    public const float ACCEPTABLE_TIMING = 0.3f;

    // UI �ִϸ��̼� ����
    public const float UI_FADE_DURATION = 0.3f;
    public const float BUTTON_SCALE_DURATION = 0.2f;
    public const float FEEDBACK_DISPLAY_TIME = 2.0f;

    // ���� ���
    public const string SAVE_FILE_NAME = "cochlear_game_data.json";
    public const string GAME_DATA_PATH = "LevelData/GameDataContainer";

    // PlayerPrefs Ű
    public const string PREF_SELECTED_STAGE = "SelectedStage";
    public const string PREF_MASTER_VOLUME = "MasterVolume";
    public const string PREF_BGM_VOLUME = "BGMVolume";
    public const string PREF_SFX_VOLUME = "SFXVolume";
    public const string PREF_HAPTIC_ENABLED = "HapticEnabled";

    // ������Ʈ Ǯ �±�
    public const string POOL_TAG_PARTICLE_SUCCESS = "ParticleSuccess";
    public const string POOL_TAG_PARTICLE_FAIL = "ParticleFail";
    public const string POOL_TAG_UI_FEEDBACK = "UIFeedback";

    // ���̾�
    public const int LAYER_UI = 5;
    public const int LAYER_EFFECTS = 8;

    // �±�
    public const string TAG_PLAYER = "Player";
    public const string TAG_UI = "UI";
    public const string TAG_AUDIO = "Audio";
}

/// <summary>
/// ���ӿ��� ����ϴ� ���� �ȷ�Ʈ
/// </summary>
public static class GameColors
{
    // ĳ���� �׸� ����
    public static readonly Color DOLPHIN_BLUE = new Color(0.2f, 0.7f, 1.0f);
    public static readonly Color PENGUIN_WHITE = new Color(0.9f, 0.9f, 1.0f);
    public static readonly Color OTAMATONE_ORANGE = new Color(1.0f, 0.6f, 0.2f);

    // UI ����
    public static readonly Color BUTTON_NORMAL = Color.white;
    public static readonly Color BUTTON_HOVER = Color.yellow;
    public static readonly Color BUTTON_PRESSED = new Color(0.8f, 0.8f, 0.8f);
    public static readonly Color BUTTON_DISABLED = Color.gray;

    // �ǵ�� ����
    public static readonly Color CORRECT_GREEN = Color.green;
    public static readonly Color WRONG_RED = Color.red;
    public static readonly Color PERFECT_GOLD = new Color(1.0f, 0.84f, 0.0f);

    // ���� ����
    public static readonly Color LOCKED_GRAY = new Color(0.5f, 0.5f, 0.5f);
    public static readonly Color UNLOCKED_WHITE = Color.white;
    public static readonly Color CLEARED_GREEN = new Color(0.4f, 0.8f, 0.4f);
}
