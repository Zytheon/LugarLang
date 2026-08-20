using CdoGtfsConverter.Models;
using Mapsui;

namespace LugarLang.Mobile.Services.Mapping;

public class MapInteractionController
{
    public enum PinMode
    {
        None,
        SettingFrom,
        SettingTo
    }

    public PinMode CurrentMode { get; private set; }

    public MapInteractionController()
    {
        CurrentMode = PinMode.None;
    }

    public void StartSettingFrom()
    {
        CurrentMode = PinMode.SettingFrom;
    }

    public void StartSettingTo()
    {
        CurrentMode = PinMode.SettingTo;
    }

    public bool TryConsumeMapTap(
        MPoint position,
        out PinMode mode)
    {
        mode = CurrentMode;

        if (CurrentMode == PinMode.None)
        {
            return false;
        }

        CurrentMode = PinMode.None;

        return true;
    }
}


