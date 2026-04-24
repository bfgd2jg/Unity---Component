using UnityEngine;

[CreateAssetMenu(fileName = "New Building Data SO", menuName = "Data/Building Data SO")]
public class BuildingDataSO : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [field: SerializeField] public float CD { get; private set; }
    [field: SerializeField] public float AttackSpeed { get; private set; }

    // 建筑模型（只负责显示 + 占位）
    [field: SerializeField] public BuildingModel BuildingPrefab { get; private set; }
}