﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMap.Forms.ToolBar
{
    [System.ComponentModel.DesignTimeVisible(true)]
    public class MapQueryToolStrip : MapToolStrip
    {

        private static readonly Common.Logging.ILog Logger = Common.Logging.LogManager.GetCurrentClassLogger();

        private System.Windows.Forms.ToolStripButton _clear;
        private System.Windows.Forms.ToolStripSeparator _sep1;
        private System.Windows.Forms.ToolStripButton _queryWindow;
        private System.Windows.Forms.ToolStripButton _queryGeometry;
        private System.Windows.Forms.ToolStripSeparator _sep2;
        private System.Windows.Forms.ToolStripComboBox _queryLayerPicker;

        private SharpMap.Data.Providers.GeometryFeatureProvider _geometryProvider;
        private SharpMap.Layers.VectorLayer _layer;

        private readonly Dictionary<string, int> _dictLayerNameToIndex
            = new Dictionary<string,int>();

       protected override void InitializeComponent()
        {
            this._clear = new System.Windows.Forms.ToolStripButton();
            this._sep1 = new System.Windows.Forms.ToolStripSeparator();
            this._queryWindow = new System.Windows.Forms.ToolStripButton();
            this._queryGeometry = new System.Windows.Forms.ToolStripButton();
            this._sep2 = new System.Windows.Forms.ToolStripSeparator();
            this._queryLayerPicker = new System.Windows.Forms.ToolStripComboBox();
            this.SuspendLayout();
            // 
            // _clear
            // 
            this._clear.Image = global::SharpMap.Properties.Resources.layer_delete;
            this._clear.Name = "_clear";
            this._clear.Size = new System.Drawing.Size(23, 22);
            // 
            // _sep1
            // 
            this._sep1.Name = "_sep1";
            this._sep1.Size = new System.Drawing.Size(6, 25);
            // 
            // _queryWindow
            // 
            this._queryWindow.CheckOnClick = true;
            this._queryWindow.Image = global::SharpMap.Properties.Resources.rectangle_edit;
            this._queryWindow.Name = "_queryWindow";
            this._queryWindow.CheckedChanged += OnCheckedChanged;
            this._queryWindow.Size = new System.Drawing.Size(23, 22);
            // 
            // _queryGeometry
            // 
            this._queryGeometry.CheckOnClick = true;
            this._queryGeometry.Image = global::SharpMap.Properties.Resources.query_spatial_vector;
            this._queryGeometry.Name = "_queryGeometry";
            this._queryGeometry.Size = new System.Drawing.Size(23, 20);
            this._queryGeometry.CheckedChanged += OnCheckedChanged;
            // 
            // _sep2
            // 
            this._sep2.Name = "_sep2";
            this._sep2.Size = new System.Drawing.Size(6, 6);
            // 
            // _queryLayerPicker
            // 
            this._queryLayerPicker.Name = "_queryLayerPicker";
            this._queryLayerPicker.Size = new System.Drawing.Size(121, 21);
            // 
            // MapQueryToolStrip
            // 
            this.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._clear,
            this._sep1,
            this._queryWindow,
            this._queryGeometry,
            this._sep2,
            this._queryLayerPicker});
            this.ResumeLayout(false);

        }

        protected override void  OnMapControlChangingInternal(System.ComponentModel.CancelEventArgs e)
        {
 	        base.OnMapControlChangingInternal(e);
            if (MapControl == null) return;

            OnClear(this, EventArgs.Empty);
            MapControl.ActiveToolChanged -= OnMapControlActiveToolChanged;
            MapControl.MapQueried -= OnMapQueried;
            MapControl.Map.Layers.ListChanged -= OnListChanged;
        }

        protected override void  OnMapControlChangedInternal(EventArgs e)
        {
 	        base.OnMapControlChangedInternal(e);

            if (MapControl == null)
            {
                Enabled =false;
                return;
            }

        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (MapControl == null) return;

            var index = _queryLayerPicker.SelectedIndex;
            if (index >= 0)
            {
                var lyrName = (string)_queryLayerPicker.Items[index];
                int lyrIndex;
                if (_dictLayerNameToIndex.TryGetValue(lyrName, out lyrIndex))
                {
                    MapControl.QueryLayerIndex = lyrIndex;
                }
            }
            else
            { }
        }

        private void OnMapControlActiveToolChanged(MapBox.Tools tool)
        {
            if (MapControl == null) return;
            switch (tool)
            {
                case MapBox.Tools.QueryGeometry:
                    _queryGeometry.Checked = true;
                    _queryWindow.Checked = false;
                    break;
                case MapBox.Tools.ZoomWindow:
                    _queryGeometry.Checked = false;
                    _queryWindow.Checked = true;
                    break;
                default:
                    _queryGeometry.Checked = false;
                    _queryWindow.Checked = false;
                    break;
            }
        }

        private void OnListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            _queryLayerPicker.Items.Clear();
            if (MapControl == null)
            {
                return;
            }

            _dictLayerNameToIndex.Clear();
            var i = 0;
            foreach(var lyr in MapControl.Map.Layers)
            {
                if (lyr.LayerName == "QueriedFeatures") continue;

                if (lyr is SharpMap.Layers.ICanQueryLayer)
                {
                    _dictLayerNameToIndex.Add(lyr.LayerName, i);
                    _queryLayerPicker.Items.Add(lyr.LayerName);
                }
                i++;
            }
        }



        private void OnClear(object sender, EventArgs e)
        {
            if (MapControl == null) return;

            var map = MapControl.Map;
            if (_layer != null && map.Layers.Contains(_layer))
            {
                map.Layers.Remove(_layer);
                _layer.Dispose();
                _layer = null;
            }
        }

        private void OnMapQueried(SharpMap.Data.FeatureDataTable features)
        {
            OnClear(this, EventArgs.Empty);

            if (MapControl == null) return;

            _geometryProvider = new SharpMap.Data.Providers.GeometryFeatureProvider(features);
            _layer = new SharpMap.Layers.VectorLayer("QueriedFeatures", _geometryProvider);
            
            var map = MapControl.Map;
            map.Layers.Add(_layer);

        }

        private void OnCheckedChanged(object sender, EventArgs e)
        {
            if (MapControl == null) return;
            
            if (_queryLayerPicker.SelectedItem == null)
            {
                System.Windows.Forms.MessageBox.Show("No layer to query selected");
                return;
            }

            var checkedButton = (System.Windows.Forms.ToolStripButton)sender;

            MapBox.Tools newTool;
            if (sender == _queryWindow)
                newTool = MapBox.Tools.QueryBox;
            else if (sender == _queryGeometry)
                newTool = MapBox.Tools.QueryGeometry;
            else
            {
                if (Logger.IsWarnEnabled)
                    Logger.Warn("Unknown object invoking OnCheckedChanged()");
                return;
            }
            TrySetActiveTool(checkedButton, newTool);
            
        }
    }
}