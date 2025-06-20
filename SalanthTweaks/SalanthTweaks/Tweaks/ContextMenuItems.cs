using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using SalanthTweaks.Attributes;
using SalanthTweaks.Config;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;
using Companion = Lumina.Excel.Sheets.Companion;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public partial class ContextMenuItems : ITweak
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    
    public string DisplayName => "Context Menu Items";
    public TweakStatus Status { get; set; }
 
    public void OnInitialize() { }

    public void OnEnable()
    {
        LoadConfig();
        var ctxMenu = Service.Get<IContextMenu>();
        ctxMenu.OnMenuOpened += AddMenus;
    }

    public void OnDisable()
    {
        var ctxMenu = Service.Get<IContextMenu>();
        ctxMenu.OnMenuOpened -= AddMenus;
    }

    public void AddMenus(IMenuOpenedArgs baseArgs)
    {
        if (Config.EnableWhatMount) WhatMountMenu(baseArgs);
        if (Config.EnableWhatBarding) WhatBardingMenu(baseArgs);
        if (Config.EnableWhatMinion) WhatMinionMenu(baseArgs);
    }
    
    private static string GetAddonText(uint id) => GetString<Addon>(id, r => r.Text);
    private static string GetString<T>(uint id, Func<T, ReadOnlySeString> extract) where T : struct, Lumina.Excel.IExcelRow<T>
    {
        return !Service.Get<IDataManager>().Excel.TryGetRow<T>(id, out var row) ? string.Empty : extract(row).ExtractText().StripSoftHyphen().FirstCharToUpper();
    }
    
    private static void WhatBardingMenu(IMenuOpenedArgs baseArgs)
    {
        if (baseArgs.MenuType != ContextMenuType.Default)  return;
        var argTarget = baseArgs.Target as MenuTargetDefault;
        if (argTarget?.TargetObject is not { ObjectKind: ObjectKind.BattleNpc, SubKind: (byte)BattleNpcSubKind.Chocobo }) return;

        baseArgs.AddMenuItem(new MenuItem
        {
            Name = "What Barding",
            IsEnabled = true,
            OnClicked = WhatBarding,
            Prefix = SeIconChar.BoxedLetterS,
            PrefixColor = 9
        });

    }

    private static unsafe void WhatBarding(IMenuItemClickedArgs baseArgs)
    {
        var argTarget = baseArgs.Target as MenuTargetDefault;
        if (argTarget?.TargetObject is not { ObjectKind: ObjectKind.BattleNpc, SubKind: (byte)BattleNpcSubKind.Chocobo }) return;

        var character = (Character *)argTarget.TargetObject.Address;
        var excel = Service.Get<IDataManager>().Excel;
        var hasHead = excel.TryGetRow<BuddyEquip>(r => r.ModelTop == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Value, out var head);
        var hasBody = excel.TryGetRow<BuddyEquip>(r => r.ModelTop == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body).Value, out var body);
        var hasLegs = excel.TryGetRow<BuddyEquip>(r => r.ModelTop == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Value, out var legs);
        var hasStain = excel.TryGetRow<Stain>(character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Stain0, out var stain);
        var name = new SeStringBuilder().PushColorType(1).Append(argTarget.TargetName).PopColorType().ToReadOnlySeString();

        var sb = new SeStringBuilder().AppendSalanthTweaksPrefix()
                             .Append("Appearance of ").Append(name).Append(":").AppendNewLine()
                             .Append($"  {GetAddonText(4987)}: ")
                             .Append(hasStain ? stain.Name.ExtractText().FirstCharToUpper() : "??").AppendNewLine()
                             .Append($"  {GetAddonText(4991)}: {(hasHead ? head.Name.ExtractText() : GetAddonText(4994))}").AppendNewLine()
                             .Append($"  {GetAddonText(4992)}: {(hasBody ? body.Name.ExtractText() : GetAddonText(4994))}").AppendNewLine()
                             .Append($"  {GetAddonText(4993)}: {(hasLegs ? legs.Name.ExtractText() : GetAddonText(4994))}").AppendNewLine();

        Service.Get<IChatGui>().Print(sb.GetViewAsSpan());
    }
    
    private static unsafe void WhatMountMenu(IMenuOpenedArgs baseArgs)
    {
        if (baseArgs.MenuType != ContextMenuType.Default)  return;
        var argTarget = baseArgs.Target as MenuTargetDefault;
        if (argTarget?.TargetObject is not { ObjectKind: ObjectKind.Player }) return;

        var gameObj = (Character*)argTarget.TargetObject.Address;
        if (gameObj->IsNotMounted()) return;

        baseArgs.AddMenuItem(new MenuItem
        {
            Name = "What Mount",
            IsEnabled = true,
            OnClicked = WhatMount,
            Prefix = SeIconChar.BoxedLetterS,
            PrefixColor = 9
        });
    }

    private static unsafe void WhatMount(IMenuItemClickedArgs baseArgs)
    {
        var argTarget = baseArgs.Target as MenuTargetDefault;
        var gameObj = (Character*)argTarget!.TargetObject!.Address;

        var excel = Service.Get<IDataManager>().Excel;
        if (!excel.GetSheet<Mount>().TryGetRow(gameObj->Mount.MountId, out var mount))
            return;
        
        var name = new SeStringBuilder().PushColorType(1)
                                        .Append(mount.Singular.ExtractText().StripSoftHyphen().FirstCharToUpper()).PopColorType().ToReadOnlySeString();
        var sb = new SeStringBuilder().AppendSalanthTweaksPrefix();

        // ReSharper disable twice AssignmentInConditionalExpression
        bool hasItem;
        if (hasItem = excel.TryGetRow<ItemAction>(r => r.Type == 1322 && r.Data[0] == mount.RowId,
                                                  out var itemAction))
        {

            if (hasItem = excel.TryGetRow<Item>(r => r.ItemAction.RowId == itemAction.RowId, out var item))
            {
                sb.Append("Mount ").Append(name).Append(" is taught by ")
                  .Append(Service.Get<ItemService>().GetItemLink(item));
            }
        }

        if (!hasItem) sb.Append("Mount: ").Append(name);
        Service.Get<IChatGui>().Print(sb.GetViewAsSpan());

    }
    
    private static void WhatMinionMenu(IMenuOpenedArgs baseArgs)
    {
        if (baseArgs.MenuType != ContextMenuType.Default)  return;
        var argTarget = baseArgs.Target as MenuTargetDefault;
        if (argTarget?.TargetObject is not { ObjectKind: ObjectKind.Companion }) return;

        baseArgs.AddMenuItem(new MenuItem
        {
            Name = "What Minion",
            IsEnabled = true,
            OnClicked = WhatMinion,
            Prefix = SeIconChar.BoxedLetterS,
            PrefixColor = 9
        });

    }
    
    private static unsafe void WhatMinion(IMenuItemClickedArgs baseArgs)
    {
        var argTarget = baseArgs.Target as MenuTargetDefault;
        if (argTarget?.TargetObject is not { ObjectKind: ObjectKind.Companion }) return;

        var gameObj = (GameObject *)argTarget.TargetObject.Address;
        var excel = Service.Get<IDataManager>().Excel;

        if (!excel.TryGetRow<Companion>(gameObj->BaseId, out var companion)) return;

        var name = new SeStringBuilder().PushColorType(1).Append(companion.Singular.ExtractText().FirstCharToUpper())
                                    .PopColorType().ToReadOnlySeString();
        var sb = new SeStringBuilder().AppendSalanthTweaksPrefix().Append("Minion ").Append(name);
       
            
        if (excel.TryGetRow<ItemAction>(r => r.Type == 853 && r.Data[0] == companion.RowId,
                                         out var itemAction))
        {

            if (excel.TryGetRow<Item>(r => r.ItemAction.RowId == itemAction.RowId, out var item))
            {
                sb.Append(" is taught by ").Append(Service.Get<ItemService>().GetItemLink(item));
            }
        }
        Service.Get<IChatGui>().Print(sb.GetViewAsSpan());
    }

    public class CtxMenuConfig : TweakConfig
    {
        private const int CurrentVersion = 1;
        public bool EnableWhatMount;
        public bool EnableWhatBarding;
        public bool EnableWhatMinion;

        public override bool Update()
        {
            if (Version >= CurrentVersion) return false;
            while (Version < CurrentVersion)
            {
                switch (Version)
                {
                    case 0:
                        // Apply v1 migrations
                        break;
                }

                Version++;
            }

            return true;
        }
    }

    [TweakConfig]
    public CtxMenuConfig Config { get; set; } = null!;
    
    public void DrawConfig()
    {
        if (ImGui.Checkbox("WhatMount", ref Config.EnableWhatMount))
        {
            SaveConfig();
        }
        if (ImGui.Checkbox("WhatBarding", ref Config.EnableWhatBarding))
        {
            SaveConfig(); 
        }
        if (ImGui.Checkbox("WhatMinion", ref Config.EnableWhatMinion))
        {
            SaveConfig(); 
        }
    }
}
