using LugarLang.Internal.Models;
using LugarLang.Internal.Models.Discovery;
using LugarLang.Internal.Services.Content;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LugarLang.Mobile.Services;

// Client-side DTOs matching LugarLang.Api.Models exactly (field-for-field
// mirrors of the real DiscoveryCategory/Place/PlaceContacts/PlacePayments).
// Kept separate from the Internal model classes so this file has no
// dependency on API-side code — only on the real Internal models it
// converts into/from.

public sealed class ContentBundleResponse
{
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public List<PlaceDto> Places { get; set; } = new();
}

public sealed class ContentVersionResponse
{
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public DiscoveryCategory ToDiscoveryCategory() => new()
    {
        Id = Id,
        Name = Name,
        Icon = Icon,
        DisplayOrder = DisplayOrder,
        IsEnabled = IsEnabled,
    };
}

public sealed class PlaceContactsDto
{
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string? PhoneNumber { get; set; }

    public PlaceContacts ToPlaceContacts() => new()
    {
        Facebook = Facebook,
        Instagram = Instagram,
        WhatsApp = WhatsApp,
        PhoneNumber = PhoneNumber,
    };
}



public sealed class PlaceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }

    // Remote URLs as sent by the API — NOT local file paths yet. Kept
    // distinct from Place.Photos (which the rest of the app treats as
    // local file paths via ImageSource.FromFile) until PhotoCacheService
    // has downloaded each one and substituted a local path.
    public List<string> Photos { get; set; } = new();

    public PlaceContactsDto Contacts { get; set; } = new();
    public string Description { get; set; } = string.Empty;

    public Place ToPlace() => new()
    {
        Id = Id,
        Name = Name,
        Category = Category,
        CategoryId = CategoryId,
        Region = Region,
        Latitude = Latitude,
        Longitude = Longitude,
        ImageUrl = ImageUrl,
        IsFeatured = IsFeatured,
        FeaturedOrder = FeaturedOrder,
        Photos = Photos, // remote URLs for now — PhotoCacheService replaces these before save
        Contacts = Contacts.ToPlaceContacts(),
        Description = Description,
    };
}

/// <summary>
/// Downloads each place's photos from their remote URLs and caches them as
/// local files, so the rest of the app can keep using
/// ImageSource.FromFile(place.Photos[0]) unchanged. Skips anything already
/// cached — a sync should only download what's actually new.
/// </summary>
public sealed class PhotoCacheService
{
    private readonly HttpClient httpClient;
    private readonly string cacheFolder;

    public PhotoCacheService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        cacheFolder = Path.Combine(FileSystem.AppDataDirectory, "PhotoCache");
        Directory.CreateDirectory(cacheFolder);
    }

    /// <summary>
    /// Replaces each place's Photos entries with local cached file paths,
    /// downloading only the ones not already present on disk. A photo URL
    /// that fails to download is left out of the result rather than
    /// blocking the rest of the sync.
    /// </summary>
    public async Task CacheAllAsync(List<Place> places, CancellationToken ct = default)
    {
        foreach (Place place in places)
        {
            List<string> localPaths = new();

            foreach (string photoRef in place.Photos)
            {
                // Already a local path from a previous sync (or never touched
                // by sync at all) — leave it alone rather than re-caching.
                if (!photoRef.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !photoRef.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    localPaths.Add(photoRef);
                    continue;
                }

                string? cachedPath = await GetOrDownloadAsync(photoRef, ct);
                if (cachedPath is not null)
                {
                    localPaths.Add(cachedPath);
                }
            }

            place.Photos = localPaths;
        }
    }

    private async Task<string?> GetOrDownloadAsync(string photoUrl, CancellationToken ct)
    {
        string fileName = HashUrlToFileName(photoUrl);
        string localPath = Path.Combine(cacheFolder, fileName);

        if (File.Exists(localPath))
        {
            return localPath; // already cached — no network call
        }

        try
        {
            byte[] bytes = await httpClient.GetByteArrayAsync(photoUrl, ct);
            await File.WriteAllBytesAsync(localPath, bytes, ct);
            return localPath;
        }
        catch
        {
            return null; // this one photo failed — don't fail the whole sync over it
        }
    }

    private static string HashUrlToFileName(string url)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        string hex = Convert.ToHexString(hash).ToLowerInvariant();
        string extension = Path.GetExtension(new Uri(url).AbsolutePath);
        if (string.IsNullOrEmpty(extension) || extension.Length > 5)
        {
            extension = ".jpg"; // reasonable default if the URL has no clear extension
        }

        return hex + extension;
    }
}

