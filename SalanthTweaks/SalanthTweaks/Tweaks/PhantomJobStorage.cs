using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class PhantomJobStorage : ITweak
{
    public void Dispose()
    {
    }

    public string DisplayName => "Phantom Job Storage";

    public void DrawConfig()
    {
        ImGui.TextWrapped("Adds two slash commands for managing your current phantom job:");
        ImGui.AlignTextToFramePadding();
        ImGui.Bullet();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("/phjstore -");
        }
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("stores the current phantom job");

        ImGui.AlignTextToFramePadding();
        ImGui.Bullet();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("/phjload -");
        }
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("loads the previously stored phantom job");

    }

    public TweakStatus Status { get; set; }
    public void OnInitialize()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }

    private byte currentJob = 0xFF;
    [Command("/phjstore", "store current phantom job", AutoEnable: true)]
    public unsafe void OnStore(string command, string args)
    {
        var pcoc = PublicContentOccultCrescent.GetInstance();
        if (pcoc == null) return;
        var inst = AgentMKDSupportJob.Instance();
        if (inst == null) return;
        currentJob = inst->CurrentJob;
        Service.Get<IChatGui>().Print("Storing phantom job");
    }

    [Command("/phjload", "load previously stored phantom job", AutoEnable: true)]
    public unsafe void OnLoad(string command, string args)
    {
        var pcoc = PublicContentOccultCrescent.GetInstance();
        if (pcoc == null) return;
        var inst = AgentMKDSupportJob.Instance();
        if (inst == null) return;
        var agent = AgentMKDSupportJobList.Instance();
        if (agent == null) return;
        if (currentJob < pcoc->State.SupportJobLevels.Length && currentJob != inst->CurrentJob)
        {
            Service.Get<IChatGui>().Print("Restoring phantom job");
            agent->ChangeSupportJob(currentJob);
        }
    }
}
