using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

public class FateList : ComponentNode<AtkComponentBase, AtkUldComponentDataBase>
{ 
    public CollisionNode Root => CollisionNode;

    public readonly ResNode ContainerNode = new()
    {
        IsVisible = true,
    };

    public override Vector2 Size
    {
        get => base.Size;
        set => base.Size = ContainerNode.Size = value;
    }

    public FateList()
    {
        SetInternalComponentType(ComponentType.Base);

        Service.Get<NativeController>().AttachNode(ContainerNode, this, NodePosition.AfterAllSiblings);
        BuildTimelines();
        InitializeComponentEvents();
    }
    private void BuildTimelines() {
        AddTimeline(new TimelineBuilder()
                    .BeginFrameSet(1, 29)
                    .AddLabel(1, 17, AtkTimelineJumpBehavior.Start, 0)
                    .AddLabel(9, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
                    .AddLabel(10, 18, AtkTimelineJumpBehavior.Start, 0)
                    .AddLabel(19, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
                    .AddLabel(20, 7, AtkTimelineJumpBehavior.Start, 0)
                    .AddLabel(29, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
                    .EndFrameSet()
                    .Build()
        );
    }
}

