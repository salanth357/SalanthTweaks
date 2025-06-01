using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
// ReSharper disable line InconsistentNaming
public unsafe class LoadingImages(ILogger<LoadingImages> Log) : ITweak
{
    private ExcelSheet<TerritoryType> territoryTypeSheet = null!;


    private const ushort Height = 1080;
    private const ushort Width = 1920;
    private const float ScaleX = 0.595f;
    private const float ScaleY = 0.595f;
    private const float X = -60f;
    private const float Y = -220f;

    public string DisplayName => "Loading Images";
    public TweakStatus Status { get; set; }
    public void OnInitialize() { }

    public void OnEnable()
    {
        var excel = Service.Get<IDataManager>().Excel;
        territoryTypeSheet = excel.GetSheet<TerritoryType>();
    }

    public void OnDisable()
    {
        var ub = (AtkUnitBase*)Service.Get<IGameGui>().GetAddonByName("_LocationTitle");
        if (Service.Get<UiHelper>().IsAddonReady(ub))
        {
            var nd = ub->GetImageNodeById(ImageNodeId);
            if (nd != null)
            {
                Service.Get<UiHelper>().UnlinkAndFreeImageNode(nd, ub);
            }
        }
    }
      

    private AtkTextureResource* GetNodeResource(AtkImageNode *node) {
        try
        {
            if (node == null) return null;
            if (node->PartsList == null) return null;
            if (node->PartId >= node->PartsList->PartCount)
            {
                Log.LogError("Node has partID {partID} larger than partsList {partCount}", node->PartId, node->PartsList->PartCount);
                return null;
            }
            var asset = node->PartsList->Parts[node->PartId].UldAsset;
            return asset == null ? null : asset->AtkTexture.Resource;
        } catch (Exception e)
        {
            Log.LogError(e, "Unable to get node Resource");
            return null;
        }
    }

    private int GetNodeIconId(AtkImageNode* node)
    {
        try
        {
            var resource = GetNodeResource(node);
            if (resource == null) return -1;
            return (int)resource->IconId;
        }
        catch (Exception e)
        {
            Log.LogError(e, "Error getting tex icon id for node {NodeId}", node->NodeId);
            return -1;
        }
    }

    private string GetNodeTexturePath(AtkImageNode* node)
    {
        try
        {
            var resource = GetNodeResource(node);
            if (resource == null || resource->TexFileResourceHandle == null) return "";
            
            var name = resource->TexFileResourceHandle->ResourceHandle.FileName;
            return name.BufferPtr == null ? "" : name.ToString();
        }
        catch (Exception e)
        {
            Log.LogError(e, "Error getting tex path for node {NodeId}", node->NodeId);
            return "";
        }
    }


    [AddonPreDraw("_LocationTitle")]
    private void LocationTitleOnDraw(AddonEvent _, AddonArgs args)
    {
        try
        {
            var addon = (AtkUnitBase*)args.Addon;

            var iconId = GetNodeIconId(Service.Get<UiHelper>().GetNodeById<AtkImageNode>(addon, 4));
            if (iconId == -1) return;
            var terriZone = territoryTypeSheet.FirstOrDefault(x => x.PlaceNameIcon == iconId);
            if (terriZone.RowId == 0) return;

            // Don't display these for instance content
            if (Service.Get<IDataManager>().Excel.GetSheet<ContentFinderCondition>().Any(x => x.ContentLinkType == 1 && x.TerritoryType.RowId == terriZone.RowId)) return;

            var loadingImage = Service.Get<IDataManager>().Excel.GetSheet<LoadingImage>().FirstOrDefault(x => x.RowId == terriZone.LoadingImage.RowId);
            if (loadingImage.RowId == 0) return;

            var imageNode = GetImageNode(addon);
            if (imageNode == null) return;

            var texName = GetNodeTexturePath(imageNode);

            // we've already loaded this tex, so we're done
            if (texName.Contains(loadingImage.FileName.ToString())) return;

            imageNode->LoadTexture($"ui/loadingimage/{loadingImage.FileName.ToString()}_hr1.tex");
            Log.LogInformation("Replacing icon for territory {territory}", terriZone.RowId);
        }
        catch (Exception e)
        {
            Log.LogError(e, "Could not replace loading image.");
        }
    }

    private const int ImageNodeId = 93;
    private AtkImageNode* GetImageNode(AtkUnitBase* parent)
    {
        var nd = Service.Get<UiHelper>().GetNodeById<AtkImageNode>(parent, ImageNodeId);
        if (nd == null)
        {
            nd = Service.Get<UiHelper>().MakeImageNode(ImageNodeId, new UiHelper.PartInfo(0, 0, Width, Height));
            if (nd == null)
            {
                return nd;
            }
            nd->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;
            nd->WrapMode = 1;
            nd->Flags = (byte)ImageNodeFlags.AutoFit;
            nd->ToggleVisibility(true);
            nd->SetWidth(Width);
            nd->SetHeight(Height);
            nd->SetScale(ScaleX, ScaleY);
            nd->SetPositionFloat(X, Y);
            nd->SetPriority(0);

            Service.Get<UiHelper>().LinkNodeAfterTargetNode(nd, parent, Service.Get<UiHelper>().GetNodeById<AtkResNode>(parent, 6));
        }
        return nd;
    }

    public void Dispose() { }
}
