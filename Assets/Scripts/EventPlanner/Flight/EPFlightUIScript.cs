using ModApi.Ui;
using UI.Xml;
using ModApi;
using UnityEngine;
using System.Windows.Forms;

namespace Assets.Scripts
{
    public class EPFlightUIScript : MonoBehaviour
    {
        private XmlLayoutController _controller;
        private XmlElement _mainPanel;

        public void OnLayoutRebubuilt(IXmlLayoutController controller)
        {
            _controller = (XmlLayoutController)controller;

            _mainPanel = _controller.xmlLayout.GetElementById("ep-main-panel");
        }

        public void OnTogglePanelState()
        {
            _mainPanel.SetActive(!_mainPanel.isActiveAndEnabled);
        }

        public void CreateEvent()
        {
            EPManager.Instance.CreateEvent("Test", "AAA", Game.Instance.FlightScene.FlightState.Time + 3600.0, 600.0);
        }
    }
}
