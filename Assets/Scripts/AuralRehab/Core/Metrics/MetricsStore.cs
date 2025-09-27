using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AuralRehab.Core;

namespace AuralRehab.Core.Metrics {
    public class MetricsStore {
        const string FileName = "metrics.json";
        readonly List<TrialResult> _buffer = new List<TrialResult>();

        [System.Serializable] class Wrapper { public TrialResult[] items; }

        public void Push(TrialResult r) { _buffer.Add(r); }

        public void Flush() {
            var path = JsonStorage.PathFor(FileName);
            List<TrialResult> all = new List<TrialResult>();
            if (File.Exists(path)) {
                var prev = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
                if (prev?.items != null) all.AddRange(prev.items);
            }
            all.AddRange(_buffer);
            var json = JsonUtility.ToJson(new Wrapper { items = all.ToArray() }, true);
            File.WriteAllText(path, json);
            _buffer.Clear();
        }
    }
}