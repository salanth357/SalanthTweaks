using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.ComponentNodes;
using KamiToolKit.System;
using Newtonsoft.Json;
using SalanthTweaks.Attributes;
using SalanthTweaks.Config;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using static KamiToolKit.Nodes.ComponentNodes.ButtonIcon;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class FateWindow : IConfigurableTweak
{
    private NativeController? _nativeController;
    private IAddonEventManager? _eventManager;

    private FateAddon? Addon { get; set; }
    public string DisplayName => "Fate Window";
    public TweakStatus Status { get; set; }

    public void Dispose()
    {
        Addon?.Dispose();
    }

    public void OnInitialize() { }

    public void OnEnable()
    {
        var nc = Service.Get<NativeController>();
        Addon = new FateAddon
        {
            InternalName = "STFateWindow",
            Title = "Fates",
            Size = new Vector2(305.0f+24, 415.0f),
            NativeController = nc,
        };
        Addon.Open();
    }

    public void DrawConfig()
    {
        Addon?.DrawConfig();
    }

    public void OnDisable()
    {
        Addon?.Dispose();
    }

    [Command("/fate", "View current fates", AutoEnable: true)]
    public void OnCommand(string command, string args)
    {
        Addon?.UpdateFateList();
        // Addon?.Open();
    }
    
    public class FateWindowConfig : TweakConfig
    {
        private const int CurrentVersion = 1;

        public override bool Update()
        {
            return true;
        }
    }

    [TweakConfig]
    public FateWindowConfig Config { get; set; } = null!;

    [JsonObject(MemberSerialization.OptIn)]
    public class ListItemNode : ResNode
    {
        [JsonProperty] private CollisionNode CollisionNode;
        [JsonProperty] private NineGridNode HoveredNineGridNode;
        [JsonProperty] private NineGridNode SelectedNineGridNode;
        private NodeBase ContentNode;

        public override void EnableEvents(IAddonEventManager eventManager, AtkUnitBase* addon)
        {
            CollisionNode.EnableEvents(eventManager, addon);
            base.EnableEvents(eventManager, addon);
        }

        public ListItemNode(NodeBase contentNode)
        {
            CollisionNode = new CollisionNode
            {
                NodeId = MakeNodeId(1),
                EnableEventFlags = true,
                CollisionType = CollisionType.Hit,
                Position = Vector2.Zero,
                Size = new Vector2(305, 24),
                IsVisible = true
            };
            CollisionNode.AddFlags(NodeFlags.Enabled);

            HoveredNineGridNode = new NineGridNode
            {
                TopOffset = 10,
                LeftOffset = 16,
                BottomOffset = 10,
                RightOffset = 1,
                BlendMode = 0,
                PartsRenderType = 240,
                Position = Vector2.Zero,
                Size = new Vector2(305, 24),
            };
            SelectedNineGridNode = new NineGridNode
            {
                TopOffset = 10,
                LeftOffset = 16,
                BottomOffset = 10,
                RightOffset = 1,
                BlendMode = 0,
                PartsRenderType = 240,
                Position = Vector2.Zero,
                Size = new Vector2(305, 24)
            };
 
            var selectPart = new Part
            {
                TextureCoordinates = new Vector2(0, 0),
                Size = new Vector2(64, 22),
            };
            selectPart.LoadTexture("ui/uld/ListItemA.tex");
            SelectedNineGridNode.AddPart(selectPart);

            var hoverPart = new Part
            {
                TextureCoordinates = new Vector2(0, 22),
                Size = new Vector2(64, 22),
            };
            hoverPart.LoadTexture("ui/uld/ListItemA.tex");
            HoveredNineGridNode.AddPart(hoverPart);

            CollisionNode.AddEvent(AddonEventType.MouseOver, () => HoveredNineGridNode.IsVisible = !SelectedNineGridNode.IsVisible);
            CollisionNode.AddEvent(AddonEventType.MouseOut, () => HoveredNineGridNode.IsVisible = false);
            CollisionNode.AddEvent(AddonEventType.MouseClick, () =>
            {
                SelectedNineGridNode.IsVisible = true;
                HoveredNineGridNode.IsVisible = false;
            });
            var nc = Service.Get<NativeController>();
            nc.AttachToNode(CollisionNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(HoveredNineGridNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(SelectedNineGridNode, this, NodePosition.AsLastChild);

            ContentNode = contentNode;
            nc.AttachToNode(contentNode, this, NodePosition.AsLastChild);

        }
        
        public override void DrawConfig() {
            base.DrawConfig();
                
            using (var progressBar = ImRaii.TreeNode("Coll")) {
                if (progressBar) {
                    CollisionNode.DrawConfig();
                }
            }
            
            using (var moduleName = ImRaii.TreeNode("Hov")) {
                if (moduleName) {
                    HoveredNineGridNode.DrawConfig();
                }
            }
                
            using (var timeRemaining = ImRaii.TreeNode("Sel")) {
                if (timeRemaining) {
                    SelectedNineGridNode.DrawConfig();
                }
            }
                            
            using (var timeRemaining = ImRaii.TreeNode("Content")) {
                if (timeRemaining) {
                    ContentNode.DrawConfig();
                }
            }
        }
        
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class FateEntryNode : ResNode
    {
        public string Name
        {
            get => NameNode.Text.TextValue;
            set => NameNode.Text = value;
        }

        private string state;
        public string State
        {
            get => state;
            set
            {
                if (state != value)
                {
                    state = value;
                    UpdateTimerNodeText();
                }
            }
        }
        
        public uint IconId
        {
            get => IconNode.IconId;
            set => IconNode.IconId = value;
        }

        public float Progress
        {
            get => ProgressBarNode.Progress;
            set
            {
                ProgressBarNode.Progress = value;
                ProgressBarNode.TooltipString = $"{value * 100:F0}%";
            }
        }

        private string timeRemaining;
        public string TimeRemaining
        {
            get => timeRemaining;
            set
            {
                if (timeRemaining != value)
                {
                    timeRemaining = value;
                    UpdateTimerNodeText();
                }
            }
        }

        private void UpdateTimerNodeText()
        {
            TimerNode.Text = $"{state} {TimeRemaining}";
        }

        public Vector3 MapLink { get; set; }

        [JsonProperty] private IconImageNode IconNode;
        [JsonProperty] private TextNode NameNode;
        [JsonProperty] private ProgressBarNode ProgressBarNode;
        [JsonProperty] private TextNode TimerNode;
        [JsonProperty] private CircleButtonNode MapButtonNode;

        public override void EnableEvents(IAddonEventManager eventManager, AtkUnitBase* addon)
        {
            MapButtonNode.EnableEvents(eventManager, addon);
            ProgressBarNode.EnableEvents(eventManager, addon);
            base.EnableEvents(eventManager, addon);
        }

        public override void DisableEvents(IAddonEventManager eventManager)
        {
            MapButtonNode.DisableEvents(eventManager);
            ProgressBarNode.DisableEvents(eventManager);
            base.DisableEvents(eventManager);
        }

        public FateEntryNode()
        {
            IsVisible = true;
            Position = new Vector2(0, 60);
            Size = new Vector2(305, 32);
            NodeFlags |= NodeFlags.AnchorTop | NodeFlags.AnchorBottom | NodeFlags.AnchorRight | NodeFlags.AnchorLeft |
                         NodeFlags.Enabled | NodeFlags.Fill;

            IconNode = new IconImageNode
            {
                NodeId = MakeNodeId(1),
                Position = new Vector2(2, 3),
                Size = new Vector2(28, 28),
                IsVisible = true
            };
            IconNode.TextureSize = new Vector2(20, 20);
            IconNode.NodeFlags |= NodeFlags.Enabled;
            NameNode = new TextNode
            {
                NodeId = MakeNodeId(2),
                Position = new Vector2(32, 0),
                Size = new Vector2(170, 32),
                IsVisible = true,
                AlignmentType = AlignmentType.Left,
                FontType = FontType.Axis,
                FontSize = 15,
                LineSpacing = 16,
                TextFlags = TextFlags.MultiLine| TextFlags.WordWrap,
                TextFlags2 = TextFlags2.Ellipsis,
                Color = ColorHelper.GetColor(8)
            };
            MapButtonNode = new CircleButtonNode
            {
                NodeId = MakeNodeId(3),
                IsVisible = true,
                Position = new Vector2(305-28, 2),
                Size = new Vector2(28, 28),
                Icon = PinPaper,
                OnClick = Click,
            };
            TimerNode = new TextNode
            {
                NodeId = MakeNodeId(4),
                IsVisible = true,
                Position = new Vector2(206, 2),
                Size = new Vector2(64, 14),
                FontSize = 12,
                FontType = FontType.MiedingerMed,
                AlignmentType = AlignmentType.Left,
            };
            ProgressBarNode = new ProgressBarNode
            {
                NodeId = MakeNodeId(5),
                IsVisible = true,
                Position = new Vector2(206, 20),
                Size = new Vector2(64, 8),
                Color = ColorHelper.GetColor(2),
                EventFlagsSet = true,
            };
            var nc = Service.Get<NativeController>();
            nc.AttachToNode(IconNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(NameNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(ProgressBarNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(TimerNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(MapButtonNode, this, NodePosition.AsLastChild);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IconNode.Dispose();
                NameNode.Dispose();
                ProgressBarNode.Dispose();
                TimerNode.Dispose();
                MapButtonNode.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Click()
        {
            Service.Get<IChatGui>().Print($"Display map for ft {MapLink.X},{MapLink.Y},{MapLink.Z}");
        }
        public override void DrawConfig() {
            base.DrawConfig();
                
            using (var progressBar = ImRaii.TreeNode("Icon Noe")) {
                if (progressBar) {
                    IconNode.DrawConfig();
                }
            }
            
            using (var moduleName = ImRaii.TreeNode("Name Node")) {
                if (moduleName) {
                    NameNode.DrawConfig();
                }
            }
                
            using (var timeRemaining = ImRaii.TreeNode("ProgressBar")) {
                if (timeRemaining) {
                    ProgressBarNode.DrawConfig();
                }
            }
                            
            using (var timeRemaining = ImRaii.TreeNode("Timer")) {
                if (timeRemaining) {
                    TimerNode.DrawConfig();
                }
            }
                            
            using (var timeRemaining = ImRaii.TreeNode("MapButton")) {
                if (timeRemaining) {
                    MapButtonNode.DrawConfig();
                }
            }
        }
        
    }


}
