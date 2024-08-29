// Copyright 2021 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGIS.Helpers;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace ArcGIS.WPF.Samples.FeatureLayerPerformance3D
{
    [ArcGIS.Samples.Shared.Attributes.Sample(
        name: "Feature layer performance 3D",
        category: "Scene",
        description: "Render features in a scene statically or dynamically by checking the feature layer rendering performance.",
        instructions: "Interact by zooming or panning on one view. The other view will automatically focus on the same viewpoint..",
        tags: new[] { "3D", "feature layer", "rendering", "performance", "dynamic", "static" })]
    public partial class FeatureLayerPerformance3D
    {
        // URI for the feature service.
        private string _featureService = "https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/World_Countries/FeatureServer/0";

        public FeatureLayerPerformance3D()
        {
            InitializeComponent();

            Initialize();

            UrlTextBox.Text = _featureService;
        }

        private void Initialize()
        {
            // Call a function to set up the AuthenticationManager for OAuth.
            ArcGISLoginPrompt.SetChallengeHandler();

            // Initialize the sample.
            InitializeSingleView();
            InitializeComparedView();

            // Start monitoring the performance
            StartMonitoring();
        }

        private void InitializeSingleView()
        {
            MySceneView.Scene = new Scene();
        }

        #region Comapred view
        private void InitializeComparedView()
        {
            // Disable 'flick' gesture
            // This is the most straightforward way to prevent the 'flick' animation on one view from competing with user interaction on the other
            MyStaticSceneView.InteractionOptions = new SceneViewInteractionOptions { IsFlickEnabled = false };
            MyDynamicSceneView.InteractionOptions = new SceneViewInteractionOptions { IsFlickEnabled = false };

            // Subscribe to viewpoint change events for both views - event raised on click+drag
            MyStaticSceneView.ViewpointChanged += OnViewpointChanged;
            MyDynamicSceneView.ViewpointChanged += OnViewpointChanged;

            // Subscribe to the navigation completed events - raised on flick
            MyStaticSceneView.NavigationCompleted += OnNavigationComplete;
            MyDynamicSceneView.NavigationCompleted += OnNavigationComplete;

            // Create the scene for displaying the feature layer in static mode.
            Scene staticScene = new Scene();

            // Create the scene for displaying the feature layer in dynamic mode.
            Scene dynamicScene = new Scene();

            // Add the scenes to the scene views.
            MyStaticSceneView.Scene = staticScene;
            MyDynamicSceneView.Scene = dynamicScene;
        }

        private void OnNavigationComplete(object sender, EventArgs eventArgs)
        {
            // Get a reference to the MapView or SceneView that raised the event
            GeoView sendingView = (GeoView)sender;

            // Get a reference to the other view
            GeoView otherView;
            if (sendingView == MyStaticSceneView)
            {
                otherView = MyDynamicSceneView;
            }
            else
            {
                otherView = MyStaticSceneView;
            }

            // Update the viewpoint on the other view
            otherView.SetViewpoint(sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale));
        }

        private void OnViewpointChanged(object sender, EventArgs e)
        {
            // Get the MapView or SceneView that sent the event
            GeoView sendingView = (GeoView)sender;

            // Only take action if this geoview is the one that the user is navigating.
            // Viewpoint changed events are fired when SetViewpoint is called; This check prevents a feedback loop
            if (sendingView.IsNavigating)
            {
                // If the MyStaticSceneView sent the event, update the MyDynamicSceneView's viewpoint
                if (sendingView == MyStaticSceneView)
                {
                    // Get the viewpoint
                    Viewpoint updateViewpoint = MyStaticSceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                    // Set the viewpoint
                    MyDynamicSceneView.SetViewpoint(updateViewpoint);
                }
                else // Else, update the MyStaticSceneView's viewpoint
                {
                    // Get the viewpoint
                    Viewpoint updateViewpoint = MyDynamicSceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                    // Set the viewpoint
                    MyStaticSceneView.SetViewpoint(updateViewpoint);
                }
            }
        }
        #endregion Comapred view

        #region Performance monitoring
        private Timer _timer;
        private void StartMonitoring()
        {
            _timer = new Timer(2000); // Update every 2 seconds
            _timer.Elapsed += UpdatePerformanceData;
            _timer.Start();
        }

        private async void UpdatePerformanceData(object sender, ElapsedEventArgs e)
        {
            var (cpuUsage, memoryUsage) = await GetCpuAndMemoryUsage();

            Dispatcher.Invoke(() =>
            {
                CpuUsageTextBlock.Text = $"{cpuUsage:F2}%";
                MemoryUsageTextBlock.Text = $"{memoryUsage:F2} MB";
            });
        }

        private async Task<(double, double)> GetCpuAndMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
            var ramCounter = new PerformanceCounter("Process", "Private Bytes", process.ProcessName, true);

            cpuCounter.NextValue();
            ramCounter.NextValue();

            await Task.Delay(500);

            var cpuUsage = Math.Round(cpuCounter.NextValue() / Environment.ProcessorCount, 2);
            var memoryUsage = Math.Round(ramCounter.NextValue() / 1024 / 1024, 2); // MB

            return (cpuUsage, memoryUsage);
        }
        #endregion Performance monitoring

        long _numberOfFeatures = 0;
        private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            _featureService = UrlTextBox.Text;
            if (string.IsNullOrEmpty(_featureService))
            {
                MessageBox.Show("Please enter a valid feature service URL.");
                return;
            }

            try
            {
                LoadDataButton.IsEnabled = false;
                if (_isComparedView)
                {
                    // Create and add the static layer.
                    var staticLayer = await CreateFeatureLayerAsync(_featureService, FeatureRenderingMode.Static);
                    MyStaticSceneView.Scene.OperationalLayers.Add(staticLayer);

                    // Create and add the dynamic layer.
                    var dynamicLayer = await CreateFeatureLayerAsync(_featureService, FeatureRenderingMode.Dynamic);
                    MyDynamicSceneView.Scene.OperationalLayers.Add(dynamicLayer);

                    // Get Feature numbers
                    var count = await QueryFearureCountAsync(dynamicLayer.FeatureTable);
                    _numberOfFeatures += count;
                    LayersInfoTextBlock.Text = $"Number of features: {_numberOfFeatures}";

                    // Fly to the full extent of the layer.
                    await MyDynamicSceneView.SetViewpointAsync(new Viewpoint(dynamicLayer.FullExtent), TimeSpan.FromSeconds(3));
                }
                else
                {
                    var renderingMode = (FeatureRenderingMode)RenderingModeComboBox.SelectedIndex;
                    var layer = await CreateFeatureLayerAsync(_featureService, renderingMode);
                    MySceneView.Scene.OperationalLayers.Add(layer);

                    // Get Feature numbers
                    var count = await QueryFearureCountAsync(layer.FeatureTable);
                    _numberOfFeatures += count;
                    LayersInfoTextBlock.Text = $"Number of features: {_numberOfFeatures}";

                    // Fly to the full extent of the layer.
                    await MySceneView.SetViewpointAsync(new Viewpoint(layer.FullExtent), TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading data");
            }
            finally
            {
                LoadDataButton.IsEnabled = true;
            }
        }

        private async Task<FeatureLayer> CreateFeatureLayerAsync(string url, FeatureRenderingMode renderingMode)
        {
            // Create the service feature table.
            ServiceFeatureTable serviceFeatureTable = new ServiceFeatureTable(new Uri(url));

            // Create the feature layer using the service feature table and set the rendering mode.
            FeatureLayer featureLayer = new FeatureLayer(serviceFeatureTable) { RenderingMode = renderingMode };

            await featureLayer.LoadAsync();

            return featureLayer;
        }

        private async Task<long> QueryFearureCountAsync(FeatureTable featureTable)
        {
            if (featureTable == null)
            {
                return 0;
            }

            // Create the query parameters.
            QueryParameters queryParameters = new QueryParameters
            {
                WhereClause = "1=1",
                ReturnGeometry = false
            };

            // Query the feature table.
            long count = await featureTable.QueryFeatureCountAsync(queryParameters);

            return count;
        }

        bool _isComparedView = false;
        private void ComparedViewCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _isComparedView = ComparedViewCheckBox.IsChecked == true;

            if (_isComparedView)
            {
                ComparedView.Visibility = Visibility.Visible;
                SingleView.Visibility = Visibility.Collapsed;
                RenderingModeComboBox.SelectedIndex = -1;
                RenderingModeComboBox.IsEnabled = false;
            }
            else
            {
                SingleView.Visibility = Visibility.Visible;
                ComparedView.Visibility = Visibility.Collapsed;
                RenderingModeComboBox.SelectedIndex = 0;
                RenderingModeComboBox.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all data in the views
            if (_isComparedView)
            {
                MyStaticSceneView.Scene.OperationalLayers.Clear();
                MyDynamicSceneView.Scene.OperationalLayers.Clear();
            }
            else
            {
                MySceneView.Scene.OperationalLayers.Clear();
            }
            LayersInfoTextBlock.Text = "";
            _numberOfFeatures = 0;
        }
    }
}