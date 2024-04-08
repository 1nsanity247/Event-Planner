using System.IO;
using ModApi.Math;
using UnityEngine;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Scenes.Events;
using System.Xml.Serialization;
using System.Collections.Generic;
using Assets.Packages.DevConsole;
using HarmonyLib;
using Assets.Scripts.Flight;
using ModApi.Ui;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Flight.UI;
using UI.Xml;

namespace Assets.Scripts
{
    public class EPManager : MonoBehaviour
    {
        public static EPManager Instance { get; private set; }
        public string EPFilePath { get; private set; }

        private EPFlightUIScript _UIScript;
        private List<EventData> _events;
        private XmlSerializer _xmlSerializer;
        private EventDatabase _eventDB;
        private bool _loadTempData = false;

        public static XmlElement eventPanelButton;
        public const string defaultIconPath = "Ui/Sprites/Common/IconButtonSquare";
        public const string sillyIconPath = "EventPlanner/Sprites/alert";
        public const string silliestIconPath = "EventPlanner/Sprites/colon3";
        public const string eventPanelButtonId = "event-panel-nav-button";

        public const int EventXmlVersion = 1;

        private void Awake()
        {
            Instance = this;
            _events = new List<EventData>();
            _xmlSerializer = new XmlSerializer(typeof(EventDatabase));

            EPFilePath = Application.persistentDataPath + "/UserData/EventPlanner/";
            Directory.CreateDirectory(EPFilePath);
        }

        private void Start()
        {
            DevConsoleApi.RegisterCommand("ClearEventXml", () =>
            {
                _eventDB = null;
                SaveEventXml();
            });
            
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel, OnBuildNavPanelUI);

            if (!File.Exists(EPFilePath + "EventData.xml"))
                SaveEventXml();
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                _UIScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<EPFlightUIScript>(
                    "EventPlanner/Flight/EventPlannerPanel",
                    (script, controller) => script.OnLayoutRebuilt(controller));

                if (!_loadTempData)
                    LoadEventXml();
                
                _loadTempData = false;

