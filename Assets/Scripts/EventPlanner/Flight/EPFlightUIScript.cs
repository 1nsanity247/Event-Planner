using ModApi.Ui;
using UI.Xml;
using ModApi;
using UnityEngine;
using System.Collections.Generic;
using ModApi.Math;
using System.Linq;

namespace Assets.Scripts
{
    public class EPFlightUIScript : MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel;
        private XmlElement listItemTemplate;

        private bool createEventPanelOpen = false;
        private string newEventTitle, newEventDesc;
        private int[] newEventTimes;
        private readonly float[] timeFactors = { 60.0f, 3600.0f, 86400.0f, 60.0f, 3600.0f, 86400.0f };

        public void OnLayoutRebubuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            mainPanel = controller.xmlLayout.GetElementById("ep-main-panel");

            listItemTemplate = controller.xmlLayout.GetElementById("text-list-item");
        }

        public void OnTogglePanelState()
        {
            mainPanel.SetActive(!mainPanel.isActiveAndEnabled);
        }

        public void AddEventButtonClicked()
        {
            if (!createEventPanelOpen)
            {
                XmlElement panel = controller.xmlLayout.GetElementById("ep-create-event-panel");
                panel.SetActive(true);

                newEventTitle = string.Empty;
                newEventDesc = string.Empty;
                newEventTimes = new int[6];
                createEventPanelOpen = true;

                panel.GetElementByInternalId("title-input").SetAndApplyAttribute("text", "");
                panel.GetElementByInternalId("desc-input").SetAndApplyAttribute("text", "");
                for (int i = 0; i < 6; i++)
                    panel.GetElementByInternalId("time" + i + "-input").SetAndApplyAttribute("text", "");
            }
        }

        public void OnNewEventTitleChanged(string value) { newEventTitle = value; }
        public void OnNewEventDescChanged(string value) { newEventDesc = value; }

        public void OnNewEventDaysChanged(string value) { SetTimeValue(value, 2); }
        public void OnNewEventHoursChanged(string value) { SetTimeValue(value, 1); }
        public void OnNewEventMinutesChanged(string value) { SetTimeValue(value, 0); }
        public void OnNewEventBufferDaysChanged(string value) { SetTimeValue(value, 5); }
        public void OnNewEventBufferHoursChanged(string value) { SetTimeValue(value, 4); }
        public void OnNewEventBufferMinutesChanged(string value) { SetTimeValue(value, 3); }

        private void SetTimeValue(string value, int index) { newEventTimes[index] = int.TryParse(value, out int res) ? res : 0; }

        public void OnCloseCreateEventPanel()
        {
            controller.xmlLayout.GetElementById("ep-create-event-panel").SetActive(false);
            createEventPanelOpen = false;
        }

        public void OnCreateEvent()
        {
            XmlElement panel = controller.xmlLayout.GetElementById("ep-create-event-panel");

            if (newEventTitle == null || newEventTitle.Length == 0) 
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Failed to create Event, invalid title!");
                return;
            }

            if (newEventDesc == null || newEventDesc.Length == 0)
                newEventDesc = "-";

            float time = 0.0f, warningBuffer = 0.0f;
            
            for (int i = 0; i < newEventTimes.Length; i++)
            {
                if (i < 3)
                    time += timeFactors[i] * newEventTimes[i];
                else
                    warningBuffer += timeFactors[i] * newEventTimes[i];
            }

            if (time <= 0.0f || warningBuffer < 0.0f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Failed to create Event, invalid time inputs!");
                return;
            }

            EPManager.Instance.CreateEvent(newEventTitle, newEventDesc, Game.Instance.FlightScene.FlightState.Time + time, warningBuffer);

            panel.SetActive(false);
            createEventPanelOpen = false;
        }

        public void EventElementClicked(int id)
        {
            print(id);
        }

        public void OnCloseNotifPanel()
        {
            controller.xmlLayout.GetElementById("ep-notif-panel").SetActive(false);
        }

        public void ShowNotifPanel(EPEvent reachedEvent)
        {
            XmlElement notifPanel = controller.xmlLayout.GetElementById("ep-notif-panel");
            notifPanel.SetActive(true);
            notifPanel.GetElementByInternalId("ep-notif-title").SetAndApplyAttribute("text", reachedEvent.title);
            notifPanel.GetElementByInternalId("description").SetAndApplyAttribute("text", reachedEvent.description);
        }

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
                string warningText = events[i].warningBuffer > 0.0f ? ((events[i].state == EPEventState.waiting ? Format(events[i].time - time - events[i].warningBuffer) : "-") + " | ") : "";
                string timeText = warningText + Format(events[i].time - time);

                elements[i].GetElementByInternalId("label").SetAndApplyAttribute("text", events[i].title);
                elements[i].GetElementByInternalId("value").SetAndApplyAttribute("text", timeText);
            }
            /*
            for (int i = 0; i < events.Count; i++)
            {
                XmlElement e = controller.xmlLayout.GetElementById("event-panel" + i);
                
                if(i < events.Count)
                {
                    string warningText = events[i].warningBuffer > 0.0f ? ((events[i].state == EPEventState.waiting ? Format(events[i].time - time - events[i].warningBuffer) : "-") + " | ") : "";
                    string timeText = warningText + Format(events[i].time - time);

                    e.SetActive(true);
                    e.GetElementByInternalId("title").SetAndApplyAttribute("text", events[i].title);
                    e.GetElementByInternalId("time").SetAndApplyAttribute("text", timeText);
                }
                else
                    e.SetActive(false);
            }
            */
        }

        private string Format(double time) { return Units.GetRelativeTimeString(time); }
    }
}
