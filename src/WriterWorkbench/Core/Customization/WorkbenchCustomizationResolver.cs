namespace WriterWorkbench.Core.Customization;

public sealed class WorkbenchCustomizationResolver(WorkbenchCustomizationProfile profile)
{
    public IReadOnlyList<CommandPlacement> GetPlacements(string surface, string area)
    {
        return profile.Placements
            .Where(placement =>
                string.Equals(placement.Surface, surface, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(placement.Area, area, StringComparison.OrdinalIgnoreCase))
            .OrderBy(placement => placement.Order)
            .ThenBy(placement => placement.SlotKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<CommandInvocation> GetMacroSteps(string macroId)
    {
        return profile.Macros
            .SingleOrDefault(macro => string.Equals(macro.Id, macroId, StringComparison.OrdinalIgnoreCase))
            ?.Steps
            .ToList()
            ?? [];
    }
}
