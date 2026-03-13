using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventSO", menuName = "Events/VoidEventSO")]
public class VoidEventSO : ScriptableObject
{
	public UnityAction OnEventRaised;

	public string 最后广播的物体;

	public void RaiseEvent(object sender = null)
	{
		UnityAction onEventRaised = OnEventRaised;
		if (onEventRaised != null)
		{
			onEventRaised();
		}
		最后广播的物体 = sender.ToString();
	}

}
