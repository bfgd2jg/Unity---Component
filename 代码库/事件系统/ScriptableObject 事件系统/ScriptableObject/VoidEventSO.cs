using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventSO", menuName = "Events/VoidEventSO")]
public class VoidEventSO : ScriptableObject
{
	public UnityAction OnEventRaised;

	public string lastObject;

	public void RaiseEvent(object sender = null)
	{
        OnEventRaised?.Invoke();

        lastObject = sender.ToString();
	}
}