/// <summary>
/// Pulls category/place content from LugarLang.Api and applies it through
/// the real CategoryContentService/PlaceContentService (via their
/// ReplaceAll methods). Photos are cached locally by PhotoCacheService
/// before being saved, so Place.Photos always holds local file paths by
/// the time it reaches PlaceContentService.
/// </summary>
public sealed class ContentSyncService
{
    private const string LastVersionFileName = "content-sync-version.json";
    private const string LastCheckFileName = "content-sync-last-check.json";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(7);

    private readonly HttpClient httpClient;
    private readonly CategoryContentService categoryContentService;
    private readonly PlaceContentService placeContentService;
    private readonly PhotoCacheService photoCacheService;
    private readonly string versionFilePath;
    private readonly string lastCheckFilePath;

    public ContentSyncService(
        HttpClient httpClient,
        CategoryContentService categoryContentService,
        PlaceContentService placeContentService)
    {
        this.httpClient = httpClient;
        this.categoryContentService = categoryContentService;
        this.placeContentService = placeContentService;
        photoCacheService = new PhotoCacheService(httpClient);
        versionFilePath = Path.Combine(FileSystem.AppDataDirectory, LastVersionFileName);
        lastCheckFilePath = Path.Combine(FileSystem.AppDataDirectory, LastCheckFileName);
    }

    /// <summary>
    /// Call on launch and/or pull-to-refresh. By default, only actually
    /// checks the network once every CheckInterval (7 days) — pass
    /// force: true to bypass that throttle, e.g. from a manual Refresh
    /// button. Never throws; any network failure is treated as "nothing to
    /// update" so it can't block app launch or wipe local content.
    /// </summary>
    public async Task<bool> SyncAsync(bool force = false, CancellationToken ct = default)
    {
        if (!force && !IsCheckDue())
        {
            return false;
        }

        ContentVersionResponse? remoteVersion = await GetRemoteVersionAsync(ct);
        if (remoteVersion is null)
        {
            return false; // offline/unreachable — don't update last-check, try again next time
        }

        WriteLastCheck();

        string? localVersion = ReadLocalVersion();
        if (localVersion == remoteVersion.Version)
        {
            return false;
        }

        ContentBundleResponse? bundle = await GetFullBundleAsync(ct);
        if (bundle is null)
        {
            return false;
        }

        List<Place> newPlaces = bundle.Places.Select(p => p.ToPlace()).ToList();
        await photoCacheService.CacheAllAsync(newPlaces, ct);

        categoryContentService.ReplaceAll(
            bundle.Categories.Select(c => c.ToDiscoveryCategory()).ToList());

        placeContentService.ReplaceAll(newPlaces);

        WriteLocalVersion(bundle.Version);
        return true;
    }

    private bool IsCheckDue()
    {
        if (!File.Exists(lastCheckFilePath))
        {
            return true;
        }

        try
        {
            string text = File.ReadAllText(lastCheckFilePath);
            if (!DateTimeOffset.TryParse(text, out DateTimeOffset lastCheck))
            {
                return true;
            }

            return DateTimeOffset.UtcNow - lastCheck >= CheckInterval;
        }
        catch
        {
            return true; // corrupt/unreadable — safest to just check
        }
    }

    private void WriteLastCheck()
    {
        try
        {
            File.WriteAllText(lastCheckFilePath, DateTimeOffset.UtcNow.ToString("O"));
        }
        catch
        {
            // non-critical — worst case, the throttle re-checks slightly more often
        }
    }

    private async Task<ContentVersionResponse?> GetRemoteVersionAsync(CancellationToken ct)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ContentVersionResponse>("/api/content/version", ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ContentBundleResponse?> GetFullBundleAsync(CancellationToken ct)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ContentBundleResponse>("/api/content", ct);
        }
        catch
        {
            return null;
        }
    }

    private string? ReadLocalVersion()
    {
        if (!File.Exists(versionFilePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(versionFilePath);
            ContentVersionResponse? info = JsonSerializer.Deserialize<ContentVersionResponse>(json);
            return info?.Version;
        }
        catch
        {
            return null;
        }
    }

    private void WriteLocalVersion(string version)
    {
        ContentVersionResponse info = new()
        {
            Version = version,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        string json = JsonSerializer.Serialize(info);
        File.WriteAllText(versionFilePath, json);
    }
}