using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace SalanthTweaks.Extensions;

public static class AtkTools
{
    public static unsafe void VisitChildren(AtkResNode* root, Func<Pointer<AtkResNode>, bool> visitAction)
    {
        if (root == null) return;
        var visitChildren = visitAction(root);
        if (visitChildren)
        {
            var nd = root->GetComponent() == null ? root->ChildNode : root->GetComponent()->AtkResNode;
            for (; nd != null; nd = nd->PrevSiblingNode)
            {
                VisitChildren(nd, visitAction);
            }
        }
    }
}
