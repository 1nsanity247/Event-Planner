using ModApi;
using ModApi.Flight;
using ModApi.Flight.Sim;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Math;
using ModApi.Scenes.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class EPEvent
    {
        public string title, description;
        public double time, warningBuffer;
        public bool warningIssued = false, reached = false;
    }

    public class EPManager : MonoBehaviour
    {
        public static EPManager Instance { get; private set; }

        private EPFlightUIScript _UIScript;
        private List<EPEvent> _events;

        private void Awake()
        {
            Instance = this;
            _events = new List<EPEvent>();
        }

        public void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                _UIScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<EPFlightUIScript>(
                    "EventPlanner/Flight/EventPlannerPanel",
                    (script, controller) => script.OnLayoutRebubuilt(controller));
            }
        }

        public void CreateEvent(string title, string description, double time, double buffer)
        {
            if (!Game.Instance.SceneManager.InFlightScene)
            {
                Debug.LogWarning("Failed to add event, currently not in Flight Scene!");
                return;
            }
            if(time < Game.Instance.GameState.GetCurrentTime())
            {
                Debug.LogWarning("Failed to add event, we don't got no time machine man!");
                return;
            }

            EPEvent newEvent = new EPEvent
            {
                title = title,
                description = description,
                time = time,
                warningBuffer = buffer
            };

            _events.Add(newEvent);

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Event " + newEvent.title + " has been planned. It will be reached in: " + Units.GetRelativeTimeString(newEvent.time - Game.Instance.FlightScene.FlightState.Time));
        }

        public void Update()
        {
            if (!Game.Instance.SceneManager.InFlightScene || _events.Count == 0) return;
            
            IFlightScene fs = Game.Instance.FlightScene;
            double gameTime = fs.FlightState.Time;

            foreach (var epEvent in _events)
            {       
                if(epEvent.time - epEvent.warningBuffer < gameTime + fs.TimeManager.DeltaTime)
                {
                    if (!epEvent.warningIssued)
                    {
                        fs.TimeManager.RequestPauseChange(true, false);
                        fs.FlightSceneUI.ShowMessage("Event " + epEvent.title + " due in: " + Units.GetRelativeTimeString(epEvent.time - gameTime));

                        epEvent.warningIssued = true;
                    }
                    else if(epEvent.time < gameTime + fs.TimeManager.DeltaTime)
                    {
                        fs.TimeManager.RequestPauseChange(true, false);
                        fs.FlightSceneUI.ShowMessage("Event " + epEvent.title + " has been reached");

                        epEvent.reached = true;
                    }
                }
            }

            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].reached)
                {
                    _events.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}