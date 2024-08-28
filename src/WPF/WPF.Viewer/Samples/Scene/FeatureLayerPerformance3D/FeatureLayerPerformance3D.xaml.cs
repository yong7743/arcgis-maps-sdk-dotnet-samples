// Copyright 2021 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
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

            // Initialize the sample.
            Initialize();
        }

        private void Initialize()
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

            // Create and add the static layer.
            FeatureLayer staticLayer = new FeatureLayer(new ServiceFeatureTable(new Uri(_featureService)))
            {
                RenderingMode = FeatureRenderingMode.Static
            };
            staticScene.OperationalLayers.Add(staticLayer);

            // Create and add the dynamic layer.
            FeatureLayer dynamicLayer = new FeatureLayer(new ServiceFeatureTable(new Uri(_featureService)))
            {
                RenderingMode = FeatureRenderingMode.Dynamic
            };
            dynamicScene.OperationalLayers.Add(dynamicLayer);

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
            if (otherView.IsVisible)
            {
                otherView.SetViewpoint(sendingView.GetCurrentViewpoint(ViewpointType.CenterAndScale));
            }
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
                    if (!MyDynamicSceneView.IsVisible)
                    {
                        return;
                    }

                    // Get the viewpoint
                    Viewpoint updateViewpoint = MyStaticSceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                    // Set the viewpoint
                    MyDynamicSceneView.SetViewpoint(updateViewpoint);
                }
                else // Else, update the MyStaticSceneView's viewpoint
                {
                    if (!MyStaticSceneView.IsVisible)
                    {
                        return;
                    }

                    // Get the viewpoint
                    Viewpoint updateViewpoint = MyDynamicSceneView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

                    // Set the viewpoint
                    MyStaticSceneView.SetViewpoint(updateViewpoint);
                }
            }
        }

        bool _enableStaticSceneView = true;
        bool _enableDynamicSceneView = true;
        private void MyStaticSceneViewButton_Click(object sender, RoutedEventArgs e)
        {
            _enableStaticSceneView = !_enableStaticSceneView;
            MyStaticSceneView.Visibility = _enableStaticSceneView ? Visibility.Visible : Visibility.Collapsed;
            MyStaticSceneViewButton.Content = _enableStaticSceneView ? "Hide" : "Show";
        }

        private void MyDynamicSceneViewButton_Click(object sender, RoutedEventArgs e)
        {
            _enableDynamicSceneView = !_enableDynamicSceneView;
            MyDynamicSceneView.Visibility = _enableDynamicSceneView ? Visibility.Visible : Visibility.Collapsed;
            MyDynamicSceneViewButton.Content = _enableDynamicSceneView ? "Hide" : "Show";
        }
    }
}