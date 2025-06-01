using System.IO;
using Dalamud.Plugin;
using Newtonsoft.Json;
using SalanthTweaks.Config;

namespace SalanthTweaks.Interfaces;

public interface IConfigurableTweak : ITweak
{
    public void DrawConfig();
}
