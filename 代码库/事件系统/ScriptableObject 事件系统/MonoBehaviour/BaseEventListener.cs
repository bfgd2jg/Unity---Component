using System;
using UnityEngine;
using UnityEngine.Events;

public class BaseEventListener<T> : MonoBehaviour
{

	private void OnEnable()
	{
		if (eventSO != null)
		{
			BaseEventSO<T> baseEventSO = eventSO;
			baseEventSO.OnEventRaised = (UnityAction<T>)Delegate.Combine(baseEventSO.OnEventRaised, new UnityAction<T>(OnEventRaised));
		}
	}

	private void OnDisable()
	{
		if (eventSO != null)
		{
			BaseEventSO<T> baseEventSO = eventSO;
			baseEventSO.OnEventRaised = (UnityAction<T>)Delegate.Remove(baseEventSO.OnEventRaised, new UnityAction<T>(OnEventRaised));
		}
	}

	private void OnEventRaised(T value)
	{
		response.Invoke(value);
	}

	public BaseEventSO<T> eventSO;

	public UnityEvent<T> response;
}
