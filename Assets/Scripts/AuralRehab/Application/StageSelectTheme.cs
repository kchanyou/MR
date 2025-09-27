using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace AuralRehab.Application {
    /// <summary>
    /// 스테이지 선택 화면의 비주얼 테마(배경/타일/라벨 색 등).
    /// - 배치는 바꾸지 않고, 스프라이트/색만 교체
    /// </summary>
    [CreateAssetMenu(menuName = "AuralRehab/Stage Select Theme", fileName = "StageSelectThemeA")]
    public class StageSelectTheme : ScriptableObject {
        [Header("Background")]
        public Sprite backgroundSprite;              // 전체 배경 이미지(없으면 backgroundColor만 사용)
        public Color  backgroundColor = Color.black; // 스프라이트가 없을 때 또는 틴트

        [Header("Tile Sprites")]
        public Sprite tileNormal;
        public Sprite tileLocked;
        public Sprite tileCleared;

        [Header("Tile Tints")]
        public Color tileTintNormal  = new Color(1, 1, 1, 0.10f);
        public Color tileTintLocked  = new Color(1, 1, 1, 0.04f);
        public Color tileTintCleared = new Color(0.20f, 0.80f, 0.35f, 0.28f);

        [Header("Text Colors")]
        public Color textNormal = Color.white;
        public Color textLocked = new Color(1, 1, 1, 0.45f);
        public Color subTextNormal = new Color(1, 1, 1, 0.85f);

        [Header("Boss Accent (Level 8)")]
        public bool  useBossAccent = true;
        public Color bossAccent    = new Color(1.0f, 0.65f, 0.1f, 0.25f);

        [Header("Optional Rewards/Markers")]
        public Sprite rewardSprite;                  // 예: 보석 아이콘
        public List<int> rewardStages = new List<int>(); // 예: {5, 7}
    }
}