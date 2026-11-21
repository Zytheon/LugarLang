
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using NetTopologySuite.Geometries;
using Microsoft.Maui.Storage;
using SkiaSharp;
using System.Security.Cryptography;
using System.Text;

using PlaceModel = LugarLang.Internal.Models.Place;

namespace LugarLang.Mobile.Services.Mapping;

public sealed class JourneyPhotoMarker
{
    public required PlaceModel Place { get; init; }
    public required string Caption { get; init; }
    public required bool IsEndpoint { get; init; }
}

public sealed class JourneyPhotoMarkerService
{
    private const int MarkerWidth = 128;
    private const int MarkerHeight = 160;

    private const float PhotoCenterX = 64;
    private const float PhotoCenterY = 58;
    private const float PhotoRadius = 34;

    public (
        MemoryLayer Layer,
        Dictionary<IFeature, JourneyPhotoMarker> MarkerLookup)
        CreateLayer(
            IEnumerable<JourneyPhotoMarker> markers,
            double endpointTargetHeight,
            double landmarkTargetHeight)
    {
        List<IFeature> features = new();

        Dictionary<IFeature, JourneyPhotoMarker> lookup = new();

        foreach (JourneyPhotoMarker marker in markers)
        {
            PlaceModel place = marker.Place;

            // Validate geographic coordinates.
            if (!double.IsFinite(place.Latitude) ||
                !double.IsFinite(place.Longitude) ||
                place.Latitude < -90 ||
                place.Latitude > 90 ||
                place.Longitude < -180 ||
                place.Longitude > 180)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PHOTO MARKER] Invalid coordinates: {place.Name}");

                continue;
            }

            var projected =
                Mapsui.Projections.SphericalMercator.FromLonLat(
                    place.Longitude,
                    place.Latitude);

            GeometryFeature feature = new()
            {
                Geometry = new NetTopologySuite.Geometries.Point(
                    projected.x,
                    projected.y)
            };

            string? markerImagePath = null;

            // Try available photos until a usable image is found.
            if (place.Photos is not null)
            {
                foreach (string photoPath in place.Photos)
                {
                    if (string.IsNullOrWhiteSpace(photoPath) ||
                        !File.Exists(photoPath))
                    {
                        continue;
                    }

                    if (TryCreatePhotoPin(
                        photoPath,
                        out string generatedPath))
                    {
                        markerImagePath = generatedPath;
                        break;
                    }
                }
            }

            if (markerImagePath is not null)
            {
                double targetSize =
                    marker.IsEndpoint
                        ? endpointTargetHeight
                        : landmarkTargetHeight;

                feature.Styles.Add(
                    new ImageStyle
                    {
                        Image = new Mapsui.Styles.Image
                        {
                            Source = new Uri(
                                Path.GetFullPath(markerImagePath))
                                .AbsoluteUri
                        },
                        SymbolScale = targetSize / MarkerHeight
                    });
            }
            else
            {
                // Fallback for missing or unreadable photos.
                feature.Styles.Add(
                    ImageStyles.CreatePinStyle(
                        marker.IsEndpoint
                            ? Mapsui.Styles.Color.FromString("#486F91")
                            : Mapsui.Styles.Color.FromString("#7895AD"),
                        Mapsui.Styles.Color.White,
                        marker.IsEndpoint ? 1.0 : 0.75));
            }

            System.Diagnostics.Debug.WriteLine(
                $"[PHOTO MARKER] {place.Name}");

            System.Diagnostics.Debug.WriteLine(
                $"[PHOTO MARKER] Photo: {markerImagePath ?? "(fallback pin)"}");

            System.Diagnostics.Debug.WriteLine(
                $"[PHOTO MARKER] Styles assigned: {feature.Styles.Count}");

