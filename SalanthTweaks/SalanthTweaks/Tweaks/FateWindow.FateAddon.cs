using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using Microsoft.Extensions.Logging;
using SalanthTweaks.UI;
using Action = System.Action;
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

        private ScrollingAreaNode component = null!;
        private unsafe AtkUnitBase* nativeAddon;

        private readonly unsafe ListPanel* listPanel = IMemorySpace.GetUISpace()->Create<ListPanel>();

        protected override unsafe void OnSetup(AtkUnitBase* addon)
        {
            Service.Get<IClientState>().TerritoryChanged += _ => needsReset = true;
            nativeAddon = addon;

            component = new ScrollingAreaNode
            {
                NodeId = 99,
                Position = ContentStartPosition,
                Size = ContentSize,
                IsVisible = true,
                ContentHeight = ContentSize.Y
            };
            NativeController.AttachNode(component, this);

            for (var i = 0; i < 5; i++)
            {
                var fh = new FateHolder
                {
                    IconId = 60501,
                    Name = $"{i}"
                };
                fh.Node.Width = component.ContentNode.Width;

                AddEntry(fh);
            }
            listPanel->UpdateLayout();
            component.ContentHeight = listPanel->Height;

            base.OnSetup(addon);
        }

        protected override unsafe void OnShow(AtkUnitBase* addon)
        {
            closed = false;
            needsReset = true;
            base.OnShow(addon);
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

        private bool needsReset;
        private bool closed;

        protected override unsafe void OnHide(AtkUnitBase* addon)
        {
            OnClose?.Invoke();
            closed = true;
            base.OnHide(addon);
        }

        protected override unsafe void OnFinalize(AtkUnitBase* addon)
        {
            closed = true;
            base.OnFinalize(addon);
        }

        public unsafe void UpdateFateList() => UpdateFateList(nativeAddon);

        private unsafe void AddEntry(FateHolder fh)
        {
            NativeController.AttachNode(fh, component.ContentNode);
            if (fh.IsDynamicEvent)
                listPanel->Entries.InsertCopy(0, fh);
            else
                listPanel->Entries.AddCopy(fh);
        }
        private unsafe void RemoveEntry(FateHolder fh)
        {
            listPanel->Entries.Remove(fh);
            NativeController.DetachNode(fh);
        }
        private unsafe void UpdateFateList(AtkUnitBase* addon)
        {
            if (needsReset)
            {
                needsReset = false;
                fates.Clear();
                dynamicEvents.Clear();
                listPanel->Clear();
            }

            if (closed) return;
            if (addon == null)
                return;

            var changed = false;
            var eventContainer = DynamicEventContainer.GetInstance();
            if (eventContainer != null)
            {
                foreach (var ev in eventContainer->Events)
                {
                    var id = ev.DynamicEventId;
                    if (ev.State == DynamicEventState.Inactive)
                    {
                        if (dynamicEvents.Remove(id, out var holder))
                        {
                            RemoveEntry(holder);
                            changed = true;
                        }

                        continue;
                    }

                    // We don't want to track Forked Tower here
                    // it's weird and obvious from the red everything
                    if (id == 0x30) continue;

                    if (!dynamicEvents.TryGetValue(id, out var fh))
                    {
                        fh = new FateHolder
                        {
                            IsDynamicEvent = true,
                            IconId = ev.IconObjective0,
                            MapLink = ev.MapMarker.Position,
                            Name = ev.Name.ToString(),
                            FateId = id,
                        };
                        fh.Node.Width = component.ContentNode.Width;
                        dynamicEvents[id] = fh;
                        AddEntry(fh);
                        changed = true;
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
                    fh.TimeRemaining = $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
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
                    {
                        RemoveEntry(holder);
                        changed = true;
                    }
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
                    fh.Node.Width = component.ContentNode.Width;
                    fates[ft.FateId] = fh;
                    AddEntry(fh);
                    changed = true;
                }

                var duration = new TimeSpan(0, 0, (int)ft.TimeRemaining);
                fh.Progress = ft.Progress / 100.0f;
                fh.State = ft.State.ToString();
                fh.TimeRemaining = $"{duration.Minutes:00}:{duration.Seconds:00}";
            }

            foreach (var oldFate in oldFates)
            {
                if (fates.Remove(oldFate, out var holder))
                {
                    RemoveEntry(holder);
                    changed = true;
                }
            }

            if (changed)
            {
                listPanel->UpdateLayout();
                component.ContentHeight = listPanel->Height;
            }
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
        public static implicit operator NodeBase(FateHolder fh) => fh.Node;

        public static unsafe implicit operator ListPanel.ListPanelEntry(FateHolder fh) =>
            new()
            {
                Node = (AtkResNode*)fh.Node,
                Alignment = ListPanel.Alignment.Left,
            };

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

        public int FateId
        {
            get => Node.FateId;
            set => Node.FateId = value;
        }

        public readonly FateEntryNode Node = new();

        public void Dispose()
        {
            Node.Dispose();
        }
    }
}
