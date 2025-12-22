using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using SalanthTweaks.Config;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Services;

[RegisterSingleton]
public sealed class TweakManager(IPluginLog PluginLog, PluginConfig PluginConfig, IEnumerable<ITweak> Tweaks, IGameInteropProvider GameInteropProvider)
{
    public void Initialize()
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Status == TweakStatus.Outdated)
                continue;

            try
            {
                PluginLog.Verbose($"Initializing {tweak.InternalName}");
                GameInteropProvider.InitializeFromAttributes(tweak);
                tweak.OnInitialize();
                tweak.Status = TweakStatus.Disabled;
            }
            catch (Exception ex)
            {
                tweak.Status = TweakStatus.InitializationFailed;
                PluginLog.Error(ex, $"[{tweak.InternalName}] Failed to initialize");
                continue;
            }

            if (!PluginConfig.EnabledTweaks.Contains(tweak.InternalName))
                continue;

            try
            {
                PluginLog.Verbose($"Enabling {tweak.InternalName}");
                EnableTweak(tweak);
                tweak.Status = TweakStatus.Enabled;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[{tweak.InternalName}] Failed to enable");
            }
        }
    }

    public void Shutdown()
    {
        foreach (var tweak in Tweaks) 
        {
            if (tweak.Status == TweakStatus.Enabled)
                DisableTweak(tweak, false);
        }
        
    }

    public void EnableTweak(ITweak tweak)
    {
        try
        {
            tweak.OnEnable();
            EnableAutoHooks(tweak);
            EnableAddonHooks(tweak);
        }
        catch (Exception ex)
        {
            tweak.Status = TweakStatus.InitializationFailed;
            PluginLog.Error(ex, "{}", $"[{tweak.InternalName}] Failed to enable");
        }
        tweak.Status = TweakStatus.Enabled;
        if (PluginConfig.EnabledTweaks.Add(tweak.InternalName))
            PluginConfig.Save();
    }

    public void DisableTweak(ITweak tweak, bool save = true)
    {
        DisableAddonHooks(tweak);
        DisableAutoHooks(tweak);
        try
        {
            tweak.OnDisable();
            tweak.Status = TweakStatus.Disabled;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "{}", $"[{tweak.InternalName}] Exception on disable");
            return;
        }
        if (save && PluginConfig.EnabledTweaks.Remove(tweak.InternalName))
            PluginConfig.Save();
    }
    
    private void EnableAutoHooks(ITweak tweak)
    {
        tweak.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Select(info => (info, info.GetCustomAttribute<Attributes.AutoHookAttribute>()))
             .ForEach(item =>
             {
                 PluginLog.Info($"{item.info} test enable");
                 if (item.Item2 is null || !item.Item2.Enable) return;
                 if (item.info.GetValue(tweak) is not IDalamudHook hook) return;
                 hook.GetType().GetMethod("Enable")?.Invoke(hook, null);
             });
        
        tweak.GetType().GetProperties().Select(info => (info, info.GetCustomAttribute<Attributes.AutoHookAttribute>()))
             .ForEach(item =>
             {
                 if (item.Item2 is null || !item.Item2.Enable) return;
                 if (item.info.GetValue(tweak) is not IDalamudHook hook) return;
                 hook.GetType().GetMethod("Enable")?.Invoke(hook, null);
             });
    }
    
    private static void DisableAutoHooks(ITweak tweak)
    {
        tweak.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Select(info => (info, info.GetCustomAttribute<Attributes.AutoHookAttribute>()))
             .ForEach(item =>
             {
                 if (item.Item2 is null || !item.Item2.Disable) return;
                 if (item.info.GetValue(tweak) is not IDalamudHook hook) return;
                 hook.GetType().GetMethod("Disable")?.Invoke(hook, null);
             });

        tweak.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Select(info => (info, info.GetCustomAttribute<Attributes.AutoHookAttribute>()))
             .ForEach(item =>
             {
                 if (item.Item2 is null || !item.Item2.Disable) return;
                 if (item.info.GetValue(tweak) is not IDalamudHook hook) return;
                 hook.GetType().GetMethod("Disable")?.Invoke(hook, null);
             });
    }


    private readonly IDictionary<ITweak, List<IAddonLifecycle.AddonEventDelegate>> eventHandlers = new Dictionary<ITweak, List<IAddonLifecycle.AddonEventDelegate>>();
    private void EnableAddonHooks(ITweak tweak)
    {
        var lifecycle = Service.Get<IAddonLifecycle>();
        tweak.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Select(info => (info, Attribute: info.GetCustomAttribute<Attributes.AddonEventAttribute>()))
             .ForEach(item =>
             {
                 if (item.Attribute is null) return;
                 item.Attribute.Event.ForEach(@event =>
                 {
                     if (!item.info.GetParameters().Select(pi => pi.ParameterType)
                              .SequenceEqual([typeof(AddonEvent), typeof(AddonArgs)]))
                         return;

                     if (item.info.ReturnType != typeof(void)) return;
                     
                     if (!eventHandlers.TryGetValue(tweak, out var handlers))
                     {
                         eventHandlers[tweak] = handlers = [];
                     }

                     var dg = void (AddonEvent ev, AddonArgs args) => item.info.Invoke(tweak, [ev, args]);
                     handlers.Add(dg.Invoke);

                     lifecycle.RegisterListener(@event, item.Attribute.AddonName, dg.Invoke);
                 });
             });
    }

    private void DisableAddonHooks(ITweak tweak)
    {
        var lifecycle = Service.Get<IAddonLifecycle>(); 
        if (eventHandlers.TryGetValue(tweak, out var handlers))
        {
            lifecycle.UnregisterListener(handlers.ToArray());
        }
        eventHandlers.Remove(tweak);
    }

    private interface IHook
    {
        void Enable();
        void Disable();
    }
}
