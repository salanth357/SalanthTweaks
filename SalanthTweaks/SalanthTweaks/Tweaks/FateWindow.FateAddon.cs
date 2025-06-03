using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using KamiToolKit;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.ComponentNodes;
using KamiToolKit.System;
using Microsoft.Extensions.Logging;
using SalanthTweaks.UI;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace SalanthTweaks.Tweaks;

public partial class FateWindow
{
    public class FateAddon : NativeAddon
    {
        private readonly ILogger<FateAddon> logger = Service.Get<ILogger<FateAddon>>();
        public Action? OnClose { get; set; }

        private readonly Dictionary<ushort, FateHolder> fates = [];
        private readonly Dictionary<ushort, FateHolder> dynamicEvents = [];

        private ListNode<NodeBase> listNode = null!;

        private unsafe AtkUnitBase* nativeAddon;
        
        protected override unsafe void OnSetup(AtkUnitBase* addon)
        {
            Service.Get<IClientState>().TerritoryChanged += _ => needsReset = true;
            nativeAddon = addon;
            listNode = new ListNode<NodeBase>
            {
                NodeId = MakeNodeId(88),
                IsVisible = true,
                Size = new Vector2(305, 335),
                Position = new Vector2(12, 40),
                BackgroundVisible = false,
                LayoutOrientation = LayoutOrientation.Vertical
            };
            NativeController.AttachToAddon(listNode, this);
            base.OnSetup(addon);
        }


        protected override unsafe void OnUpdate(AtkUnitBase* addon)
        {
            try
            {
                UpdateFateList(addon);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Uncaught exception in OnUpdate hook"); 
            }

            base.OnUpdate(addon);
        }

        private bool isGoingAway;
        private bool needsReset;

        protected override unsafe void OnHide(AtkUnitBase* addon)
        {
            isGoingAway = true;
            OnClose?.Invoke();
            base.OnHide(addon);
        }

        public unsafe void UpdateFateList() => UpdateFateList(nativeAddon);
        private unsafe void UpdateFateList(AtkUnitBase* addon)
        {
            if (needsReset)
            {
                needsReset = false;
                fates.Clear();
                dynamicEvents.Clear();
                listNode.Clear();
            }
            if (isGoingAway) return;
            if (addon == null) 
                return;

            var cd = EventFramework.Instance()->GetPublicContentDirector();
            var eventManager = new Lazy<IAddonEventManager>(Service.Get<IAddonEventManager>);
            if (cd is not null && cd->Type == PublicContentDirectorType.OccultCrescent)
            {
                var pc = (PublicContentOccultCrescent*)cd;

                foreach (var ev in pc->DynamicEventContainer.Events)
                {
                    var id = GetFieldOffset<DynamicEvent, ushort>(&ev, 0x74);
                    if (ev.State == DynamicEventState.Inactive)
                    {
                        if (dynamicEvents.Remove(id, out var holder))
                            listNode.Remove(holder.Node);

                        continue;
                    }
                    
                    if (!dynamicEvents.TryGetValue(id, out var fh))
                    {
                        fh = new FateHolder
                        {
                            IsDynamicEvent = true,
                            IconId = ev.IconObjective0,
                            MapLink = ev.MapMarker.Position,
                            Name = ev.Name.ToString(),
                            FateId = id
                        };
                        dynamicEvents[id] = fh;
                        listNode.Add(fh.Node);
                        fh.Node.EnableEvents(eventManager.Value, addon);
                    }

                    var duration = ev.State switch
                    {
                        DynamicEventState.Register => DateTimeOffset.FromUnixTimeSeconds(ev.StartTimestamp)
                                                                    .Subtract(DateTimeOffset.UtcNow),
                        DynamicEventState.Battle => new TimeSpan(0, 0, (int)ev.SecondsLeft),
                        _ => TimeSpan.Zero
                    };


                    fh.Progress = ev.Progress / 100.0f;
                    fh.State = ev.State.ToString();
                    fh.TimeRemaining = $"{duration.Minutes:00}:{duration.Seconds:00}";

                    // Forked Tower - hide timer since it's not relevant
                    if (id == 0x30)
                        fh.State = fh.TimeRemaining = "";
                }
            }

            var oldFates = fates.Keys.ToHashSet();
            var fateTable = Service.Get<IFateTable>();
            foreach (var ft in fateTable)
            {
                oldFates.Remove(ft.FateId);
                if (ft.State == FateState.Ended)
                {
                    if (fates.Remove(ft.FateId, out var holder))
                        listNode.Remove(holder.Node);
                    continue;
                }

                if (!fates.TryGetValue(ft.FateId, out var fh))
                {
                    fh = new FateHolder
                    {
                        IconId = ft.IconId,
                        MapLink = ft.Position,
                        Name = ft.Name.ToString(),
                        FateId = ft.FateId
                    };
                    fates[ft.FateId] = fh;
                    listNode.Add(fh.Node);
                    fh.Node.EnableEvents(eventManager.Value, addon);
                }

                var duration = new TimeSpan(0, 0, (int)ft.TimeRemaining);
                fh.Progress = ft.Progress / 100.0f;
                fh.State = ft.State.ToString();
                fh.TimeRemaining = $"{duration.Minutes:00}:{duration.Seconds:00}";
            }

            foreach (var oldFate in oldFates)
                if (fates.Remove(oldFate, out var holder))
                    listNode.Remove(holder.Node);

            foreach (var n in listNode)
                n.RemoveFlags(NodeFlags.HasCollision);
            
            nativeAddon->UldManager.UpdateDrawNodeList();
            nativeAddon->UpdateCollisionNodeList(false);
        }

