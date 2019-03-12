using System;
using System.Xml.Linq;
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
        [Input("GraphNode")]
        IDiffSpread<GraphNode> FInput;

        [Output("GraphNode", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Element", AutoFlush = false)]
        ISpread<XElement> FXml;

        [Output("Selected", AutoFlush = false)]
        ISpread<string> FSelectedName;

        ElementHost WPFHost;
        StackPanel FStackPanel;
        bool NeedsFlush = false;

        FontFamily FFont = new FontFamily("Verdana");
        int FSize = 8;
        #pragma warning restore
        #endregion fields & pins

        public InspektorNode()
        {
            FStackPanel = new StackPanel();
            FStackPanel.VerticalAlignment = VerticalAlignment.Stretch;

            var scrollview = new ScrollViewer();
            scrollview.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollview.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollview.Content = FStackPanel;

            WPFHost = new ElementHost();
            WPFHost.Dock = System.Windows.Forms.DockStyle.Fill;
            WPFHost.BackColor = System.Drawing.Color.LightGray;
            WPFHost.Child = scrollview;
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
                FXml.SliceCount = FInput.SliceCount;
                FSelectedName.SliceCount = FInput.SliceCount;
                FStackPanel.Children.Clear();
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    FOutput[i] = FInput[i];
                    FXml[i] = FOutput[i]?.XElement();
                    FSelectedName[i] = FOutput[i]?.Name ?? string.Empty;
                    if (FInput[i] != null)
                    {
                        var tv = new TreeView();
                        tv.Background = Brushes.LightGray;
                        tv.Padding = new Thickness(0, 4, 0, 4);
                        tv.BorderThickness = new Thickness(0, 0, 0, 1);
                        var gnc = Populate(tv.Items, FInput[i], i);
                        gnc.Focus();
                        var group = new GroupBox();
                        group.BorderThickness = new Thickness(0);
                        group.Header = FInput[i].Scene.Filename;
                        group.FontFamily = FFont;
                        group.FontSize = FSize;
                        group.Content = tv;
                        FStackPanel.Children.Add(group);
                    }
                }
                NeedsFlush = true;
            }
            if (NeedsFlush)
            {
                FOutput.Flush();
                FXml.Flush();
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

        void GraphNodeSelected(object sender, RoutedEventArgs e)
        {
            var gnc = (e.Source as GraphNodeControl);
            if (gnc != null)
            {
                FOutput[gnc.SliceIndex] = gnc.GraphNode;
                FXml[gnc.SliceIndex] = gnc.GraphNode.XElement();
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

                this.Loaded += GraphViewItem_Loaded;
                this.BorderBrush = Brushes.DarkGray;
                this.BorderThickness = new Thickness(0, 0, 0, 1);
                this.Margin = new Thickness(0, 2, 0, 2);
            }

            private void GraphViewItem_Loaded(object sender, RoutedEventArgs e)
            {
                if (this.VisualChildrenCount > 0)
                {
                    var grid = this.GetVisualChild(0) as Grid;
                    if (grid != null && grid.ColumnDefinitions.Count > 2)
                        grid.ColumnDefinitions.RemoveAt(1);
                }
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
                
                this.Secondary.Text = $"[id {node.ID}]";
                if (node.AnimationsReference != null)
                    foreach (var a in node.AnimationsReference.Animations)
                        this.Secondary.Text += $" - {a.Name} tps:{a.TicksPerSecond} t:{a.Duration}";
            }
        }

        internal class MeshElementControl : GraphNodeControl
        {
            internal MeshElementControl(GraphNode node, int sliceIndex) : base(node, sliceIndex)
            {
                var me = node.Element as MeshElement;

                this.Secondary.Text += $" - vertices: {me.Mesh.VerticesCount}";

                if (me.Mesh.UvChannelCount > 0)
                    this.Secondary.Text += $" - {me.Mesh.UvChannelCount} UVs";

                this.Secondary.Text += $" - materialId: {me.MaterialID}";

                if (node.Material.Textures.Length > 0)
                    this.Secondary.Text += $" - textures: {node.Material.Textures.Length}";

                var grid = new Grid();
                grid.Margin = new Thickness(1, 3, 1, 3);
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var lAmb = new Label();
                Grid.SetRow(lAmb, 0);
                Grid.SetColumn(lAmb, 0);
                var cAmb = node.Material.AmbientColor.ToColor();
                lAmb.Background = new SolidColorBrush(Color.FromArgb(cAmb.A, cAmb.R, cAmb.G, cAmb.B));
                grid.Children.Add(lAmb);

                var lDiff = new Label();
                Grid.SetRow(lDiff, 0);
                Grid.SetColumn(lDiff, 1);
                var cDiff = node.Material.DiffuseColor.ToColor();
                lDiff.Background = new SolidColorBrush(Color.FromArgb(cDiff.A, cDiff.R, cDiff.G, cDiff.B));
                grid.Children.Add(lDiff);

                var lSpec = new Label();
                Grid.SetRow(lSpec, 0);
                Grid.SetColumn(lSpec, 2);
                var cSpec = node.Material.SpecularColor.ToColor();
                lSpec.Background = new SolidColorBrush(Color.FromArgb(cSpec.A, cSpec.R, cSpec.G, cSpec.B));
                grid.Children.Add(lSpec);

                this.Stack.Children.Add(grid);

                foreach (var ti in node.Material.Textures)
                    this.Items.Add(new TextureInfoControl(ti));
            }
        }

        internal class TextureInfoControl : GraphViewItem
        {
            internal TextureInfoControl(TextureInfo textureInfo)
                :base($"{textureInfo.Intent}")
            {
                this.Focusable = false;
                
                this.Secondary.Text = $"textureId: {textureInfo.Index} - {textureInfo.Path}";
            }
        }
    }
}
