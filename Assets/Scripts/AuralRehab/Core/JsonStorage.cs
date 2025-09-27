using System.IO;
using UnityEngine;

namespace AuralRehab.Core {
    public static class JsonStorage {
        public static string PathFor(string file) =>
            System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, file);

        public static void Save<T>(string file, T data) {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(PathFor(file), json);
        }

        public static bool TryLoad<T>(string file, out T data) where T : new() {
            var path = PathFor(file);
            if (!File.Exists(path)) { data = new T(); return false; }
            var json = File.ReadAllText(path);
            data = JsonUtility.FromJson<T>(json);
            if (data == null) data = new T();
            return true;
        }
    }
}