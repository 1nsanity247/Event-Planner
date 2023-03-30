using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using ModApi.Math;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class EPFlightUIScript : MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel;
        private XmlElement listItemTemplate;

        private bool createEventPanelOpen = false;
        private readonly float[] timeFactors = { 86400.0f, 3600.0f, 60.0f, 86400.0f, 3600.0f, 60.0f };

        public void OnLayoutRebubuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            mainPanel = controller.xmlLayout.GetElementById("ep-main-panel");

            listItemTemplate = controller.xmlLayout.GetElementById("text-list-item");
        }

        public void OnTogglePanelState() { mainPanel.SetActive(!mainPanel.isActiveAndEnabled); }

        public void AddEventButtonClicked()
        {
            if (createEventPanelOpen) return;

            XmlElement panel = controller.xmlLayout.GetElementById("ep-create-event-panel");
            panel.SetActive(true);
            createEventPanelOpen = true;

            panel.GetElementByInternalId("title-input").SetAndApplyAttribute("text", "");
            panel.GetElementByInternalId("desc-input").SetAndApplyAttribute("text", "");
            for (int i = 0; i < 6; i++)
                panel.GetElementByInternalId("time-input" + i).SetAndApplyAttribute("text", "");
        }

        public void OnCloseCreateEventPanel()
        {
            controller.xmlLayout.GetElementById("ep-create-event-panel").SetActive(false);
            createEventPanelOpen = false;
        }

        public void OnCreateEvent()
        {
            XmlElement panel = controller.xmlLayout.GetElementById("ep-create-event-panel");
            
            var inputs = panel.GetFormData();

            string title = inputs["title-input"];
            string desc = inputs["desc-input"];

            if (title == null || title.Length == 0) 
            {
                ShowMessage("Failed to create Event, invalid title!");
                return;
            }

            if (desc == null || desc.Length == 0)
                desc = "-";

            float[] times = { 0.0f, 0.0f };
            
            for (int i = 0; i < 6; i++)
                times[i/3] += timeFactors[i] * (float.TryParse(inputs["time-input" + i], out float res) ? res : 0);

            if (times[0] <= 0.0f || times[1] < 0.0f)
            {
                ShowMessage("Failed to create Event, invalid time inputs!");
                return;
            }

            EPManager.Instance.CreateEvent(title, desc, Game.Instance.FlightScene.FlightState.Time + times[0], times[1]);

            panel.SetActive(false);
            createEventPanelOpen = false;
        }

        public void EventElementClicked(int id)
        {
            print(id);
        }

        public void ShowNotifPanel(EPEvent reachedEvent)
        {
            XmlElement notifPanel = controller.xmlLayout.GetElementById("ep-notif-panel");
            notifPanel.SetActive(true);
            notifPanel.GetElementByInternalId("ep-notif-title").SetAndApplyAttribute("text", reachedEvent.title);
            notifPanel.GetElementByInternalId("description").SetAndApplyAttribute("text", reachedEvent.description);
        }
        
        public void OnCloseNotifPanel() { controller.xmlLayout.GetElementById("ep-notif-panel").SetActive(false); }

        private void AddEventListItem(XmlElement list, int id)
        {
            XmlElement listItem = Instantiate(listItemTemplate);
            XmlElement component = listItem.GetComponent<XmlElement>();

            component.Initialise(list.xmlLayoutInstance, (RectTransform)listItem.transform, listItemTemplate.tagHandler);
            list.AddChildElement(component);
            component.SetAttribute("active", "true");
            component.SetAttribute("id", "event-list-item" + id.ToString());
            component.ApplyAttributes();
        }

        private void RebuildEventListUi(XmlElement list, int count)
        {
            List<XmlElement> elements = new List<XmlElement>(list.childElements);

            foreach (var element in elements)
                list.RemoveChildElement(element, true);

            for (int i = 0; i < count; i++)
                AddEventListItem(list, i);
        }

        public void UpdateEventList(List<EPEvent> events)
        {
            XmlElement list = controller.xmlLayout.GetElementById("event-list");

            if(list.childElements.Count != events.Count)
                RebuildEventListUi(list, events.Count);

            List<XmlElement> elements = new List<XmlElement>(list.childElements);

            double time = Game.Instance.FlightScene.FlightState.Time;

            for (int i = 0; i < events.Count; i++)
            {
                string warningText = events[i].warningBuffer > 0.0f ? ((events[i].state == EPEventState.Waiting ? FormatTime(events[i].time - time - events[i].warningBuffer) : "-") + " | ") : "";
                string timeText = warningText + FormatTime(events[i].time - time);

                elements[i].GetElementByInternalId("label").SetAndApplyAttribute("text", events[i].title);
                elements[i].GetElementByInternalId("value").SetAndApplyAttribute("text", timeText);
            }
        }

        private string FormatTime(double time) { return Units.GetRelativeTimeString(time); }

        private void ShowMessage(string message) { Game.Instance.FlightScene?.FlightSceneUI.ShowMessage(message); }
    }
}
