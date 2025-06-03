using System.Net.Mime;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using SalanthTweaks.Attributes;
using SalanthTweaks.Config;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class FateWindow : IConfigurableTweak
{
#pragma warning disable CS0169 // Field is never used
    private NativeController? nativeController;
    private IAddonEventManager? eventManager;
#pragma warning restore CS0169 // Field is never used

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

    }

    public void DrawConfig()
    {
        ImGui.TextWrapped("Use /fates to open a window listing currently available fates.");
    }

    public void OnDisable()
    {
        Addon?.Dispose(); 
    }
    
    [Command("/fates", "View current fates", AutoEnable: true)]
    public void OnCommand(string command, string args)
    {
        var nc = Service.Get<NativeController>(); 
        Addon ??= new FateAddon
        {
            InternalName = "STFateWindow",
            Title = "Fates",
            Size = new Vector2(305.0f+24, 415.0f),
            NativeController = nc,
            OnClose = () => Addon = null
        };
        Addon.Open();
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

    public class ListItemNode : ResNode
    {
        private CollisionNode collisionNode;
        private NineGridNode hoveredNineGridNode;
        private NineGridNode selectedNineGridNode;
        private NodeBase contentNode;

        public override void EnableEvents(IAddonEventManager eventManager, AtkUnitBase* addon)
        {
            collisionNode.EnableEvents(eventManager, addon);
            base.EnableEvents(eventManager, addon);
        }

        public ListItemNode(NodeBase contentNode)
        {
            collisionNode = new CollisionNode
            {
                NodeId = MakeNodeId(1),
                EnableEventFlags = true,
                CollisionType = CollisionType.Hit,
                Position = Vector2.Zero,
                Size = new Vector2(305, 24),
                IsVisible = true
            };
            collisionNode.AddFlags(NodeFlags.Enabled);

            hoveredNineGridNode = new NineGridNode
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
            selectedNineGridNode = new NineGridNode
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
                Size = new Vector2(64, 22)
            };
            selectPart.LoadTexture("ui/uld/ListItemA.tex");
            selectedNineGridNode.AddPart(selectPart);

            var hoverPart = new Part
            {
                TextureCoordinates = new Vector2(0, 22),
                Size = new Vector2(64, 22)
            };
            hoverPart.LoadTexture("ui/uld/ListItemA.tex");
            hoveredNineGridNode.AddPart(hoverPart);

            collisionNode.AddEvent(AddonEventType.MouseOver, () => hoveredNineGridNode.IsVisible = !selectedNineGridNode.IsVisible);
            collisionNode.AddEvent(AddonEventType.MouseOut, () => hoveredNineGridNode.IsVisible = false);
            collisionNode.AddEvent(AddonEventType.MouseClick, () =>
            {
                selectedNineGridNode.IsVisible = true;
                hoveredNineGridNode.IsVisible = false;
            });
            var nc = Service.Get<NativeController>();
            nc.AttachToNode(collisionNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(hoveredNineGridNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(selectedNineGridNode, this, NodePosition.AsLastChild);

            this.contentNode = contentNode;
            nc.AttachToNode(contentNode, this, NodePosition.AsLastChild);

        }
        
        public override void DrawConfig() {
            base.DrawConfig();
                
            using (var progressBar = ImRaii.TreeNode("Coll")) {
                if (progressBar) {
                    collisionNode.DrawConfig();
                }
            }
            
            using (var moduleName = ImRaii.TreeNode("Hov")) {
                if (moduleName) {
                    hoveredNineGridNode.DrawConfig();
                }
            }
                
            using (var timeRemaining = ImRaii.TreeNode("Sel")) {
                if (timeRemaining) {
                    selectedNineGridNode.DrawConfig();
                }
            }
                            
            using (var timeRemaining = ImRaii.TreeNode("Content")) {
                if (timeRemaining) {
                    contentNode.DrawConfig();
                }
            }
        }
    }
}
