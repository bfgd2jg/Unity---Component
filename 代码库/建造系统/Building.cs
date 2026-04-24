using UnityEngine;

public class Building : MonoBehaviour
{
    BuildingModel model;

    public void Setup(BuildingDataSO data, float rotation)
    {
        model = Instantiate(data.BuildingPrefab, transform);
        model.Rotate(rotation);

        gameObject.name = data.Name;
    }
}