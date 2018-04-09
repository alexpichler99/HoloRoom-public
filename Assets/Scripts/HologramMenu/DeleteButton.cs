using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteButton : MonoBehaviour, IInputClickHandler
{
    public void OnInputClicked(InputClickedEventData eventData)
    {
        var containerScript = this.GetComponentInParent<ContainerScript>();
        if (containerScript != null)
            Destroy(containerScript.selectedHologram.gameObject);
    }
}
