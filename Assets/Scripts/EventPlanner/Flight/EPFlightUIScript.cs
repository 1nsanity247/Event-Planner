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
        private XmlLayoutController _controller;
        private XmlElement _mainPanel;

        public int MaxEvents { get; private set; } = 8;
        private bool createEventPanelOpen = false;
        private string newEventTitle, newEventDesc;
        private int[] newEventTimes;
        private readonly float[] timeFactors = { 60.0f, 3600.0f, 86400.0f, 60.0f, 3600.0f, 86400.0f };

        public void OnLayoutRebubuilt(IXmlLayoutController controller)
        {
            _controller = (XmlLayoutController)controller;
            _mainPanel = _controller.xmlLayout.GetElementById("ep-main-panel");
        }

        public void OnTogglePanelState()
        {
            _mainPanel.SetActive(!_mainPanel.isActiveAndEnabled);
        }

        public void AddEventButtonClicked()
        {
            if (!createEventPanelOpen)
            {
                XmlElement panel = _controller.xmlLayout.GetElementById("ep-create-event-panel");
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
            _controller.xmlLayout.GetElementById("ep-create-event-panel").SetActive(false);
            createEventPanelOpen = false;
        }

        public void OnCreateEvent()
        {
            XmlElement panel = _controller.xmlLayout.GetElementById("ep-create-event-panel");

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
            _controller.xmlLayout.GetElementById("ep-notif-panel").SetActive(false);
        }

        public void ShowNotifPanel(EPEvent reachedEvent)
        {
            XmlElement notifPanel = _controller.xmlLayout.GetElementById("ep-notif-panel");
            notifPanel.SetActive(true);
            notifPanel.GetElementByInternalId("ep-notif-title").SetAndApplyAttribute("text", reachedEvent.title);
            notifPanel.GetElementByInternalId("description").SetAndApplyAttribute("text", reachedEvent.description);
        }

        public void UpdateEventList(List<EPEvent> events)
        {
            _controller.xmlLayout.GetElementById("event-list").SetAndApplyAttribute("height", (35 * Mathf.Min(MaxEvents, events.Count)).ToString());
            _controller.xmlLayout.GetElementById("add-event-button").SetAndApplyAttribute("offsetXY", "0 " + (-35.0 * (events.Count + 1)));
            _controller.xmlLayout.GetElementById("add-event-button").SetActive(events.Count < MaxEvents);

            double time = Game.Instance.FlightScene.FlightState.Time;

            for (int i = 0; i < MaxEvents; i++)
            {
                XmlElement e = _controller.xmlLayout.GetElementById("event-panel" + i);
                
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
        }

        private string Format(double time) { return Units.GetRelativeTimeString(time); }
    }
}
