using ModApi.Ui;
using UI.Xml;
using UnityEngine;

public class EPFlightUIScript : MonoBehaviour
{
    private XmlLayoutController _controller;
    
    public void OnLayoutRebubuilt(IXmlLayoutController controller)
    {
        _controller = (XmlLayoutController)controller;
    }

    public void OnIconClicked()
    {
        Debug.Log("AAAAAA");
    }
}
