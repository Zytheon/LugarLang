using MauiLocation =
    Microsoft.Maui.Devices.Sensors.Location;

using Microsoft.Maui.Devices.Sensors;

namespace LugarLang.Mobile.Services.Location;

public class LocationService
{
    public async Task<MauiLocation?> GetCurrentLocationAsync()
    {
        PermissionStatus permission =
            await Permissions.CheckStatusAsync<
                Permissions.LocationWhenInUse>();

        if (permission != PermissionStatus.Granted)
        {
            permission =
                await Permissions.RequestAsync<
                    Permissions.LocationWhenInUse>();
        }

        if (permission != PermissionStatus.Granted)
        {
            return null;
        }

        GeolocationRequest request =
            new(
                GeolocationAccuracy.Low);

        return await Geolocation.GetLocationAsync(
            request);
    }
}