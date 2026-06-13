using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Structs;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class CosmicLotteryHelper : ITweak
{
    public string DisplayName => "CosmicLotteryHelper";
    public TweakStatus Status { get; set; }

    public void OnInitialize() { }

    public void Dispose() { }

    public unsafe void OnEnable()
    {
        var addon = (AddonWKSLottery*)RaptureAtkUnitManager.Instance()->GetAddonByName("WKSLottery");
        InitNodes(addon);
    }

    public void OnDisable() { }


    public struct Chances()
    {
        public readonly Dictionary<uint, float> Left = [];
        public readonly Dictionary<uint, float> Right = [];
        public readonly Dictionary<uint, float> Third = [];
    }

    private readonly List<TextNineGridNode> leftNodes = [];
    private readonly List<TextNineGridNode> rightNodes = [];
    private readonly List<TextNineGridNode> thirdNodes = [];

    private unsafe void InitNodes(AddonWKSLottery* addon)
    {
        if (addon == null)
            return;
        var pRenderer = addon->GetRenderer();
        if (pRenderer == null) return;
        InitNodes(ref Unsafe.AsRef<AddonWKSLottery.Renderer>(pRenderer));
    }

    public void InitNodes(ref AddonWKSLottery.Renderer renderer)
    {
        leftNodes.Clear();
        rightNodes.Clear();
        thirdNodes.Clear();
        CreateNode(renderer.LeftWheelItems, leftNodes);
        CreateNode(renderer.RightWheelItems, rightNodes);
        // CreateNode(renderer.ThirdWheelItems, thirdNodes);
        UpdateUI(ref renderer);
        return;

        unsafe void CreateNode(Span<AddonWKSLottery.WKSLotteryItem> items, List<TextNineGridNode> nodes)
        {
            foreach (var item in items)
            {
                const int ngnWidth = 40;
                var tn = new TextNineGridNode
                {
                    NodeFlags = NodeFlags.AnchorRight | NodeFlags.AnchorTop | NodeFlags.Enabled,
                    IsVisible = true,
                    FontSize = 12,
                    FontType = FontType.Axis,
                    Position = new Vector2(item.RootComponent->OwnerNode->GetWidth() - ngnWidth - 10, 17),
                    Height = 20,
                    Width = ngnWidth,
                    TextOutlineColor = ColorHelper.GetColor(550),
                };
                tn.TextFlags |= TextFlags.Edge;
                tn.TextNode.AlignmentType = AlignmentType.Center;
                nodes.Add(tn);
                tn.AttachNode(item.RootComponent->OwnerNode);
                var width = tn.Position.X - item.ItemNameTextNode->GetXFloat();
                item.ItemNameTextNode->SetWidth((ushort)width);
                item.ItemNameTextNode->SetText(item.ItemNameTextNode->GetText());
            }
        }
    }


    [AddonPostSetup("WKSLottery")]
    private unsafe void OnWKSLotteryPostSetup(AddonEvent ev, AddonArgs args)
    {
        var sb = new StringBuilder();
        var idx = 0;

        var sargs = (AddonSetupArgs)args;
        var avSpan = new Span<AtkValue>((void*)sargs.AtkValues, (int)sargs.AtkValueCount);

        foreach (var av in avSpan)
        {
            sb.AppendLine($"{idx++}: {av.ToString()}");
        }

        Service.Get<IPluginLog>().Information($"WKSLottery post-setup\n{sb}");
        InitNodes((AddonWKSLottery*)args.Addon.Address);
        Service.Get<IFramework>().RunOnTick(UpdateUI);
    }

    [AddonPostRefresh("WKSLottery")]
    private unsafe void OnWKSLotteryPostRefresh(AddonEvent ev, AddonArgs args)
    {
        Service.Get<IPluginLog>().Information($"WKSLottery post-refresh");
        UpdateUI((AddonWKSLottery*)args.Addon.Address);
    }

    private unsafe void UpdateUI()
    {
        var addon = (AddonWKSLottery*)RaptureAtkUnitManager.Instance()->GetAddonByName("WKSLottery");
        UpdateUI(addon);
    }

    private unsafe void UpdateUI(AddonWKSLottery* addon)
    {
        if (addon == null)
            return;
        var pRenderer = addon->GetRenderer();
        if (pRenderer == null) return;
        UpdateUI(ref Unsafe.AsRef<AddonWKSLottery.Renderer>(pRenderer));
    }

    private void UpdateUI(ref AddonWKSLottery.Renderer renderer)
    {
        Service.Get<IPluginLog>().Info("UpdateUI");

        var chances = CalculateChances(ref renderer);

        UpdateWheel(renderer.LeftWheelItems, leftNodes, chances.Left);
        UpdateWheel(renderer.RightWheelItems, rightNodes, chances.Right);
        // UpdateWheel(renderer.ThirdWheelItems, thirdNodes, chances.Third);
        return;

        void UpdateWheel(
            Span<AddonWKSLottery.WKSLotteryItem> wheelItems, List<TextNineGridNode> nodes,
            Dictionary<uint, float> probabilities)
        {
            for (var i = 0; i < wheelItems.Length; i++)
            {
                ref var item = ref wheelItems[i];
                if (!probabilities.TryGetValue(item.Index, out var probability))
                {
                    continue;
                }

                var node = nodes[i];
                Service.Get<IPluginLog>().Info($"UpdateWheel {i} - {probability} {probability * 100:N0}");
                node.String = $"{probability * 100:N0}%";
                // node.Width = node.TextNode.Width;
            }
        }
    }

    private Chances CalculateChances(ref AddonWKSLottery.Renderer renderer)
    {
        var chances = new Chances();

        var pl = Service.Get<IPluginLog>();
        ProcessWheel(renderer.LeftWheelChunks, chances.Left);
        ProcessWheel(renderer.RightWheelChunks, chances.Right);
        // ProcessWheel(renderer.ThirdWheelChunks, chances.Third);

        return chances;

        void ProcessWheel(Span<AddonWKSLottery.WKSLotteryWheelChunk> chunks, Dictionary<uint, float> results)
        {
            pl.Info("ProcessWheel");
            SortedDictionary<uint, uint> sectionCount = [];
            var idx = 0;
            foreach (var chunk in chunks)
            {
                pl.Info($"  {idx++} {chunk.ItemIndex}");

                sectionCount.TryAdd(chunk.ItemIndex, (uint)0);
                sectionCount[chunk.ItemIndex]++;
            }

            foreach (var section in sectionCount)
            {
                pl.Info($"= {section.Key} {section.Value}");
                results[section.Key] = (float)section.Value / chunks.Length;
            }
        }
    }
}
