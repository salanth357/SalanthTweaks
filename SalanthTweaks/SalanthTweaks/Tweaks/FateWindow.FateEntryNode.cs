using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace SalanthTweaks.Tweaks;

public partial class FateWindow
{
    public sealed class FateEntryNode : ListButtonNode
    {
        #region === Properties ===

        public bool IsDynamicEvent { get; set; }

        public string Name
        {
            get => LabelNode.Text.TextValue;
            set => LabelNode.Text = value;
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
            timerNode.Text = $"{state.FirstOrDefault()} {TimeRemaining}";
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                SizeHorizontally();
            }
        }

        public int FateId { get; set; }
        public Vector3 MapLink { get; set; }

        #endregion

        private IconImageNode iconNode;

        private ResNode progressResNode;
        private BasicProgressBarNode progressBarNode;
        private TextNode timerNode;

        public FateEntryNode()
        {
            // These aren't cropped correctly by default for whatever reason
            SelectedBackgroundNode.TopOffset = HoverBackgroundNode.TopOffset = 10;
            SelectedBackgroundNode.BottomOffset = HoverBackgroundNode.BottomOffset = 10;

            IsVisible = true;
            Position = new Vector2(0, 60);
            Height = 40;

            iconNode = new IconImageNode
            {
                NodeId = MakeNodeId(1),
                Position = new Vector2(5, 7),
                Size = new Vector2(26, 26),
                IsVisible = true,
                TextureSize = new Vector2(20, 20)
            };
            
            // Customize the existing label node to match what we want
            LabelNode.FontSize = 14;
            LabelNode.LineSpacing = 16;
            LabelNode.AlignmentType = AlignmentType.Left;
            LabelNode.TextFlags = TextFlags.MultiLine | TextFlags.WordWrap;
            LabelNode.TextFlags2 = TextFlags2.Ellipsis;
            LabelNode.Position = new Vector2(32, 4);
            LabelNode.Size = new Vector2(170, 32);
            LabelNode.AddFlags(NodeFlags.Clip);

            progressResNode = new ResNode
            {
                Size = new Vector2(74, Height),
                IsVisible = true
            };
            timerNode = new TextNode
            {
                NodeId = MakeNodeId(4),
                IsVisible = true,
                Position = new Vector2(0, 8),
                Size = new Vector2(74, 14),
                FontSize = 12,
                FontType = FontType.MiedingerMed,
                AlignmentType = AlignmentType.Left  
            };
            timerNode.AddFlags(NodeFlags.HasCollision);
            progressBarNode = new BasicProgressBarNode
            {
                NodeId = MakeNodeId(5),
                IsVisible = true,
                Position = new Vector2(0, Height - 8 - 6),
                Size = new Vector2(74, 8),
                Color = ColorHelper.GetColor(2),
                EventFlagsSet = true,
            };
            var nc = Service.Get<NativeController>();
            nc.AttachNode(iconNode, this, NodePosition.AfterAllSiblings);
            nc.AttachNode(progressBarNode, progressResNode);
            nc.AttachNode(timerNode, progressResNode);
            nc.AttachNode(progressResNode, this, NodePosition.AfterAllSiblings);

            OnClick += Click;
        }

        private void SizeHorizontally()
        {
            progressResNode.X = Width - progressResNode.Width;
            LabelNode.Width = progressResNode.X - LabelNode.X - 4;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                iconNode.Dispose();
                progressBarNode.Dispose();
                timerNode.Dispose();
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
                AgentMap.Instance()->OpenMap(clientState.MapId);
                Service.Get<IFramework>().Run(() => AgentMap.Instance()->IsFlagMarkerSet = false);
                AgentMap.Instance()->FocusAddon();
            }
        }
    }
}
