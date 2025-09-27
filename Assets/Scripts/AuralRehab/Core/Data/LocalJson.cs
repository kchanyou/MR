using System;
using System.IO;
using UnityEngine;

namespace AuralRehab.Core.Data {
    /// <summary>
    /// 단순 JSON 직렬화/역직렬화 유틸 + 프로필 DTO
    /// </summary>
    public static class LocalJson {
        public static bool TrySave<T>(string path, T data, bool pretty = false) {
            try {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonUtility.ToJson(data, pretty);
                File.WriteAllText(path, json);
                return true;
            } catch (Exception e) {
                Debug.LogWarning($"Save fail: {path}\n{e}");
                return false;
            }
        }

        public static bool TryLoad<T>(string path, out T data) {
            data = default;
            try {
                if (!File.Exists(path)) return false;
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
                return true;
            } catch (Exception e) {
                Debug.LogWarning($"Load fail: {path}\n{e}");
                return false;
            }
        }
    }

    [Serializable]
    public class ProfileData {
        public string id;
        public string nickname;
        public long createdAtUnix;

        public static ProfileData Create(string name) {
            return new ProfileData {
                id = System.Guid.NewGuid().ToString("N"),
                nickname = string.IsNullOrEmpty(name) ? "게스트" : name,
                createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}