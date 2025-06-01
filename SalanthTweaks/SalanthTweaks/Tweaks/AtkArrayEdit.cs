using System.Globalization;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;
using SalanthTweaks.Services;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class AtkArrayEdit : ITweak
{
    private IChatGui? ChatGui;
    private CommandService? CommandService;
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
    
    public string DisplayName => "AtkArrayEdit";
    public TweakStatus Status { get; set; }
    public void OnInitialize()
    {
        ChatGui = Service.Get<IChatGui>();
        CommandService = Service.Get<CommandService>();
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
    
    [Command("/atk", "View and edit atk values", AutoEnable: true)]
    public unsafe void OnCommand(string command, string args)
    {
        // /atk [ns] # # 
        var parts = args.Split(' ', 4);
        if (parts.Length < 3)
        {
            ChatGui?.PrintError("Invalid arguments");
            return;
        }

        var set = parts.Length == 4;
        var kind = parts[0];
        if (kind is not ("n" or "s"))
        {
            ChatGui?.PrintError("Invalid AtkArray type");
            return;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var arrayNum))
        {
            ChatGui?.PrintError("Invalid array number");
            return;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var arrayIndex))
        {
            ChatGui?.PrintError("Invalid array index");
            return;
        }

        string message;
        var arrayHolder = AtkStage.Instance()->AtkArrayDataHolder;
        if (kind == "n")
        {
            if (arrayNum < 0 || arrayNum >= arrayHolder->NumberArrayCount)
            {
                ChatGui?.PrintError("Array number is out of range");
                return;
            }

            var array = arrayHolder->GetNumberArrayData(arrayNum);
            if (arrayIndex < 0 || arrayIndex >= array->Size)
            {
                ChatGui?.PrintError("Array index is out of range");
            }

            message = $"{kind}{arrayNum}.{arrayIndex}: {array->IntArray[arrayIndex]}";

            if (set)
            {
                var useHex = parts[3].StartsWith("0x");
                if (int.TryParse(parts[3], useHex ? NumberStyles.HexNumber : NumberStyles.None, CultureInfo.InvariantCulture, out var newValue))
                {
                    message += $" -> {newValue}";
                    array->SetValue(arrayIndex, newValue);
                }
            }
        }
        else
        {
            if (arrayNum < 0 || arrayNum >= arrayHolder->StringArrayCount)
            {
                ChatGui?.PrintError("Array number is out of range");
                return;
            }

            var array = arrayHolder->GetStringArrayData(arrayNum);
            if (arrayIndex < 0 || arrayIndex >= array->Size)
            {
                ChatGui?.PrintError("Array index is out of range");
            }
            var str = array->StringArray[arrayIndex];
            message = $"{kind}{arrayNum}.{arrayIndex}: {(str == null ? "\u2400" : Utf8String.FromSequence(str)->ToString())}";

            if (set)
            {
                if (parts[3] == "!!CLEAR")
                {
                    array->SetValue(arrayIndex, null);
                }
                else
                {
                    array->SetValue(arrayIndex, parts[3]);
                }
            }
        }
        ChatGui?.Print(message, "AtkEdit");
    }
}
