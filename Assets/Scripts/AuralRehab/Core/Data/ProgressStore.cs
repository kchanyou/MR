using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AuralRehab.Core.Data {
    /// <summary>
    /// 캠페인별 스테이지 진행도 저장/조회 유틸.
    /// - 저장 위치: persistentDataPath/progress.json
    /// - 모델: { "maxCleared": { "A": 0, "B": 0, "C": 0 } }  (0=아직 클리어 없음)
    /// - 해제 규칙: stage 1은 항상 해제, 그 외는 (stage-1) <= maxCleared 일 때 해제
    /// </summary>
    public static class ProgressStore {
        const string FileName = "progress.json";

        [Serializable]
        class ProgressDTO {
            public Dictionary<string, int> maxCleared = new Dictionary<string, int> {
                { "A", 0 }, { "B", 0 }, { "C", 0 }
            };
        }

        static ProgressDTO _cache;

        static string PathAbs => System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, FileName);

        static ProgressDTO Load() {
            if (_cache != null) return _cache;
            try {
                if (!File.Exists(PathAbs)) {
                    _cache = new ProgressDTO(); // 기본값
                    Save();                      // 파일 생성
                    return _cache;
                }
                var json = File.ReadAllText(PathAbs);
                _cache = JsonUtility.FromJson<ProgressDTO>(json) ?? new ProgressDTO();
            } catch (Exception e) {
                Debug.LogWarning($"Progress load fail: {PathAbs}\n{e}");
                _cache = new ProgressDTO();
            }
            // 키 보정
            if (!_cache.maxCleared.ContainsKey("A")) _cache.maxCleared["A"] = 0;
            if (!_cache.maxCleared.ContainsKey("B")) _cache.maxCleared["B"] = 0;
            if (!_cache.maxCleared.ContainsKey("C")) _cache.maxCleared["C"] = 0;
            return _cache;
        }

        static void Save() {
            try {
                var dir = System.IO.Path.GetDirectoryName(PathAbs);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonUtility.ToJson(_cache, true);
                File.WriteAllText(PathAbs, json);
            } catch (Exception e) {
                Debug.LogWarning($"Progress save fail: {PathAbs}\n{e}");
            }
        }

        static string Key(AuralRehab.Application.CampaignId id) => id switch {
            AuralRehab.Application.CampaignId.A => "A",
            AuralRehab.Application.CampaignId.B => "B",
            AuralRehab.Application.CampaignId.C => "C",
            _ => "A"
        };

        public static int GetMaxCleared(AuralRehab.Application.CampaignId id) {
            var p = Load();
            return p.maxCleared[Key(id)];
        }

        /// <summary>클리어 갱신. 더 큰 값일 때만 저장.</summary>
        public static void SetCleared(AuralRehab.Application.CampaignId id, int stage1to8) {
            stage1to8 = Mathf.Clamp(stage1to8, 1, 8);
            var p = Load();
            var k = Key(id);
            if (p.maxCleared[k] < stage1to8) {
                p.maxCleared[k] = stage1to8;
                Save();
            }
        }

        public static bool IsUnlocked(AuralRehab.Application.CampaignId id, int stage1to8) {
            if (stage1to8 <= 1) return true;
            return GetMaxCleared(id) >= (stage1to8 - 1);
        }

        /// <summary>개발/테스트용 진행도 초기화</summary>
        public static void ResetAll() {
            _cache = new ProgressDTO();
            Save();
        }
    }
}