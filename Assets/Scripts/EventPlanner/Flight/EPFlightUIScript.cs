using ModApi.Ui;
using UI.Xml;
using ModApi;
using UnityEngine;

namespace Assets.Scripts
{
    public class EPFlightUIScript : MonoBehaviour
    {
        private XmlLayoutController _controller;

        public void OnLayoutRebubuilt(IXmlLayoutController controller)
        {
            _controller = (XmlLayoutController)controller;
        }

        public void OnIconClicked()
        {
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage(":3");
        }
    }
}
