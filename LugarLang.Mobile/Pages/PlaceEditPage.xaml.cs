namespace LugarLang.Mobile.Pages;

using LugarLang.Mobile.Models;
using LugarLang.Mobile.Services.Content;

public partial class PlaceEditPage : ContentPage
{
    private readonly Place place;

    private readonly PlaceContentService
        placeContentService;

    public PlaceEditPage(
        Place place,
        PlaceContentService placeContentService)
    {
        InitializeComponent();

        this.place =
            place;

        this.placeContentService =
            placeContentService;

        NameEntry.Text =
            place.Name;

        Entry? regionEntry =
            this.FindByName<Entry>(
                "RegionEntry");

        if (regionEntry != null)
        {
            regionEntry.Text =
                place.Region;
        }

        FacebookEntry.Text =
            place.Contacts.Facebook;

        InstagramEntry.Text =
            place.Contacts.Instagram;

        WhatsAppEntry.Text =
            place.Contacts.WhatsApp;

        PhoneNumberEntry.Text =
            place.Contacts.PhoneNumber;


        DescriptionEditor.Text =
            place.Description;

        if (place.Photos.Count > 0)
        {
            PlacePhotoPreview.Source =
                ImageSource.FromFile(
                    place.Photos[0]);
        }

        if (!string.IsNullOrWhiteSpace(
            place.Payments.GCashQrPhoto))
        {
            GcashQrPreview.Source =
                ImageSource.FromFile(
                    place.Payments.GCashQrPhoto);
        }

        if (!string.IsNullOrWhiteSpace(
            place.Payments.MayaQrPhoto))
        {
            MayaQrPreview.Source =
                ImageSource.FromFile(
                    place.Payments.MayaQrPhoto);
        }
    }

    private async void OnAddGcashQrClicked(
    object sender,
    EventArgs e)
    {
        FileResult? result =
            await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle =
                        "Select GCash QR"
                });

        if (result == null)
        {
            return;
        }

        string fileName =
            $"gcash_{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";

        string destination =
            Path.Combine(
                FileSystem.AppDataDirectory,
                fileName);

        using Stream sourceStream =
            await result.OpenReadAsync();

        using FileStream destinationStream =
            File.Create(destination);

        await sourceStream.CopyToAsync(
            destinationStream);

        place.Payments.GCashQrPhoto =
            destination;

        GcashQrPreview.Source =
            ImageSource.FromFile(
                destination);
    }

    private async void OnAddMayaQrClicked(
        object sender,
        EventArgs e)
    {
        FileResult? result =
            await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle =
                        "Select Maya QR"
                });

        if (result == null)
        {
            return;
        }

        string fileName =
            $"maya_{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";

        string destination =
            Path.Combine(
                FileSystem.AppDataDirectory,
                fileName);

        using Stream sourceStream =
            await result.OpenReadAsync();

        using FileStream destinationStream =
            File.Create(destination);

        await sourceStream.CopyToAsync(
            destinationStream);

        place.Payments.MayaQrPhoto =
            destination;

        MayaQrPreview.Source =
            ImageSource.FromFile(
                destination);
    }

    private async void OnAddPhotoClicked(
        object sender,
        EventArgs e)
    {
        FileResult? result =
            await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle =
                        "Select a restaurant photo"
                });

        if (result == null)
        {
            return;
        }

        string fileName =
            $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";

        string destination =
            Path.Combine(
                FileSystem.AppDataDirectory,
                fileName);

        using Stream sourceStream =
            await result.OpenReadAsync();

        using FileStream destinationStream =
            File.Create(destination);

        await sourceStream.CopyToAsync(
            destinationStream);

        place.Photos.Add(
            destination);

        PlacePhotoPreview.Source =
            ImageSource.FromFile(
                destination);
    }

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        place.Name =
            NameEntry.Text ??
            string.Empty;

        Entry? regionEntry =
    this.FindByName<Entry>(
        "RegionEntry");

        place.Region =
            regionEntry?.Text?.Trim() ??
            string.Empty;

        place.Contacts.Facebook =
            FacebookEntry.Text;

        place.Contacts.Instagram =
            InstagramEntry.Text;


        place.Contacts.WhatsApp =
            WhatsAppEntry.Text;

        place.Contacts.PhoneNumber =
            PhoneNumberEntry.Text;

        place.Description =
            DescriptionEditor.Text ??
            string.Empty;

        placeContentService.UpdatePlace(
            place);

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}