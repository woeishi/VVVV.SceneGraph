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
        [Input("Graph In")]
        IDiffSpread<GraphNode> FInput;

        [Output("Graph Out", AutoFlush = false)]
        ISpread<GraphNode> FOutput;

        [Output("Selected", AutoFlush = false)]
        ISpread<string> FSelectedName;

        ElementHost WPFHost;
        StackPanel FStackPanel;
        TreeView View = new TreeView();
        bool NeedsFlush = false;
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

        GraphViewItem Populate(ItemCollection collection, GraphNode node, int sliceIndex)
        {
            GraphViewItem gnc;
            if (node.Element is MeshElement)
            {
                var mpe = node.Element as MeshElement;
                gnc = new MeshElementControl(mpe.Mesh, mpe.MeshID, mpe.Material);

                for (int t = 0; t < mpe.Material.TexturePath.Count; t++)
                    gnc.Items.Add(new TextureSlotControl(mpe.Material, t));
            }
            else
            {
                gnc = new GraphNodeControl(node, sliceIndex);
            
                foreach (var c in node.Children)
                    Populate(gnc.Items, c, sliceIndex);
                if (gnc.Items.Count > 0)
                    gnc.IsExpanded = true;

            }

            //SetElementInfo(tn.Items, node);

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

        //void SetElementInfo(ItemCollection collection, GraphNode node)
        //{
        //    if (node.Element is MeshElement)
        //    {
        //        MeshElement me = node.Element as MeshElement;
        //        for (int i = 0; i < me.MeshCount; i++)
        //        {
        //            var texCount = me.Materials[i].TexturePath.Count;
        //            var mec = new MeshElementControl(me.Meshes[i], me.MeshIDs[i], me.Materials[i]);

        //            for (int t = 0; t < texCount; t++)
        //                mec.Items.Add(new TextureSlotControl(me.Materials[i], t));

        //            collection.Add(mec);
        //        }
        //    }
        //}

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

            public virtual void AddMetaInfo(string meta) { } 
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

                //var me = node.Element as MeshElement;
                //if (me != null)
                //    this.Secondary.Text += $" - Meshes: {me.MeshCount}";
            }

            public override void AddMetaInfo(string meta)
            {
                this.Secondary.Text += " - " + meta;
            }
        }

        internal class MeshElementControl : GraphViewItem
        {
            internal MeshElementControl(AssimpNet.AssimpMesh mesh, int meshID, AssimpNet.AssimpMaterial material)
                :base($"Mesh_{meshID}", true)
            {
                if (mesh.UvChannelCount > 0)
                    this.Secondary.Text += " - Has UVs";
                this.Secondary.Text += $" - Vertices: {mesh.VerticesCount} - MaterialID: {mesh.MaterialIndex}";
                if (material.TextureType.Count > 0)
                    this.Secondary.Text += $" - Textures: {material.TextureType.Count}";

                var grid = new Grid();
                grid.Margin = new Thickness(1, 3, 1, 3);
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var lAmb = new Label();
                var cAmb = material.AmbientColor.ToColor();
                lAmb.Background = new SolidColorBrush(Color.FromArgb(cAmb.A, cAmb.R, cAmb.G, cAmb.B));
                Grid.SetRow(lAmb, 0);
                Grid.SetColumn(lAmb, 0);
                grid.Children.Add(lAmb);

                var lDiff = new Label();
                var cDiff = material.DiffuseColor.ToColor();
                lDiff.Background = new SolidColorBrush(Color.FromArgb(cDiff.A, cDiff.R, cDiff.G, cDiff.B));
                Grid.SetRow(lDiff, 0);
                Grid.SetColumn(lDiff, 1);
                grid.Children.Add(lDiff);

                var lSpec = new Label();
                var cSpec = material.SpecularColor.ToColor();
                lSpec.Background = new SolidColorBrush(Color.FromArgb(cSpec.A, cSpec.R, cSpec.G, cSpec.B));
                Grid.SetRow(lSpec, 0);
                Grid.SetColumn(lSpec, 2);
                grid.Children.Add(lSpec);

                this.Stack.Children.Add(grid);
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
