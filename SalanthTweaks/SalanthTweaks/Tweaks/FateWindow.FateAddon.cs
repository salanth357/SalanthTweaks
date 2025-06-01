using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace SalanthTweaks.Tweaks;

public partial class FateWindow
{
    public class FateAddon : NativeAddon
    {
        public class FateHolder : IDisposable
        {
            public ushort Id;
            public bool IsDynamicEvent;

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

            public readonly FateEntryNode Node = new();

            public void Dispose()
            {
                Node.Dispose();
            }
        }

        private Dictionary<ushort, FateHolder> Fates = [];
        private Dictionary<ushort, FateHolder> DynamicEvents = [];

        private ListNode<FateEntryNode> ListNode;

        private unsafe AtkUnitBase* NativeAddon;

        private FateEntryNode fen;
        protected override unsafe void OnSetup(AtkUnitBase* addon)
        {
            NativeAddon = addon;
            ListNode = new ListNode<FateEntryNode>
            {
                IsVisible = true,
                Position = new Vector2(12, 40),
                Size = new Vector2(305, 335),
                BackgroundVisible = false,
                LayoutOrientation = LayoutOrientation.Vertical,
            };
            fen = new FateEntryNode
            {
                IsVisible = true,
                Progress = 0.5f,
                IconId = 60722,
                State = "I",
                Name = "SomeFate",
                TimeRemaining = "00:00",
            };
            NativeController.AttachToAddon(ListNode, this);
            // NativeController.AttachToAddon(fen, this);
            ListNode.Add(fen);
            base.OnSetup(addon);
        }

        protected override unsafe void OnUpdate(AtkUnitBase* addon)
        {
//            UpdateFateList(addon);
            base.OnUpdate(addon);
        }

        public unsafe void UpdateFateList() => UpdateFateList(NativeAddon);
        private unsafe void UpdateFateList(AtkUnitBase* addon)
        {
            var pl = Service.Get<IPluginLog>();
            if (addon == null)
            {
                pl.Info("Addon is null");
                return;
            }
            pl.Info("{0}", addon->NameString);
            var cd = EventFramework.Instance()->GetPublicContentDirector();
            var eventManager = new Lazy<IAddonEventManager>(Service.Get<IAddonEventManager>);
            if (cd is not null && cd->Type == PublicContentDirectorType.OccultCrescent)
            {
                var pc = (PublicContentOccultCrescent*)cd;

                foreach (var ev in pc->DynamicEventContainer.Events)
                {
                    var id = GetFieldOffset<DynamicEvent, ushort>(&ev, 0x70);
                    pl.Info("ev - {0} {1} ", id, ev.Name);
                    if (ev.State == DynamicEventState.Inactive)
                    {
                        pl.Info("  inactive");
                        if (DynamicEvents.Remove(id, out var holder))
                            ListNode.Remove(holder.Node);

                        continue;
                    }

                    var minutes = ev.SecondsLeft / 60;
                    var seconds = ev.SecondsLeft - (minutes * 60);
                    
                    if (!DynamicEvents.TryGetValue(id, out var fh))
                    {
                        pl.Info("  alloc");
                        fh = new FateHolder()
                        {
                            Id = id,
                            IconId = ev.IconObjective0,
                            IsDynamicEvent = true,
                            MapLink = ev.MapMarker.Position,
                            Name = ev.Name.ToString()
                        };
                        DynamicEvents[id] = fh;
                        ListNode.Add(fh.Node);
                        // fh.Node.EnableEvents(eventManager.Value, addon);
                    }

                    fh.Progress = ev.Progress / 255.0f;
                    fh.State = ev.State.ToString();
                    fh.TimeRemaining = $"{minutes}:{seconds:00}";
                }
            }

            var oldFates = Fates.Keys.ToHashSet();
            var fates = Service.Get<IFateTable>();
            foreach (var ft in fates)
            {
                pl.Info("ft - {0} {1}", ft.FateId, ft.Name);
                oldFates.Remove(ft.FateId); 
                if (ft.State == FateState.Ended)
                {
                    pl.Info("  ended");
                    if (Fates.Remove(ft.FateId, out var holder))
                        ListNode.Remove(holder.Node);
                    continue;
                }

                if (!DynamicEvents.TryGetValue(ft.FateId, out var fh))
                {
                    pl.Info("  alloc");
                    fh = new FateHolder()
                    {
                        Id = ft.FateId,
                        IconId = ft.IconId,
                        MapLink = ft.Position,
                        Name = ft.Name.ToString(),
                    };
                    Fates[ft.FateId] = fh;
                    ListNode.Add(fh.Node);
                    // fh.Node.EnableEvents(eventManager.Value, addon);
                }

                var minutes = ft.TimeRemaining / 60;
                var seconds = ft.TimeRemaining - (minutes * 60);

                fh.Progress = ft.Progress / 255.0f;
                fh.State = ft.State.ToString();
                fh.TimeRemaining = $"{minutes}:{seconds:00}";
            }

            foreach (var oldFate in oldFates)
            {
                pl.Info("purge {0}", oldFate);
                if (Fates.Remove(oldFate, out var holder))
                    ListNode.Remove(holder.Node);
            }
        }

        public void DrawConfig()
        {
        }
    }

}
