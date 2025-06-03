using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public class WindowManager : IDisposable
{

    private readonly WindowSystem windowSystem;
    private readonly IDalamudPluginInterface pluginInterface;

    public WindowManager(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        windowSystem = new WindowSystem(pluginInterface.InternalName);
        this.pluginInterface.UiBuilder.Draw += windowSystem.Draw;
    }

    public void AddWindow(Window window) => windowSystem.AddWindow(window);


    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        windowSystem.Windows.OfType<IDisposable>().ForEach(window => window.Dispose());
        windowSystem.RemoveAllWindows();
    }
}
