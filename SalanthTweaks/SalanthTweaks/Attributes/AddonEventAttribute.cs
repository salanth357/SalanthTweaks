using Dalamud.Game.Addon.Lifecycle;
using JetBrains.Annotations;
namespace SalanthTweaks.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[MeansImplicitUse]
public class AddonEventAttribute(string addonName, params AddonEvent[] events) : Attribute
{
    public AddonEventAttribute(AddonEvent @event, string addonName) : this(addonName, @event) { }

    public AddonEvent[] Event { get; } = events;
    public string AddonName { get; } = addonName;
}

public class AddonPreSetupAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PreSetup, addonNames);

public class AddonPostSetupAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PostSetup, addonNames);

public class AddonFinalizeAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PreFinalize, addonNames);

public class AddonPreUpdateAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PreUpdate, addonNames);

public class AddonPostUpdateAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PostUpdate, addonNames);

public class AddonPreRequestedUpdateAttribute(string addonNames)
    : AddonEventAttribute(AddonEvent.PreRequestedUpdate, addonNames);

public class AddonPostRequestedUpdateAttribute(string addonNames)
    : AddonEventAttribute(AddonEvent.PostRequestedUpdate, addonNames);

public class AddonPreDrawAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PreDraw, addonNames);

public class AddonPostDrawAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PostDraw, addonNames);

public class AddonPreRefreshAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PreRefresh, addonNames);

public class AddonPostRefreshAttribute(string addonNames) : AddonEventAttribute(AddonEvent.PostRefresh, addonNames);

public class AddonPreReceiveEventAttribute(string addonNames)
    : AddonEventAttribute(AddonEvent.PreReceiveEvent, addonNames);

public class AddonPostReceiveEventAttribute(string addonNames)
    : AddonEventAttribute(AddonEvent.PostReceiveEvent, addonNames);
