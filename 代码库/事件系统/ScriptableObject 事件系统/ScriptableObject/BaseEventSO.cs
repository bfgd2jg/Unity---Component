using UnityEngine;
using UnityEngine.Events;

public class BaseEventSO<T> : ScriptableObject
{
	public UnityAction<T> OnEventRaised;

	public string lastObject;
	
	public void RaiseEvent(T value, object sender = null)
	{
        OnEventRaised?.Invoke(value);

        lastObject = sender.ToString();
	}
}

