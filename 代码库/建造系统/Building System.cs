using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingSystem : MonoBehaviour
{
    // 格子大小
    public const float CellSize = 1f;

    [SerializeField] BuildingPreview buildingPreview;
    [SerializeField] Building buildingScript;
    [SerializeField] BuildingDataSO[] data;

    BuildingPreview preview;
    BuildingGrid currentGrid;
    BuildingGrid[] allGrids;

    Vector3 hitPoint;

    void Start()
    {
        RefreshGrids();
    }

    void RefreshGrids()
    {
        allGrids = FindObjectsByType<BuildingGrid>(FindObjectsSortMode.None);
    }

    void Update()
    {
        bool isOverGrid = TryGetGridByMath(out currentGrid, out hitPoint);

        if (preview != null)
            HandlePreview(isOverGrid, hitPoint);

        // 数字键选择建筑
        for (int i = 0; i < data.Length; i++)
        {
            var key = Keyboard.current[(Key)((int)Key.Digit1 + i)];

            if (key != null && key.wasPressedThisFrame)
                SelectBuilding(i);
        }
    }

    public void SelectBuilding(int index)
    {
        if (index >= data.Length || data[index] == null) return;

        if (preview != null)
            Destroy(preview.gameObject);

        preview = Instantiate(buildingPreview, hitPoint, Quaternion.identity, transform);
        preview.Setup(data[index]);
    }

    void HandlePreview(bool isOverGrid, Vector3 hitPoint)
    {
        if (isOverGrid && currentGrid != null)
        {
            // 计算格子
            Vector2Int origin = currentGrid.WorldToGridPosition(hitPoint);

            // 吸附
            preview.transform.position = currentGrid.GridToWorld(origin.x, origin.y);

            // 获取占位
            var offsets = preview.BuildingModel.GetShapeOffsets();

            bool canBuild = currentGrid.CanBuild(origin, offsets);

            // 左键建造
            if (canBuild && Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceBuilding(origin, offsets);
            }
        }
        else
        {
            preview.transform.position = hitPoint;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Destroy(preview.gameObject);
            preview = null;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
            preview.Rotate(90);
    }

    void PlaceBuilding(Vector2Int origin, List<Vector2Int> offsets)
    {
        Building building = Instantiate(buildingScript, preview.transform.position, Quaternion.identity, transform);

        building.Setup(preview.Data, preview.BuildingModel.Rotation);

        currentGrid.SetBuilding(building, origin, offsets);

        Destroy(preview.gameObject);
        preview = null;
    }

    bool TryGetGridByMath(out BuildingGrid hitGrid, out Vector3 hitPoint)
    {
        hitGrid = null;
        hitPoint = Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        foreach (var grid in allGrids)
        {
            Plane plane = new Plane(Vector3.up, grid.transform.position);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);

                if (grid.IsPointInGrid(point))
                {
                    hitGrid = grid;
                    hitPoint = point;
                    return true;
                }
            }
        }

        Plane defaultPlane = new Plane(Vector3.up, Vector3.zero);
        if (defaultPlane.Raycast(ray, out float d))
            hitPoint = ray.GetPoint(d);

        return false;
    }
}