        public void DrawConfig()
        {
            foreach (var (id, fate) in fates.Concat(dynamicEvents))
            {
                ImGui.Text(id.ToString());
                ObjectPrinter.DrawObject(fate);
            }
        }
    }
    public class FateHolder : IDisposable
    {

        public bool IsDynamicEvent
        {
            get => Node.IsDynamicEvent;
            set => Node.IsDynamicEvent = value;
        }

        public string State
        {
            get => Node.State;
            set => Node.State = value;
        }
            
        public string Name
        {
            get => Node.Name;
            set => Node.Name = value;
        }

        public uint IconId
        {
            get => Node.IconId;
            set => Node.IconId = value;
        }

        public float Progress
        {
            get => Node.Progress;
            set => Node.Progress = value;
        }

        public string TimeRemaining
        {
            get => Node.TimeRemaining;
            set => Node.TimeRemaining = value;
        }

        public Vector3 MapLink
        {
            get => Node.MapLink;
            set => Node.MapLink = value;
        }
        
        
        public int FateId         {
            get => Node.FateId;
            set => Node.FateId = value;
        }

        public readonly FateEntryNode Node = new();

        public void Dispose()
        {
            Node.Dispose();
        }
    }


    public sealed class FateEntryNode : ResNode
    {
        public bool IsDynamicEvent { get; set; }

        public string Name
        {
            get => nameNode.Text.TextValue;
            set => nameNode.Text = value;
        }

        private string state = "";

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
            get => iconNode.IconId;
            set => iconNode.IconId = value;
        }

        public float Progress
        {
            get => progressBarNode.Progress;
            set
            {
                progressBarNode.Progress = value;
                progressBarNode.TooltipString = $"{value * 100:F0}%";
            }
        }

