using UnityEngine;

public class BuildingPreview : MonoBehaviour
{
    public BuildingDataSO Data { get; private set; }
    public BuildingModel BuildingModel { get; private set; }

    public void Setup(BuildingDataSO data)
    {
        Data = data;

        // 生成模型
        BuildingModel = Instantiate(data.BuildingPrefab, transform);
    }

    public void Rotate(float angle)
    {
        BuildingModel.Rotate(angle);
    }
}