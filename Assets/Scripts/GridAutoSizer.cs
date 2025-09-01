using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoSizer : UIBehaviour
{
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private bool squareCells = true;

    private RectTransform rt;
    private int columns = 4;
    private int rows = 4;

    protected override void Awake()
    {
        base.Awake();
        rt = GetComponent<RectTransform>();
        if (!grid) grid = GetComponent<GridLayoutGroup>();
    }

    public void Initialize(GridLayoutGroup target)
    {
        grid = target;
        if (!rt) rt = GetComponent<RectTransform>();
    }

    public void SetGrid(int cols, int rows)
    {
        this.columns = Mathf.Max(1, cols);
        this.rows = Mathf.Max(1, rows);
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = this.columns;
        }
    }

    public void SetSquareCells(bool square) => squareCells = square;

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        RecalculateNow();
    }

    public void RecalculateNow()
    {
        if (!grid || !rt || columns <= 0 || rows <= 0) return;

        Rect r = rt.rect;

        float totalW = r.width;
        float totalH = r.height;

        float padH = grid.padding.left + grid.padding.right;
        float padV = grid.padding.top + grid.padding.bottom;

        float innerW = Mathf.Max(0f, totalW - padH);
        float innerH = Mathf.Max(0f, totalH - padV);

        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;

        float cellW = (innerW - spacingX * (columns - 1)) / columns;
        float cellH = (innerH - spacingY * (rows - 1)) / rows;

        if (squareCells)
        {
            float size = Mathf.Floor(Mathf.Min(cellW, cellH));
            grid.cellSize = new Vector2(size, size);
        }
        else
        {
            grid.cellSize = new Vector2(Mathf.Floor(cellW), Mathf.Floor(cellH));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
