using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;
using SalanthTweaks.Structs;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class PhantomJobHighlight : ITweak
{
    public string DisplayName => "Highlight Knowledge Stone Jobs";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose() { }

    public void OnEnable() { }

    public void OnDisable() { }


    // [AddonPreRefresh("MKDSupportJobList")]
    // public void OnMKDSupportJobListPreRefresh(
    //     AddonEvent ev, AddonArgs bargs)
    // {
    //     var args = (AddonRefreshArgs)bargs;
    //
    //     if (args.AtkValueSpan.Length < 9)
    //         return;
    //
    //     ref var atkVal = ref args.AtkValueSpan[9];
    //     atkVal.Type = (ValueType)(((int)atkVal.Type & 0xFFFF0000) | 0x00000);
    // }

    [AddonPostSetup("MKDSupportJobList")]
    public unsafe void OnMKDSupportJobListSetup(AddonEvent _, AddonArgs args)
    {
        var addon = (AddonMKDSupportJob*)((AddonSetupArgs)args).Addon.Address;

        var b = addon->Base;
        foreach (var row in addon->Rows)
        {
            foreach (var job in row.Jobs)
            {
                if (job.JobId >= b.AtkValuesSpan[26].Int)
                {
                    Plugin.Log.Info($"{job.JobId} >= {addon->Base.AtkValuesSpan[26].Int}");
                    continue;
                }

                var val = addon->Base.AtkValuesSpan[job.JobId + 2];
                // They're smuggling jobId in Type, because this is actually a data obj pointer

                Dalamud.Utility.Util.DumpMemory((IntPtr)val.AtkValues, 0x10);
                var jobId = ((uint)val.AtkValues->Type & 0xFF00) >> 8;
                Plugin.Log.Info($"typebyte {jobId:X}");
                if (jobId is 1 or 3 or 6 or 15)
                {
                    AddBox(job.Button);
                    Plugin.Log.Info("job proc");
                }
            }
        }
    }

    private unsafe void AddBox(AtkComponentButton* btn)
    {
        if (btn == null) return;

        var res = new ResNode
        {
            IsVisible = true,
            Size = new Vector2(btn->OwnerNode->Width, btn->OwnerNode->Height),
        };

        var bg = new SimpleNineGridNode
        {
            IsVisible = true,
            TopOffset = 0,
            BottomOffset = 0,
            LeftOffset = 3,
            RightOffset = 3,
            TexturePath = "ui/uld/MKDWindow.tex",
            TextureCoordinates = new Vector2(110, 0),
            TextureSize = new Vector2(9, 41),

            Alpha = 0.375f,
            Position = new Vector2(2, 4),
            Size = new Vector2(44, 62),
        };

        var glow = new SimpleNineGridNode
        {
            IsVisible = true,
            TopOffset = 30,
            BottomOffset = 30,
            LeftOffset = 30,
            RightOffset = 30,
            TexturePath = "ui/uld/IconA_Frame.tex",
            TextureCoordinates = new Vector2(240, 0),
            TextureSize = new Vector2(72, 72),

            Position = new Vector2(-12, -10),
            Size = new Vector2(72, 92),
        };
        glow.AddColor = new Vector3(0, 0.125f, 0.50f);
        bg.AttachNode(res);
        glow.AttachNode(res);

        Service.Get<UiHelper>().LinkNodeAfterTargetNode((AtkResNode*)res, (AtkComponentBase*)btn, btn->AtkResNode);
    }
}
