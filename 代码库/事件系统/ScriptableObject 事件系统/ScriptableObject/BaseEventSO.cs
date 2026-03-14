using UnityEngine;
using UnityEngine.Events;

public class BaseEventSO<T> : ScriptableObject
{
	public void RaiseEvent(T value, object sender = null)
	{
		UnityAction<T> onEventRaised = OnEventRaised;
		if (onEventRaised != null)
		{
			onEventRaised(value);
		}
		最后广播的物体 = sender.ToString();
	}

	public UnityAction<T> OnEventRaised;

	public string 最后广播的物体;
}
