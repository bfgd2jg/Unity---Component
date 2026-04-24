using System.Collections.Generic;
using UnityEngine;

public class BuildingGrid : MonoBehaviour
{
    [SerializeField] int width = 10;
    [SerializeField] int height = 10;

    BudingGridCell[,] grid;

    void Awake()
    {
        grid = new BudingGridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new BudingGridCell();
            }
        }
    }

    // 世界坐标 → 格子坐标
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos - transform.position).x / BuildingSystem.CellSize);
        int y = Mathf.FloorToInt((worldPos - transform.position).z / BuildingSystem.CellSize);
        return new Vector2Int(x, y);
    }

    // 格子 → 世界中心点
    public Vector3 GridToWorld(int x, int y)
    {
        float centerX = transform.position.x + (x * BuildingSystem.CellSize) + BuildingSystem.CellSize / 2f;
        float centerZ = transform.position.z + (y * BuildingSystem.CellSize) + BuildingSystem.CellSize / 2f;

        return new Vector3(centerX, transform.position.y, centerZ);
    }

    // 判断点是否在网格范围内
    public bool IsPointInGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;

        float maxWidth = width * BuildingSystem.CellSize;
        float maxHeight = height * BuildingSystem.CellSize;

        return local.x >= 0 && local.x <= maxWidth &&
               local.z >= 0 && local.z <= maxHeight;
    }

    // 建造检测（核心：origin + offsets）
    public bool CanBuild(Vector2Int origin, List<Vector2Int> offsets)
    {
        foreach (var offset in offsets)
        {
            int x = origin.x + offset.x;
            int y = origin.y + offset.y;

            if (!IsValid(x, y) || !grid[x, y].IsEmpty())
                return false;
        }

        return true;
    }

    // 设置占用
    public void SetBuilding(Building building, Vector2Int origin, List<Vector2Int> offsets)
    {
        foreach (var offset in offsets)
        {
            int x = origin.x + offset.x;
            int y = origin.y + offset.y;

            if (IsValid(x, y))
                grid[x, y].SetBuilding(building);
        }
    }

    bool IsValid(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;

        for (int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(origin + new Vector3(0, 0, y * BuildingSystem.CellSize),
                            origin + new Vector3(width * BuildingSystem.CellSize, 0, y * BuildingSystem.CellSize));
        }

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(origin + new Vector3(x * BuildingSystem.CellSize, 0, 0),
                            origin + new Vector3(x * BuildingSystem.CellSize, 0, height * BuildingSystem.CellSize));
        }
    }
}

public class BudingGridCell
{
    Building building;

    public void SetBuilding(Building building)
    {
        this.building = building;
    }

    public bool IsEmpty()
    {
        return building == null;
    }
}