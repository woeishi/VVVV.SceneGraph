using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.Generic;
using SceneGraph.Core;

namespace VVVV.SceneGraph
{
    [PluginInfo(Name = "Reverse", Category = "SceneGraph",
                Help = "Reverses the order of the slices in the Spread.", Tags = "invert, spreadop",
                Author = "woei")]
    public class ReverseNode : ReverseBin<GraphNode> {}

    [PluginInfo(Name = "Select", Category = "SceneGraph",
                Help = "Returns each slice of the Input spread as often as specified by the corresponding Select slice. 0 meaning the slice will be omitted. ",
                Tags = "repeat, resample, duplicate, spreadop")]
    public class SelectNode : Select<GraphNode> { }

    [PluginInfo(Name = "CAR", Category = "SceneGraph",
                Help = "Splits a given spread into first slice and remainder.", Tags = "split, spreadop",
                Author = "woei")]
    public class CARNode : CARBin<GraphNode> { }

    [PluginInfo(Name = "CDR", Category = "SceneGraph",
                Help = "Splits a given spread into remainder and last slice.", Tags = "split, spreadop",
                Author = "woei")]
    public class CDRNode : CDRBin<GraphNode> { }

    [PluginInfo(Name = "Zip", Category = "SceneGraph",
                Help = "Interleaves all Input spreads.", Tags = "interleave, join, spreadop")]
    public class ZipNode : Zip<GraphNode> { }

    [PluginInfo(Name = "Unzip", Category = "SceneGraph",
                Help = "The inverse of Zip. Interprets the Input spread as being interleaved and untangles it. With Bin Size.", Tags = "split, spreadop")]
    public class UnzipNode : Unzip<GraphNode> { }

    [PluginInfo(Name = "Cons", Category = "SceneGraph",
                Help = "Concatenates all Input spreads.", Tags = "spreadop")]
    public class ConsNode : Cons<GraphNode> { }

    [PluginInfo(Name = "DeleteSlice", Category = "SceneGraph",
                Help = "Removes slices from the Spread at the positions addressed by the Index pin.", Tags = "remove, spreadop",
                Author = "woei")]
    public class DeleteSliceNode : DeleteSlice<GraphNode> { }

    [PluginInfo(Name = "GetSpread", Category = "SceneGraph",
                Help = "Returns sub-spreads from the input specified via offset and count", Tags = "generic, spreadop",
                Author = "woei")]
    public class GetSpreadNode : GetSpreadAdvanced<GraphNode> { }
}
