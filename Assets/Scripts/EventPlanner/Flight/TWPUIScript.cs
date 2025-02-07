using ModApi;
using ModApi.Flight.Sim;
using ModApi.Ui;
using MonoMod.RuntimeDetour.Platforms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{  
    public class TWPUIScript : MonoBehaviour
    {
        private XmlLayoutController controller;
        private bool _panelVisible = false;
        private const int _plotRes = 200;
        private Vector2 _plotTimesInDays = new Vector2(400, 400);

        private const double _dayLength = 86400.0;
        private const string _plotPath = "EventPlanner/Sprites/plot";
        private const string _gradientPath = "EventPlanner/Sprites/gradient";

        private IOrbitNode _originNode;
        private List<IOrbitNode> _originOptions;
        private IOrbitNode _destinationNode;
        private List<IOrbitNode> _destinationOptions;
        private Color _noSolutionColor;
        private Gradient _colorGradient;
        private float[] _velocities;
        private Color[] _pixels;
        private float _timeToDepartureOfClickedPixel = 0.0f;
        private float _lastStartFlightDurationInDays = 0.0f;

        private readonly Vector2Int[] _neighbourOffsets = {
            new Vector2Int(-1,-1),new Vector2Int(0,-1),
            new Vector2Int(1,-1),new Vector2Int(-1,0),
            new Vector2Int(1,0),new Vector2Int(-1,1),
            new Vector2Int(0,1),new Vector2Int(1,1),
        };

        private void Awake()
        {
            _originOptions = new List<IOrbitNode>();
            _destinationOptions = new List<IOrbitNode>();

            _noSolutionColor = new Color(0.25f, 0.25f, 0.25f);
            _colorGradient = new Gradient();
            GradientColorKey[] colors =
            {
                new GradientColorKey(new Color(0.0f, 0.0f, 1.0f), 0.0f),
                new GradientColorKey(new Color(64 / 255.0f, 224 / 255.0f, 208 / 255.0f), 0.33f),
                new GradientColorKey(new Color(255 / 255.0f, 140 / 255.0f, 0), 0.66f),
                new GradientColorKey(new Color(255 / 255.0f, 0, 128 / 255.0f), 1.0f)
            };
            _colorGradient.colorKeys = colors;
            _colorGradient.mode = GradientMode.PerceptualBlend;

            _velocities = new float[_plotRes * _plotRes];
            _pixels = new Color[_plotRes * _plotRes];
        }

        public void OnLayoutRebuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            controller.xmlLayout.GetElementById("plot-time-input").SetAndApplyAttribute("text", FormatNumberDynamic(_plotTimesInDays.x));

            UpdateGradient();

            InitializeDropdowns();
            UpdateOriginDropdownOptions();
            OnOriginDropDownChanged(1);
            var dropDown = controller.xmlLayout.GetElementById("origin-dropdown").gameObject.GetComponentInChildren<TMP_Dropdown>();
            dropDown.SetValueWithoutNotify(1);
        }

        private void UpdateGradient() 
        {
            int gradientImageRes = 32;
            var gradientPixels = new Color[gradientImageRes];
            for (int i = 0; i < gradientImageRes; i++) {
                gradientPixels[i] = _colorGradient.Evaluate((float)i / (gradientImageRes - 1));
            }

            Texture2D gradientImage = new Texture2D(1, gradientImageRes, TextureFormat.RGBA32, false);
            gradientImage.wrapMode = TextureWrapMode.Clamp;
            gradientImage.SetPixels(gradientPixels);
            gradientImage.Apply();

            Sprite gradientSprite = Sprite.Create(gradientImage, new Rect(Vector2.zero, new Vector2(1, gradientImageRes)), Vector2.zero);
            XmlLayoutResourceDatabase.instance.AddResource(_gradientPath, gradientSprite);
            controller.xmlLayout.GetElementById("gradient-image").SetAndApplyAttribute("sprite", _gradientPath);
        }

        void OnEditGradient() 
        {
            Game.Instance.UserInterface.CreateGradientEditor(_colorGradient, (s) => {
                _colorGradient = s;
                UpdateGradient();
            }, false, false);
        }

        void InitializeDropdowns()
        {
            var originDropdown = controller.xmlLayout.GetElementById("origin-dropdown").gameObject.GetComponentInChildren<TMP_Dropdown>();
            originDropdown.transform.Find("Template").GetComponent<ScrollRect>().scrollSensitivity = 20f;
            originDropdown.onValueChanged.AddListener(OnOriginDropDownChanged);
            
            var destinationDropdown = controller.xmlLayout.GetElementById("destination-dropdown").gameObject.GetComponentInChildren<TMP_Dropdown>();
            destinationDropdown.transform.Find("Template").GetComponent<ScrollRect>().scrollSensitivity = 20f;
            destinationDropdown.onValueChanged.AddListener((int o) => _destinationNode = _destinationOptions[o]);
        }

        void OnOriginDropDownChanged(int o)
        {
            _originNode = _originOptions[o];
            UpdateDestinationDropdownOptions(_originNode.Parent);
        }

        void UpdateOriginDropdownOptions()
        {
            var dropDown = controller.xmlLayout.GetElementById("origin-dropdown").gameObject.GetComponentInChildren<TMP_Dropdown>();

            var mainPlanets = Game.Instance.FlightScene.FlightState.RootNode.ChildPlanets.ToList<IPlanetNode>();
            _originOptions.Clear();
            _originOptions.Add(Game.Instance.FlightScene.CraftNode.OrbitNode);
            foreach (var planet in mainPlanets) {
                _originOptions.Add(planet);
                _originOptions.AddRange(planet.ChildPlanets);
            }

            List<string> options = new List<string>(_originOptions.Count);
            options.Add("This Craft");
            for (int i = 1; i < _originOptions.Count; i++) {
                options.Add(_originOptions[i].Name);
            }

            dropDown.ClearOptions();
            dropDown.AddOptions(options);
        }

        void UpdateDestinationDropdownOptions(IPlanetNode originParent)
        {
            if (originParent == null) {
                return;
            }
            
            var dropDown = controller.xmlLayout.GetElementById("destination-dropdown").gameObject.GetComponentInChildren<TMP_Dropdown>();

            _destinationOptions = originParent.ChildPlanets.ToList<IOrbitNode>();
            _destinationOptions.Remove(_originNode);
            
            List<string> options = new List<string>(_destinationOptions.Count);
            foreach (var node in _destinationOptions) {
                options.Add(node.Name);
            }

            dropDown.ClearOptions();
            dropDown.AddOptions(options);
            dropDown.value = 0;
            
            _destinationNode = _destinationOptions.Count > 0 ? _destinationOptions[0] : null;
        }

        void UpdatePlot()
        {
            if (_originNode == null || _destinationNode == null || _originNode == _destinationNode) {
                return;
            }

            var parentBody = _originNode.Parent;
            
            double mu = Constants.GravitationConstant * parentBody.PlanetData.Mass;
            double startDepartureTime = _originNode.Orbit.Time;
            float averageFlightDurationInDays = (float)(0.25 * (_originNode.Orbit.Period + _destinationNode.Orbit.Period) / _dayLength);
            _lastStartFlightDurationInDays = Mathf.Max(0.25f * averageFlightDurationInDays, (float)averageFlightDurationInDays - 0.5f * _plotTimesInDays.y);
            Vector2 pixelTimeInc = _plotTimesInDays / _plotRes;

            float minVel = float.MaxValue;
            float maxVel = 0.0f;

            var watch = new Stopwatch();
            watch.Start();

            for (int y = 0; y < _plotRes; y++) {
                for (int x = 0; x < _plotRes; x++) {
                    double departureTime = startDepartureTime + pixelTimeInc.x * _dayLength * x;
                    double arrivalTime = departureTime + (_lastStartFlightDurationInDays + pixelTimeInc.y * y) * _dayLength;

                    var departurePoint = _originNode.GetPointAtTime(departureTime);
                    var arrivalPoint = _destinationNode.GetPointAtTime(arrivalTime);

                    var mainResult = LambertSolver.LambertUV(departurePoint.Position, arrivalPoint.Position, arrivalTime - departureTime, 1, mu);
                    var altResult = LambertSolver.LambertUV(departurePoint.Position, arrivalPoint.Position, arrivalTime - departureTime, -1, mu);

                    float mainVel = (float)(mainResult.departureVelocity - departurePoint.Velocity).magnitude;
                    float altVel = (float)(altResult.departureVelocity - departurePoint.Velocity).magnitude;

                    float vel = float.MaxValue;

                    // this is pretty bad logic but it works
                    if (mainResult.solved && altResult.solved) {
                        vel = Mathf.Min(mainVel, altVel);
                    }
                    else if (mainResult.solved || altResult.solved) {
                        vel = mainResult.solved ? mainVel : altVel;
                    }
                    
                    if(mainResult.solved && altResult.solved) {
                        minVel = Mathf.Min(minVel, vel);
                        maxVel = Mathf.Max(maxVel, vel);
                    }

                    _velocities[x + _plotRes * y] = vel;
                }
            }

            watch.Stop();
            print("TWP plot time: " + watch.Elapsed.TotalMilliseconds);

            for (int y = 0; y < _plotRes; y++)
            {
                for (int x = 0; x < _plotRes; x++)
                {
                    float vel = _velocities[x + _plotRes * y];

                    if(vel > maxVel) {
                        float sum = 0.0f;
                        int count = 0;
                        int r = 2;
                        for (int dy = -r; dy <= r; dy++) {
                            for (int dx = -r; dx <= r; dx++) {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (Mathf.Min(nx, ny) >= 0 && Mathf.Max(nx, ny) < _plotRes) {
                                    float nValue = _velocities[nx + _plotRes * ny];
                                    if (nValue < maxVel) {
                                        sum += nValue;
                                        count++;
                                    }
                                }
                            }
                        }

                        foreach (var neighbour in _neighbourOffsets) {
                            int nx = x + neighbour.x;
                            int ny = y + neighbour.y;
                            if(Mathf.Min(nx,ny) >= 0 && Mathf.Max(nx,ny) < _plotRes) {
                                float nValue = _velocities[nx + _plotRes * ny];
                                if(nValue < maxVel) {
                                    sum += nValue;
                                    count++;
                                }
                            }
                        }
                        if(count > 0) {
                            vel = sum / count;
                        }
                    }

                    float value = Mathf.Sqrt(Mathf.InverseLerp(minVel, maxVel, vel));

                    _pixels[x + _plotRes * y] = vel > maxVel ? _noSolutionColor : _colorGradient.Evaluate(value);
                }
            }

            Texture2D plotImage = new Texture2D(_plotRes, _plotRes, TextureFormat.RGBA32, false);
            plotImage.wrapMode = TextureWrapMode.Clamp;
            plotImage.SetPixels(_pixels);
            plotImage.Apply();

            Sprite plotSprite = Sprite.Create(plotImage, new Rect(Vector2.zero, _plotRes * Vector2.one), Vector2.zero);
            XmlLayoutResourceDatabase.instance.AddResource(_plotPath, plotSprite);

            controller.xmlLayout.GetElementById("plot-dep-max").SetAndApplyAttribute("text", FormatNumberDynamic(_plotTimesInDays.x));
            controller.xmlLayout.GetElementById("plot-arr-min").SetAndApplyAttribute("text", FormatNumberDynamic(_lastStartFlightDurationInDays));
            controller.xmlLayout.GetElementById("plot-arr-max").SetAndApplyAttribute("text", FormatNumberDynamic(_lastStartFlightDurationInDays + _plotTimesInDays.y));

            controller.xmlLayout.GetElementById("gradient-vel-min").SetAndApplyAttribute("text", minVel.ToString("n0") + "m/s");
            controller.xmlLayout.GetElementById("gradient-vel-max").SetAndApplyAttribute("text", maxVel.ToString("n0") + "m/s");

            var plotElement = controller.xmlLayout.GetElementById("plot-image");
            plotElement.SetAndApplyAttribute("width", "500");
            plotElement.SetAndApplyAttribute("height", "500");
            plotElement.SetAndApplyAttribute("sprite", _plotPath);

            controller.xmlLayout.GetElementById("plot-context-menu").SetActive(false);
        }

        void OnPlotClicked() {
            Vector3 mousePos = UnityEngine.Input.mousePosition;

            var plotElement = controller.xmlLayout.GetElementById("plot-image");

            Vector3 localMousePos = plotElement.rectTransform.InverseTransformPoint(mousePos);
            Vector2 plotSize = plotElement.rectTransform.rect.size;

            Vector2 localNormPos = new Vector2(0.5f + localMousePos.x / plotSize.x, 0.5f + localMousePos.y / plotSize.y);

            Vector2 times = Vector2.Scale(localNormPos, _plotTimesInDays);
            float dv = 0.0f;

            Vector2Int pixelPos = Vector2Int.RoundToInt(localNormPos * _plotRes);
            if(pixelPos.x >= 0 && pixelPos.y >= 0 && pixelPos.x < _plotRes && pixelPos.y < _plotRes) {
                dv = _velocities[pixelPos.y * _plotRes + pixelPos.x];
            }

            float uiScale = Game.Instance.Settings.Game.General.UserInterfaceScale;

            Vector3 menuOffset = new Vector3();
            menuOffset.x = localNormPos.x < 0.5f ? 20.0f : -240.0f;
            menuOffset.y = localNormPos.y < 0.5f ? 20.0f : -150.0f;
            
            var contextMenuPanel = controller.xmlLayout.GetElementById("plot-context-menu");
            contextMenuPanel.SetActive(true);
            contextMenuPanel.rectTransform.position = mousePos + uiScale * menuOffset;
            contextMenuPanel.GetElementByInternalId("circle").transform.position = mousePos - uiScale * new Vector3(5.0f, 5.0f, 0.0f);
            contextMenuPanel.GetElementByInternalId("departure-time-text").SetAndApplyAttribute("text", FormatNumberDynamic(times.x) + "d");
            contextMenuPanel.GetElementByInternalId("flight-duration-text").SetAndApplyAttribute("text", FormatNumberDynamic(_lastStartFlightDurationInDays + times.y) + "d");
            contextMenuPanel.GetElementByInternalId("dv-text").SetAndApplyAttribute("text", dv.ToString("n0") + "m/s");

            _timeToDepartureOfClickedPixel = times.x * (float)_dayLength;
        }

        void CreateEventFromContextMenu() {
            var epUIScript = EPManager.Instance.EPUIScript;

            if(epUIScript != null && _originNode != null && _destinationNode != null) {
                epUIScript.ShowCreateEventPanel(false);
                epUIScript.FillEventDataForNewEventCreation("Transfer " + _originNode.Name + " to " + _destinationNode.Name, _timeToDepartureOfClickedPixel);
            }
        }

        public void OnTogglePanelState() {
            _panelVisible = !_panelVisible;
            controller.xmlLayout.GetElementById("twp-main-panel").SetActive(_panelVisible);
        }

        private void OnCloseContextMenu() {
            controller.xmlLayout.GetElementById("plot-context-menu").SetActive(false);
        }

        void OnChangePlotTime(float amount) {
            float newTime = _plotTimesInDays.x + amount;
            _plotTimesInDays = Mathf.Max(1.0f, Mathf.Round(newTime / amount) * amount) * Vector2.one;
            controller.xmlLayout.GetElementById("plot-time-input").SetAndApplyAttribute("text", FormatNumberDynamic(_plotTimesInDays.x));
        }

        void OnSetPlotTime(string value) {
            if (float.TryParse(value, out float amount)) {
                _plotTimesInDays = Mathf.Max(1e-3f, amount) * Vector2.one;
                controller.xmlLayout.GetElementById("plot-time-input").SetAndApplyAttribute("text", FormatNumberDynamic(_plotTimesInDays.x));
            }
        }

        private string FormatNumberDynamic(float num) {
            int orderOfMagnitude = Mathf.FloorToInt(Mathf.Log10(num));
            return num.ToString("n" + Mathf.Max(0, -orderOfMagnitude + 3));
        }

        public void SetUIVisibility(bool state) {
            controller.xmlLayout.GetElementById("twp-main-panel").SetActive(state && _panelVisible);
        }
    }
}
