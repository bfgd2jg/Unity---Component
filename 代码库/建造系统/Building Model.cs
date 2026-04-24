using System.Collections.Generic;
using UnityEngine;

public class BuildingModel : MonoBehaviour
{
    public float Rotation => wrapper.eulerAngles.y;

    [SerializeField] Transform wrapper;

    BuildingShapeUnit[] shapeUnits;

    void Awake()
    {
        shapeUnits = GetComponentsInChildren<BuildingShapeUnit>();
    }

    public void Rotate(float angle)
    {
        wrapper.Rotate(0, angle, 0);
    }

    // 返回格子偏移
    public List<Vector2Int> GetShapeOffsets()
    {
        List<Vector2Int> offsets = new();

        foreach (var unit in shapeUnits)
        {
            Vector3 local = unit.transform.localPosition;

            int x = Mathf.RoundToInt(local.x / BuildingSystem.CellSize);
            int y = Mathf.RoundToInt(local.z / BuildingSystem.CellSize);

            offsets.Add(new Vector2Int(x, y));
        }

        return offsets;
    }
}