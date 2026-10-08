using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Core.Customization;

public sealed class WorkbenchCustomizationProfileService
{
    private const string DefaultProfileId = "profile-default";
    private const string DefaultProfileName = "기본 작업대";
    private readonly CommandRegistry _commandRegistry;
    private readonly WorkbenchCustomizationProfileStore _store;

    public WorkbenchCustomizationProfileService(string filePath, CommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
        _store = new WorkbenchCustomizationProfileStore(filePath, commandRegistry);
    }

    public async Task<WorkbenchCustomizationProfile> LoadOrCreateActiveProfileAsync(
        CancellationToken token,
        string? preferredProfileId = null)
    {
        var profiles = await _store.LoadProfilesAsync(token);
        if (profiles.Count == 0)
        {
            var defaultProfile = WorkbenchCustomizationProfileFactory.CreateDefault(
                DefaultProfileId,
                DefaultProfileName,
                _commandRegistry);
            await _store.SaveProfileAsync(defaultProfile, token);
            return defaultProfile;
        }

        if (!string.IsNullOrWhiteSpace(preferredProfileId))
        {
            var preferred = profiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, preferredProfileId, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return profiles[0];
    }

    public Task<IReadOnlyList<WorkbenchCustomizationProfile>> LoadProfilesAsync(CancellationToken token)
    {
        return _store.LoadProfilesAsync(token);
    }

    public Task SaveProfileAsync(WorkbenchCustomizationProfile profile, CancellationToken token)
    {
        return _store.SaveProfileAsync(profile, token);
    }
}
