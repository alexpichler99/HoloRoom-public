using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseButton : MonoBehaviour, IInputClickHandler
{

    public void OnInputClicked(InputClickedEventData eventData)
    {
        var parent = this.transform.parent;
        if (parent != null)
            parent.gameObject.SetActive(false);
    }
}
