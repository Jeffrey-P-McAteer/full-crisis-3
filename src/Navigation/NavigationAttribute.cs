using System;

namespace FullCrisis3.Navigation;

/// <summary>
/// Marks a control for automatic navigation discovery
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NavigationOrderAttribute : Attribute
{
    public int Order { get; }
    public int GridRow { get; set; } = -1;
    public int GridColumn { get; set; } = -1;
    public bool IsDefault { get; set; } = false;

    public NavigationOrderAttribute(int order)
    {
        Order = order;
    }
}