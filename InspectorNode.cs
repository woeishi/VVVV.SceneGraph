using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;

using SceneGraph.Core;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Inspektor", Category = "SceneGraph", AutoEvaluate = true,
                Help = "provides a treeview on the graph with node details. outputs the selected nodes for debugging purpose", Tags = "GUI, SceneExplorer, Treeview",
                Author = "woei")]
    public class InspektorNode : System.Windows.Forms.UserControl, IPluginEvaluate, IDisposable
    {
        #region fields & pins
        #pragma warning disable 0649
        [Input("Graph In")]
        IDiffSpread<GraphNode> FInput;

        [Output("Graph Out", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Selected", AutoFlush = false)]
        ISpread<string> FSelectedName;

        ElementHost WPFHost;
        StackPanel FStackPanel;
        bool NeedsFlush = false;
        #pragma warning restore
        #endregion fields & pins

        public InspektorNode()
        {
            WPFHost = new ElementHost();
            WPFHost.Dock = System.Windows.Forms.DockStyle.Fill;
            WPFHost.BackColor = System.Drawing.Color.LightGray;
            FStackPanel = new StackPanel();
            FStackPanel.VerticalAlignment = VerticalAlignment.Stretch;
            FStackPanel.CanVerticallyScroll = true;
            WPFHost.Child = FStackPanel;
            Controls.Add(WPFHost);
        }

        public new void Dispose()
        {
            WPFHost.Dispose();
            base.Dispose();
        }

        public void Evaluate(int spreadMax)
        {
            if (FInput.IsChanged)
            {
                FOutput.SliceCount = FInput.SliceCount;
                FSelectedName.SliceCount = FInput.SliceCount;
                FStackPanel.Children.Clear();
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    FOutput[i] = FInput[i];
                    FSelectedName[i] = FOutput[i]?.Name ?? string.Empty;
                    var tv = new TreeView();
                    tv.Background = Brushes.LightGray;
                    tv.Padding = new Thickness(0, 4, 0, 4);
                    tv.BorderThickness = new Thickness(0, 0, 0, 1);
                    if (FInput[i] != null)
                    {
                        var gnc = Populate(tv.Items, FInput[i], i);
                        gnc.AddMetaInfo(FInput[i].Scene.Filename);
                        gnc.Focus();
                        FStackPanel.Children.Add(tv);
                    }
                }
                NeedsFlush = true;
            }
            if (NeedsFlush)
            {
                FOutput.Flush();
                FSelectedName.Flush();
                NeedsFlush = false;
            }
        }

        GraphNodeControl Populate(ItemCollection collection, GraphNode node, int sliceIndex)
        {
            GraphNodeControl gnc;
            if (node.Element is MeshElement)
            {
                gnc = new MeshElementControl(node, sliceIndex);
            }
            else
            {
                gnc = new GraphNodeControl(node, sliceIndex);
            
                foreach (var c in node.Children)
                    Populate(gnc.Items, c, sliceIndex);
                if (gnc.Items.Count > 0)
                    gnc.IsExpanded = true;
            }

            gnc.Selected += GraphNodeSelected;
            collection.Add(gnc);
            return gnc;
        }

        private void GraphNodeSelected(object sender, RoutedEventArgs e)
        {
            var gnc = (e.Source as GraphNodeControl);
            if (gnc != null)
            {
                FOutput[gnc.SliceIndex] = gnc.GraphNode;
                FSelectedName[gnc.SliceIndex] = gnc.GraphNode.Name;
                NeedsFlush = true;
            }
            e.Handled = true;
        }

        internal class GraphViewItem : TreeViewItem
        {
            internal StackPanel Stack { get; }
            internal TextBlock Secondary { get; set; }
            internal GraphViewItem(string mainText, bool selectable = false)
            {
                Stack = new StackPanel();
                Stack.Orientation = Orientation.Horizontal;

                if (selectable)
                    Stack.Children.Add(new TextBox() { Background = Brushes.Transparent, BorderThickness = new Thickness(0), IsReadOnly = true });
                else
                    Stack.Children.Add(new TextBlock());

                dynamic primary = Stack.Children[0];
                primary.Padding = new Thickness(4, 2, 6, 2);
                primary.FontFamily = new FontFamily("Verdana");
                primary.FontSize = 10;
                primary.Text = mainText;

                Secondary = new TextBlock();
                Secondary.Padding = new Thickness(2, 1, 2, 1);
                Secondary.VerticalAlignment = VerticalAlignment.Center;
                Secondary.FontFamily = new FontFamily("Verdana");
                Secondary.FontSize = 8;
                Stack.Children.Add(Secondary);

                this.Header = Stack;
            }
        }

        internal class GraphNodeControl : GraphViewItem
        {
            public GraphNode GraphNode { get; }
            public int SliceIndex { get; }

            internal GraphNodeControl(GraphNode node, int sliceIndex) : base(node.Name, true)
            {
                GraphNode = node;
                SliceIndex = sliceIndex;
                this.BorderBrush = Brushes.DarkGray;
                this.BorderThickness = new Thickness(0, 0, 0, 1);
                this.Margin = new Thickness(0, 2, 0, 2);

                this.Secondary.Text = $"[id {node.ID}]";
            }

            public void AddMetaInfo(string meta)
            {
                this.Secondary.Text += " - " + meta;
            }
        }

        internal class MeshElementControl : GraphNodeControl
        {
            internal MeshElementControl(GraphNode node, int sliceIndex)
                :base(node, sliceIndex)
            {
                var me = node.Element as MeshElement;

                if (me.Mesh.UvChannelCount > 0)
                    this.Secondary.Text += " - Has UVs";
                this.Secondary.Text += $" - Vertices: {me.Mesh.VerticesCount} - MaterialID: {me.Mesh.MaterialIndex}";
                if (me.Material.TextureType.Count > 0)
                    this.Secondary.Text += $" - Textures: {me.Material.TextureType.Count}";

                var grid = new Grid();
                grid.Margin = new Thickness(1, 3, 1, 3);
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var lAmb = new Label();
                var cAmb = me.Material.AmbientColor.ToColor();
                lAmb.Background = new SolidColorBrush(Color.FromArgb(cAmb.A, cAmb.R, cAmb.G, cAmb.B));
                Grid.SetRow(lAmb, 0);
                Grid.SetColumn(lAmb, 0);
                grid.Children.Add(lAmb);

                var lDiff = new Label();
                var cDiff = me.Material.DiffuseColor.ToColor();
                lDiff.Background = new SolidColorBrush(Color.FromArgb(cDiff.A, cDiff.R, cDiff.G, cDiff.B));
                Grid.SetRow(lDiff, 0);
                Grid.SetColumn(lDiff, 1);
                grid.Children.Add(lDiff);

                var lSpec = new Label();
                var cSpec = me.Material.SpecularColor.ToColor();
                lSpec.Background = new SolidColorBrush(Color.FromArgb(cSpec.A, cSpec.R, cSpec.G, cSpec.B));
                Grid.SetRow(lSpec, 0);
                Grid.SetColumn(lSpec, 2);
                grid.Children.Add(lSpec);

                this.Stack.Children.Add(grid);

                for (int t = 0; t < me.Material.TexturePath.Count; t++)
                    this.Items.Add(new TextureSlotControl(me.Material, t));
            }
        }

        internal class TextureSlotControl : GraphViewItem
        {
            internal TextureSlotControl(AssimpNet.AssimpMaterial material, int slot)
                :base($"{material.TextureType[slot]}")
            {
                this.Focusable = false;
                
                this.Secondary.Text = $"{material.TexturePath[slot]}";
            }
        }
    }
}