            features.Add(feature);
            lookup[feature] = marker;
        }

        MemoryLayer layer = new()
        {
            Name = "Selected Journey Photo Markers",
            Features = features
        };

        System.Diagnostics.Debug.WriteLine(
            $"[PHOTO MARKER] Final feature count: {features.Count}");

        return (layer, lookup);
    }

    private static bool TryCreatePhotoPin(
        string photoPath,
        out string generatedPath)
    {
        generatedPath = string.Empty;

        try
        {
            string absolutePhotoPath =
                Path.GetFullPath(photoPath);

            if (!File.Exists(absolutePhotoPath))
                return false;

            DateTime lastModified =
                File.GetLastWriteTimeUtc(absolutePhotoPath);

            string cacheKey =
                $"{absolutePhotoPath}|{lastModified.Ticks}";

            string hash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(cacheKey)));

            string cacheDirectory = Path.Combine(
                FileSystem.CacheDirectory,
                "LugarLang",
                "JourneyMarkers");

            Directory.CreateDirectory(cacheDirectory);

            generatedPath = Path.Combine(
                cacheDirectory,
                $"{hash}.png");

            if (File.Exists(generatedPath))
                return true;

            using SKBitmap? photo =
                SKBitmap.Decode(absolutePhotoPath);

            if (photo is null ||
                photo.Width <= 0 ||
                photo.Height <= 0)
            {
                generatedPath = string.Empty;
                return false;
            }

            using SKBitmap markerBitmap = new(
                MarkerWidth,
                MarkerHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);

            using SKCanvas canvas = new(markerBitmap);

            canvas.Clear(SKColors.Transparent);

            DrawPinBackground(canvas);
            DrawCircularPhoto(canvas, photo);
            DrawPhotoFrame(canvas);

            using SKImage image =
                SKImage.FromBitmap(markerBitmap);

            using SKData data = image.Encode(
                SKEncodedImageFormat.Png,
                100);

            File.WriteAllBytes(
                generatedPath,
                data.ToArray());

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[PHOTO MARKER] Image generation failed: {ex.Message}");

            generatedPath = string.Empty;
            return false;
        }
    }

    private static void DrawPinBackground(SKCanvas canvas)
    {
        using SKPath pinPath = new();

        pinPath.MoveTo(64, 154);

        pinPath.CubicTo(
            51, 135,
            12, 94,
            12, 58);

        pinPath.CubicTo(
            12, 29,
            35, 8,
            64, 8);

        pinPath.CubicTo(
            93, 8,
            116, 29,
            116, 58);

        pinPath.CubicTo(
            116, 94,
            77, 135,
            64, 154);

        pinPath.Close();

        // Soft shadow.
        using SKPaint shadowPaint = new()
        {
            IsAntialias = true,
            Color = new SKColor(25, 45, 65, 75),
            MaskFilter = SKMaskFilter.CreateBlur(
                SKBlurStyle.Normal,
                4)
        };

        canvas.Save();
        canvas.Translate(0, 3);
        canvas.DrawPath(pinPath, shadowPaint);
        canvas.Restore();

        // White pin body.
        using SKPaint fillPaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.White
        };

        canvas.DrawPath(pinPath, fillPaint);

        // Blue outline.
        using SKPaint outlinePaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = SKColor.Parse("#486F91")
        };

        canvas.DrawPath(pinPath, outlinePaint);
    }

    private static void DrawCircularPhoto(
        SKCanvas canvas,
        SKBitmap photo)
    {
        float cropSize = Math.Min(
            photo.Width,
            photo.Height);

        float cropX =
            (photo.Width - cropSize) / 2f;

        float cropY =
            (photo.Height - cropSize) / 2f;

        SKRect sourceRect = new(
            cropX,
            cropY,
            cropX + cropSize,
            cropY + cropSize);

        SKRect destinationRect = new(
            PhotoCenterX - PhotoRadius,
            PhotoCenterY - PhotoRadius,
            PhotoCenterX + PhotoRadius,
            PhotoCenterY + PhotoRadius);

        using SKPath photoClip = new();

        photoClip.AddCircle(
            PhotoCenterX,
            PhotoCenterY,
            PhotoRadius);

        using SKPaint photoPaint = new()
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

        canvas.Save();

        canvas.ClipPath(
            photoClip,
            SKClipOperation.Intersect,
            true);

        canvas.DrawBitmap(
            photo,
            sourceRect,
            destinationRect,
            photoPaint);

        canvas.Restore();
    }

    private static void DrawPhotoFrame(SKCanvas canvas)
    {
        using SKPaint whiteFrame = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 7,
            Color = SKColors.White
        };

        canvas.DrawCircle(
            PhotoCenterX,
            PhotoCenterY,
            PhotoRadius + 1,
            whiteFrame);

        using SKPaint blueFrame = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = SKColor.Parse("#486F91")
        };

        canvas.DrawCircle(
            PhotoCenterX,
            PhotoCenterY,
            PhotoRadius + 4,
            blueFrame);
    }
}
