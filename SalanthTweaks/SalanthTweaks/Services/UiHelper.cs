using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Logging;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace SalanthTweaks.Services;

[RegisterSingleton]
public unsafe class UiHelper(ILogger<UiHelper> Log) : IDisposable
{

    public bool IsAddonReady(AtkUnitBase* addon)
    {
        if (addon is null) return false;
        if (addon->RootNode is null) return false;
        return addon->RootNode->ChildNode is not null;
    }

    public T* GetNodeById<T>(AtkUnitBase* unitBase, uint nodeId, NodeType? type = null) where T : unmanaged => GetNodeById<T>(&unitBase->UldManager, nodeId, type);
    public T* GetNodeById<T>(AtkComponentBase* component, uint nodeId, NodeType? type = null) where T : unmanaged => GetNodeById<T>(&component->UldManager, nodeId, type);
    public T* GetNodeById<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
        if (uldManager == null) return null;
        if (uldManager->NodeList == null) return null;
        for (var i = 0; i < uldManager->NodeListCount; i++) {
            var n = uldManager->NodeList[i];
            if (n == null || n->NodeId != nodeId || type != null && n->Type != type.Value) continue;
            return (T*)n;
        }

        return null;
    }

    public record PartInfo(ushort U, ushort V, ushort Width, ushort Height);

    public AtkImageNode* MakeImageNode(uint id, PartInfo partInfo)
    {
        if (!TryMakeImageNode(id, 0, 0, 0, 0, out var imageNode))
        {
            Log.LogError("Failed to alloc memory for AtkImageNode.");
            return null;
        }

        if (!TryMakePartsList(0, out var partsList))
        {
            Log.LogError("Failed to alloc memory for AtkUldPartsList.");
            FreeImageNode(imageNode);
            return null;
        }

        if (!TryMakePart(partInfo.U, partInfo.V, partInfo.Width, partInfo.Height, out var part))
        {
            Log.LogError("Failed to alloc memory for AtkUldPart.");
            FreePartsList(partsList);
            FreeImageNode(imageNode);
            return null;
        }

        if (!TryMakeAsset(0, out var asset))
        {
            Log.LogError("Failed to alloc memory for AtkUldAsset.");
            FreePart(part);
            FreePartsList(partsList);
            FreeImageNode(imageNode);
        }

        part->UldAsset = asset;
        AddPart(partsList, part);
        imageNode->PartsList = partsList;

        return imageNode;
    }
    
    public bool TryMakeImageNode(uint id, NodeFlags resNodeFlags, uint resNodeDrawFlags, byte wrapMode, byte imageNodeFlags, [NotNullWhen(true)] out AtkImageNode* imageNode)
    {
        imageNode = AtkUldManager.CreateAtkImageNode();

        if (imageNode is not null)
        {
            imageNode->AtkResNode.Type = NodeType.Image;
            imageNode->AtkResNode.NodeId = id;
            imageNode->AtkResNode.NodeFlags = resNodeFlags;
            imageNode->AtkResNode.DrawFlags = resNodeDrawFlags;
            imageNode->WrapMode = wrapMode;
            imageNode->PartsList = null;
            imageNode->PartId = 0;
            imageNode->Flags = imageNodeFlags;
            return true;
        }

        return false;
    }    
    
    public bool TryMakePartsList(uint id, [NotNullWhen(true)] out AtkUldPartsList* partsList)
    {
        partsList = (AtkUldPartsList*) IMemorySpace.GetUISpace()->Malloc((ulong) sizeof(AtkUldPartsList), 8);

        if (partsList is not null)
        {
            partsList->Id = id;
            partsList->PartCount = 0;
            partsList->Parts = null;
            return true;
        }

        return false;
    }
    
    public bool TryMakePart(ushort u, ushort v, ushort width, ushort height, [NotNullWhen(true)] out AtkUldPart* part)
    {
        part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8);

        if (part is not null)
        {
            part->U = u;
            part->V = v;
            part->Width = width;
            part->Height = height;
            return true;
        }

        return false;
    }

    public bool TryMakeAsset(uint id, [NotNullWhen(true)] out AtkUldAsset* asset)
    {
        asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8);

        if (asset is not null)
        {
            asset->Id = id;
            asset->AtkTexture.Ctor();
            return true;
        }

        return false;
    }
    
    public void AddPart(AtkUldPartsList* partsList, AtkUldPart* part)
    {
        // copy pointer to old array
        var oldPartArray = partsList->Parts;
        
        // allocate space for new array
        var newSize = partsList->PartCount + 1;
        var newArray = (AtkUldPart*) IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart) * newSize, 8);

        if (oldPartArray is not null)
        {
            // copy each member of old array2
            foreach (var index in Enumerable.Range(0, (int)partsList->PartCount))
            {
                Buffer.MemoryCopy(oldPartArray + index, newArray + index, sizeof(AtkUldPart), sizeof(AtkUldPart));
            }
        
            // free old array
            IMemorySpace.Free(oldPartArray, (ulong)sizeof(AtkUldPart) * partsList->PartCount);
        }
        
        // add new part
        Buffer.MemoryCopy(part, newArray + (newSize - 1), sizeof(AtkUldPart), sizeof(AtkUldPart));
        partsList->Parts = newArray;
        partsList->PartCount = newSize;
    }
    
    public void LinkNodeAfterTargetNode<T>(T* atkNode, AtkUnitBase* parent, AtkResNode* targetNode) where T : unmanaged {
        var node = (AtkResNode*)atkNode;
        var prev = targetNode->PrevSiblingNode;
        node->ParentNode = targetNode->ParentNode;

        targetNode->PrevSiblingNode = node;
        prev->NextSiblingNode = node;

        node->PrevSiblingNode = prev;
        node->NextSiblingNode = targetNode;

        parent->UldManager.UpdateDrawNodeList();
    }
    
    public void UnlinkAndFreeImageNode(AtkImageNode* node, AtkUnitBase* parent)
    {
        if (node->AtkResNode.PrevSiblingNode is not null)
            node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
            
        if (node->AtkResNode.NextSiblingNode is not null)
            node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
            
        parent->UldManager.UpdateDrawNodeList();

        FreePartsList(node->PartsList);
        FreeImageNode(node);
    }

    public void FreeImageNode(AtkImageNode* node)
    {
        node->AtkResNode.Destroy(false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkImageNode));
    }
    
    public void FreePartsList(AtkUldPartsList* partsList)
    {
        foreach (var index in Enumerable.Range(0, (int)partsList->PartCount))
        {
            var part = &partsList->Parts[index];
            
            FreeAsset(part->UldAsset);
            FreePart(part);
        }
        
        IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
    }
    
    public void FreePart(AtkUldPart* part)
    {
        IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
    }
    
    public void FreeAsset(AtkUldAsset* asset)
    {
        IMemorySpace.Free(asset, (ulong) sizeof(AtkUldAsset));
    }

    public void Dispose()
    {
    }
}