                LoadEventsFromDatabase();

                Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
            }
        }

        public static void OnBuildNavPanelUI(BuildUserInterfaceXmlRequest request)
        {
            var nameSpace = XmlLayoutConstants.XmlNamespace;
            var translationButton = request.XmlDocument
                .Descendants(nameSpace + "ContentButton")
                .First(x => (string)x.Attribute("id") == "nav-sphere-translation");
            
            string iconPath = ModSettings.Instance.Silliness.Value switch
            {
                ModSettings.SillinessLevel.Default => defaultIconPath,
                ModSettings.SillinessLevel.Silly => sillyIconPath,
                ModSettings.SillinessLevel.Silliest => silliestIconPath,
                _ => defaultIconPath,
            };
            
            translationButton.Parent.Add(
                new XElement(
                    nameSpace + "ContentButton",
                    new XAttribute("id", eventPanelButtonId),
                    new XAttribute("class", "panel-button toggle-button-toggled audio-btn-click"),
                    new XAttribute("tooltip", "Event Panel"),
                    new XAttribute("name", "NavPanel.EventPanelButton"),
                    new XElement(
                        nameSpace + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", iconPath))));

            //request.AddOnLayoutRebuiltAction(xmlLayoutController =>
            //{
            //    button = (XmlElement)xmlLayoutController.XmlLayout.GetElementById(buttonId);
            //    button.AddOnClickEvent(OnTogglePanelState);
            //});
        }

        public void OnTogglePanelState()
        {
            if (_UIScript == null)
                return;

            _UIScript.OnTogglePanelState();
        }

        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            switch (e.ExitReason)
            {
                case FlightSceneExitReason.SaveAndDestroy:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.SaveAndRecover:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.SaveAndExit:
                    SaveEventsToDatabase();
                    SaveEventXml();
                    _loadTempData = false;
                    break;
                case FlightSceneExitReason.CraftNodeChanged:
                    SaveEventsToDatabase();
                    _loadTempData = true;
                    break;
                case FlightSceneExitReason.QuickLoad:
                    _loadTempData = true;
                    break;
                default:
                    break;
            }
        }

        public void OnQuickSave()
        {
            SaveEventsToDatabase();
            _loadTempData = true;
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
            _eventDB ??= new EventDatabase { xmlVersion = EventXmlVersion };

            if (_eventDB.xmlVersion != EventXmlVersion)
                Debug.Log("Mismatched event xml version, surely it'll be fine :clueless:");
        }

        private void SaveEventsToDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;
            EventGameState eventList = _eventDB.lists.Find((EventGameState state) => { return state.gameStateId == currentGameStateId; });

            if (eventList == null)
            {
                eventList = new EventGameState();
                eventList.gameStateId = currentGameStateId;
                _eventDB.lists.Add(eventList);
            }

            eventList.events.Clear();

            foreach (var item in _events)
            {
                eventList.events.Add(new EventData
                {
                    title = item.title,
                    description = item.description,
                    time = item.time,
                    warningBuffer = item.warningBuffer,
                    state = item.state
                });
            }
        }

        private void LoadEventsFromDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;
            EventGameState eventList = _eventDB.lists.Find((EventGameState state) => { return state.gameStateId == currentGameStateId; });

            if (eventList == null) return;

            _events.Clear();

            foreach (var item in eventList.events)
            {
                _events.Add(new EventData
                {
                    title = item.title,
                    description = item.description,
                    time = item.time,
                    warningBuffer = item.warningBuffer,
                    state = item.state
                });
            }
        }

        public void CreateEvent(string title, string description, double time, double buffer)
        {
            if (!Game.Instance.SceneManager.InFlightScene || time < Game.Instance.GameState.GetCurrentTime()) return;

            EventData newEvent = new EventData
            {
                title = title,
                description = description,
                time = time,
                warningBuffer = buffer
            };

            _events.Add(newEvent);
            _events.Sort((EventData a, EventData b) => a.time.CompareTo(b.time));

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Event " + newEvent.title + " has been planned. It will be reached in: " + Units.GetRelativeTimeString(newEvent.time - Game.Instance.FlightScene.FlightState.Time));
        }

        public void RemoveEvent(string title)
        {
            RemoveEvent(_events.FindIndex((EventData e) => e.title == title));
        }

        public void RemoveEvent(int id)
        {
            if (id < 0 || id > _events.Count)
                return;

            _events.RemoveAt(id);
        }

        public void WarpToNextEvent()
        {
            if (_events.Count == 0)
                return;

            const double desiredWarpTime = 5.0;

            IFlightScene fs = Game.Instance.FlightScene;
            double timeToNextEvent = _events[0].time - fs.FlightState.Time;

            foreach (var timeMode in fs.TimeManager.Modes)
            {
                if (timeToNextEvent / timeMode.TimeMultiplier < desiredWarpTime || timeMode == fs.TimeManager.Modes.Last())
                {
                    Game.Instance.FlightScene.TimeManager.SetMode(timeMode, true);
                    break;
                }
            }
        }

        private void Update()
        {   
            if (!Game.Instance.SceneManager.InFlightScene || _UIScript == null) return;

            _UIScript.SetUIVisibility(Game.Instance.FlightScene.FlightSceneUI.Visible);
            _UIScript.UpdateEventList(_events);

            if (_events.Count == 0) return;
            
            IFlightScene fs = Game.Instance.FlightScene;
            double gameTime = fs.FlightState.Time;

            foreach (var epEvent in _events)
            {
                if (epEvent.state == EPEventState.Waiting && epEvent.warningBuffer > 0.0f)
                {
                    if (epEvent.time - epEvent.warningBuffer < gameTime + fs.TimeManager.DeltaTime)
                    {
                        fs.TimeManager.RequestPauseChange(true, false);
                        fs.FlightSceneUI.ShowMessage("Event " + epEvent.title + " due in: " + Units.GetRelativeTimeString(epEvent.time - gameTime));

                        epEvent.state = EPEventState.WarningIssued;
                    }
                }
                else
                {
                    if (fs.TimeManager.CurrentMode.TimeMultiplier > fs.TimeManager.RealTime.TimeMultiplier)
                    {
                        if (epEvent.time < gameTime + 10.0 * fs.TimeManager.DeltaTime)
                            fs.TimeManager.DecreaseTimeMultiplier();
                    }
                    else if (epEvent.time < gameTime + fs.TimeManager.DeltaTime)
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

    public enum EPEventState { Waiting, WarningIssued, Reached }

    public class EventDatabase
    {
        [XmlAttribute]
        public int xmlVersion;
        [XmlArray("GameStates")]
        public List<EventGameState> lists = new List<EventGameState>();
    }

    public class EventGameState
    {
        [XmlAttribute]
        public string gameStateId;
        [XmlArray("Events")]
        public List<EventData> events = new List<EventData>();
    }

    public class EventData
    {
        [XmlAttribute]
        public string title;
        [XmlAttribute]
        public string description;
        [XmlAttribute]
        public double time;
        [XmlAttribute]
        public double warningBuffer;
        [XmlAttribute]
        public EPEventState state;
    }

    [HarmonyPatch(typeof(FlightSceneScript), "QuickSave")]
    class QuickSave_Patch
    {
        static bool Prefix()
        {
            EPManager.Instance?.OnQuickSave();
            return true;
        }
    }

    [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
    class LayoutRebuilt_Patch
    {
        static bool Prefix(NavPanelController __instance)
        {
            EPManager.eventPanelButton = __instance.xmlLayout.GetElementById(EPManager.eventPanelButtonId);
            EPManager.eventPanelButton.AddOnClickEvent(EPManager.Instance.OnTogglePanelState);

            return true;
        }
    }
}