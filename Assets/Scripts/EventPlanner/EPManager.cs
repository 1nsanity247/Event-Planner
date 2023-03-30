using System.IO;
using ModApi.Math;
using UnityEngine;
using ModApi.Flight;
using ModApi.Scenes.Events;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public enum EPEventState { Waiting, WarningIssued, Reached }

    public class EPEvent
    {
        public string title, description;
        public double time, warningBuffer;
        public EPEventState state = EPEventState.Waiting;
    }

    public class EPManager : MonoBehaviour
    {
        public static EPManager Instance { get; private set; }
        public string EPFilePath { get; private set; }

        private EPFlightUIScript _UIScript;
        private List<EPEvent> _events;
        private XmlSerializer _xmlSerializer;
        private EventDatabase _eventDB;

        private void Awake()
        {
            Instance = this;
            _events = new List<EPEvent>();
            _xmlSerializer = new XmlSerializer(typeof(EventDatabase));

            EPFilePath = Application.persistentDataPath + "/UserData/EventPlanner/";
            Directory.CreateDirectory(EPFilePath);
        }

        private void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneTransitionStarted += OnScenceTransitionStarted;

            if (!File.Exists(EPFilePath + "EventData.xml"))
                SaveEventXml();
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                _UIScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<EPFlightUIScript>(
                    "EventPlanner/Flight/EventPlannerPanel",
                    (script, controller) => script.OnLayoutRebubuilt(controller));

                LoadEventXml();
                LoadEventsFromDatabase();
            }
        }
        
        private void OnScenceTransitionStarted(object sender, SceneTransitionEventArgs e)
        {
            if(e.TransitionFromScene == "Flight")
            {
                SaveEventsToDatabase();
                SaveEventXml();
            }
        }

        private void SaveEventXml()
        {
            FileStream stream = new FileStream(EPFilePath + "EventData.xml", FileMode.Create);
            _xmlSerializer.Serialize(stream, _eventDB);
            stream.Close();
        }

        private void LoadEventXml()
        {
            FileStream stream = new FileStream(EPFilePath + "EventData.xml", FileMode.Open);
            _eventDB = _xmlSerializer.Deserialize(stream) as EventDatabase;
            _eventDB ??= new EventDatabase();
        }

        private void SaveEventsToDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;
            EventDataList eventList = null;

            foreach (var list in _eventDB.lists)
            {
                if (list.GameStateId == currentGameStateId)
                    eventList = list;
            }

            if (eventList == null)
            {
                eventList = new EventDataList();
                eventList.GameStateId = currentGameStateId;
                _eventDB.lists.Add(eventList);
            }

            eventList.events.Clear();

            foreach (var item in _events)
            {
                eventList.events.Add(new EventData
                {
                    Title = item.title,
                    Description = item.description,
                    Time = item.time,
                    WarningBuffer = item.warningBuffer,
                    State = item.state
                });
            }
        }

        private void LoadEventsFromDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;

            _events.Clear();

            foreach (var list in _eventDB.lists)
            {
                if (list.GameStateId == currentGameStateId)
                {
                    foreach (var item in list.events)
                    {
                        _events.Add(new EPEvent
                        {
                            title = item.Title,
                            description = item.Description,
                            time = item.Time,
                            warningBuffer = item.WarningBuffer,
                            state = item.State
                        });
                    }
                }
            }
        }

        public void CreateEvent(string title, string description, double time, double buffer)
        {
            if (!Game.Instance.SceneManager.InFlightScene || time < Game.Instance.GameState.GetCurrentTime()) return;

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

        private void Update()
        {   
            if (!Game.Instance.SceneManager.InFlightScene || _UIScript == null) return;

            _UIScript.UpdateEventList(_events);

            if(_events.Count > 0)
            {
                IFlightScene fs = Game.Instance.FlightScene;
                double gameTime = fs.FlightState.Time;

                foreach (var epEvent in _events)
                {       
                    if(epEvent.time - epEvent.warningBuffer < gameTime + fs.TimeManager.DeltaTime)
                    {
                        if (epEvent.state == EPEventState.Waiting && epEvent.warningBuffer > 0.0f)
                        {
                            fs.TimeManager.RequestPauseChange(true, false);
                            fs.FlightSceneUI.ShowMessage("Event " + epEvent.title + " due in: " + Units.GetRelativeTimeString(epEvent.time - gameTime));

                            epEvent.state = EPEventState.WarningIssued;
                        }
                        else if(epEvent.time < gameTime + fs.TimeManager.DeltaTime)
                        {
                            fs.TimeManager.RequestPauseChange(true, false);
                            fs.FlightSceneUI.ShowMessage("Event " + epEvent.title + " has been reached");

                            epEvent.state = EPEventState.Reached;
                        }
                    }
                }

                for (int i = 0; i < _events.Count; i++)
                {
                    if (_events[i].state == EPEventState.Reached)
                    {
                        _UIScript.ShowNotifPanel(_events[i]);
                        _events.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }

    public class EventDatabase
    {
        [XmlArray("GameStates")]
        public List<EventDataList> lists = new List<EventDataList>();
    }

    public class EventDataList
    {
        [XmlAttribute]
        public string GameStateId;
        [XmlArray("Events")]
        public List<EventData> events = new List<EventData>();
    }

    public class EventData
    {
        [XmlAttribute]
        public string Title;
        [XmlAttribute]
        public string Description;
        [XmlAttribute]
        public double Time;
        [XmlAttribute]
        public double WarningBuffer;
        [XmlAttribute]
        public EPEventState State;
    }
}