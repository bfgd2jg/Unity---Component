using System;
using UnityEngine;
using UnityEngine.Events;

public class VoidEventListener : MonoBehaviour
{
	public VoidEventSO eventSO;

	public UnityEvent response;
    
	private void OnEnable()
	{
		if (eventSO != null)
		{
			VoidEventSO voidEventSO = eventSO;
			voidEventSO.OnEventRaised = (UnityAction)Delegate.Combine(voidEventSO.OnEventRaised, new UnityAction(this.OnEventRaised));
		}
	}

	private void OnDisable()
	{
		if (eventSO != null)
		{
			VoidEventSO voidEventSO = this.eventSO;
			voidEventSO.OnEventRaised = (UnityAction)Delegate.Remove(voidEventSO.OnEventRaised, new UnityAction(this.OnEventRaised));
		}
	}

	private void OnEventRaised()
	{
		response.Invoke();
	}

}