        private string timeRemaining = "00:00";

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
            timerNode.Text = $"{state.First()} {TimeRemaining}";
        }

        public int FateId { get; set; }
        public Vector3 MapLink { get; set; }

        private IconImageNode iconNode;
        private TextNode nameNode;
        private ProgressBarNode progressBarNode;
        private TextNode timerNode;
        private CircleButtonNode mapButtonNode;

        public override unsafe void EnableEvents(IAddonEventManager eventManager, AtkUnitBase* addon)
        {
            mapButtonNode.EnableEvents(eventManager, addon);
            progressBarNode.EnableEvents(eventManager, addon);
            base.EnableEvents(eventManager, addon);
        }

        public override void DisableEvents(IAddonEventManager eventManager)
        {
            mapButtonNode.DisableEvents(eventManager);
            progressBarNode.DisableEvents(eventManager);
            base.DisableEvents(eventManager);
        }

        public FateEntryNode()
        {
            IsVisible = true;
            Position = new Vector2(0, 60);
            Size = new Vector2(305, 32);

            iconNode = new IconImageNode
            {
                NodeId = MakeNodeId(1),
                Position = new Vector2(2, 3),
                Size = new Vector2(28, 28),
                IsVisible = true
            };
            iconNode.TextureSize = new Vector2(20, 20);
            iconNode.NodeFlags |= NodeFlags.Enabled;
            nameNode = new TextNode
            {
                NodeId = MakeNodeId(2),
                Position = new Vector2(32, 0),
                Size = new Vector2(170, 32),
                IsVisible = true,
                AlignmentType = AlignmentType.Left,
                FontType = FontType.Axis,
                FontSize = 15,
                LineSpacing = 16,
                TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
                TextFlags2 = TextFlags2.Ellipsis,
                TextColor = ColorHelper.GetColor(8)
            };
            mapButtonNode = new CircleButtonNode
            {
                NodeId = MakeNodeId(3),
                IsVisible = true,
                Position = new Vector2(305 - 28, 2),
                Size = new Vector2(28, 28),
                Icon = ButtonIcon.PinPaper,
                OnClick = Click
            };
            timerNode = new TextNode
            {
                NodeId = MakeNodeId(4),
                IsVisible = true,
                Position = new Vector2(206, 2),
                Size = new Vector2(64, 14),
                FontSize = 12,
                FontType = FontType.MiedingerMed,
                AlignmentType = AlignmentType.Left,
            };
            timerNode.AddFlags(NodeFlags.HasCollision);
            progressBarNode = new ProgressBarNode
            {
                NodeId = MakeNodeId(5),
                IsVisible = true,
                Position = new Vector2(206, 20),
                Size = new Vector2(64, 8),
                Color = ColorHelper.GetColor(2),
                EventFlagsSet = true,
            };
            var nc = Service.Get<NativeController>();
            nc.AttachToNode(iconNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(nameNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(progressBarNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(timerNode, this, NodePosition.AsLastChild);
            nc.AttachToNode(mapButtonNode, this, NodePosition.AsLastChild);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                iconNode.Dispose();
                nameNode.Dispose();
                progressBarNode.Dispose();
                timerNode.Dispose();
                mapButtonNode.Dispose();
            }

            base.Dispose(disposing);
        }

        private unsafe void Click()
        {
            if (IsDynamicEvent)
            {
                var clientState = Service.Get<IClientState>();
                var omf = stackalloc OpenMapInfo[1];
                Unsafe.InitBlock(omf, 0, (uint)sizeof(OpenMapInfo));
                omf->MapId = clientState.MapId;
                omf->Type = MapType.Bozja;
                omf->FateId = (uint)FateId;
                AgentMap.Instance()->OpenMap(omf);
            }
            else
            {
                var clientState = Service.Get<IClientState>();
                AgentMap.Instance()->SetFlagMapMarker(clientState.TerritoryType, clientState.MapId, MapLink, 0);
                AgentMap.Instance()->OpenMap(clientState.MapId, windowTitle: "Hi there");
                Service.Get<IFramework>().Run(() => AgentMap.Instance()->IsFlagMarkerSet = false);
                AgentMap.Instance()->FocusAddon();
            }
        }
    }
}
