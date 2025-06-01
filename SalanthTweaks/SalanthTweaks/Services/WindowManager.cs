using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public class WindowManager : IDisposable
{

    private readonly WindowSystem WindowSystem;
    private readonly IDalamudPluginInterface PluginInterface;

    public WindowManager(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        WindowSystem = new WindowSystem(pluginInterface.InternalName);
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
    }

    public void AddWindow(Window window) => WindowSystem.AddWindow(window);


    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        WindowSystem.Windows.OfType<IDisposable>().ForEach(window => window.Dispose());
        WindowSystem.RemoveAllWindows();
    }
}
