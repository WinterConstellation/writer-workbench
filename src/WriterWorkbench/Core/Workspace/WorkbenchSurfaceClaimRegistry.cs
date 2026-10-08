namespace WriterWorkbench.Core.Workspace;

public sealed class WorkbenchSurfaceClaimRegistry
{
    public const string MainOwnerId = "main-window";
    private readonly Dictionary<string, string> _surfaceOwners = new(StringComparer.OrdinalIgnoreCase);

    public bool TryClaim(string ownerId, string surfaceId, out string? occupiedBy)
    {
        ownerId = NormalizeRequired(ownerId, nameof(ownerId));
        surfaceId = NormalizeRequired(surfaceId, nameof(surfaceId));

        if (_surfaceOwners.TryGetValue(surfaceId, out var existingOwner) &&
            !string.Equals(existingOwner, ownerId, StringComparison.OrdinalIgnoreCase))
        {
            occupiedBy = existingOwner;
            return false;
        }

        ReleaseOwner(ownerId);
        _surfaceOwners[surfaceId] = ownerId;
        occupiedBy = null;
        return true;
    }

    public bool CanClaim(string ownerId, string surfaceId)
    {
        ownerId = NormalizeRequired(ownerId, nameof(ownerId));
        surfaceId = NormalizeRequired(surfaceId, nameof(surfaceId));
        return !_surfaceOwners.TryGetValue(surfaceId, out var existingOwner) ||
               string.Equals(existingOwner, ownerId, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsClaimed(string surfaceId)
    {
        return _surfaceOwners.ContainsKey(NormalizeRequired(surfaceId, nameof(surfaceId)));
    }

    public bool IsClaimedBy(string ownerId, string surfaceId)
    {
        ownerId = NormalizeRequired(ownerId, nameof(ownerId));
        surfaceId = NormalizeRequired(surfaceId, nameof(surfaceId));
        return _surfaceOwners.TryGetValue(surfaceId, out var existingOwner) &&
               string.Equals(existingOwner, ownerId, StringComparison.OrdinalIgnoreCase);
    }

    public void ReleaseOwner(string ownerId)
    {
        ownerId = NormalizeRequired(ownerId, nameof(ownerId));
        foreach (var surfaceId in _surfaceOwners
                     .Where(pair => string.Equals(pair.Value, ownerId, StringComparison.OrdinalIgnoreCase))
                     .Select(pair => pair.Key)
                     .ToList())
        {
            _surfaceOwners.Remove(surfaceId);
        }
    }

    public IReadOnlyList<WorkbenchSurfaceAvailability> GetAvailability(string ownerId)
    {
        ownerId = NormalizeRequired(ownerId, nameof(ownerId));
        return WorkbenchSurfaceCatalog.All
            .Select(option =>
            {
                var occupied = _surfaceOwners.TryGetValue(option.SurfaceId, out var existingOwner)
                    ? existingOwner
                    : null;
                var available = occupied is null ||
                                string.Equals(occupied, ownerId, StringComparison.OrdinalIgnoreCase);
                return new WorkbenchSurfaceAvailability(
                    option.SurfaceId,
                    option.Name,
                    option.Description,
                    available,
                    occupied);
            })
            .ToList();
    }

    private static string NormalizeRequired(string value, string name)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", name);
        }

        return normalized;
    }
}
