using UnityEngine;
using UnityEngine.UI;

namespace AuralRehab.GamePlay {
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(GridLayoutGroup))]
    public class G6OptionGridAutoSizer : MonoBehaviour {
        [Header("Layout")]
        [SerializeField, Min(1)] int fixedRows = 2;     // 두 줄 고정
        [SerializeField] Vector2 minCellSize = new Vector2(140, 140);
        [SerializeField] Vector2 maxCellSize = new Vector2(600, 400);
        [SerializeField] bool useUnscaledTime = true;

        RectTransform _rt;
        GridLayoutGroup _grid;
        int _lastChildCount = -1;
        Vector2 _lastSize;

        void Awake() {
            _rt = GetComponent<RectTransform>();
            _grid = GetComponent<GridLayoutGroup>();
            if (_grid.constraint != GridLayoutGroup.Constraint.FixedRowCount) {
                _grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                _grid.constraintCount = fixedRows;
            }
        }

        void OnEnable() { Recalc(); }
        void OnRectTransformDimensionsChange() { Recalc(); }
        void Update() {
            // 자식 수가 바뀌면 재계산(선택지 개수 변경 대응)
            if (_lastChildCount != _rt.childCount) Recalc();
            // 부모 크기 변화 대응
            var size = _rt.rect.size;
            if ((size - _lastSize).sqrMagnitude > 1f) Recalc();
        }

        public void Recalc() {
            if (_rt == null || _grid == null) return;
            _lastChildCount = _rt.childCount;
            _lastSize = _rt.rect.size;

            int cols = Mathf.CeilToInt(Mathf.Max(1, _lastChildCount) / (float)fixedRows);

            var pad = _grid.padding;
            float totalW = _rt.rect.width  - pad.left - pad.right  - _grid.spacing.x * (cols - 1);
            float totalH = _rt.rect.height - pad.top  - pad.bottom - _grid.spacing.y * (fixedRows - 1);

            float cellW = totalW / cols;
            float cellH = totalH / fixedRows;

            var cell = new Vector2(
                Mathf.Clamp(cellW, minCellSize.x, maxCellSize.x),
                Mathf.Clamp(cellH, minCellSize.y, maxCellSize.y)
            );
            _grid.cellSize = cell;
            _grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            _grid.constraintCount = fixedRows;
        }
    }
}