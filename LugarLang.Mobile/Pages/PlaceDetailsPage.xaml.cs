namespace LugarLang.Mobile.Pages;

using LugarLang.Mobile.Models;

public partial class PlaceDetailsPage : ContentPage
{
    private readonly Place place;

    public PlaceDetailsPage(
        Place place)
    {
        InitializeComponent();

        this.place =
            place;

        LoadPlace();
    }

    private void LoadPlace()
    {
        Title =
            place.Name;

        PlaceNameLabel.Text =
            place.Name;

        PlaceDescriptionLabel.Text =
            place.Description ??
            string.Empty;

        if (place.Photos.Count > 0)
        {
            PlacePhoto.Source =
                ImageSource.FromFile(
                    place.Photos[0]);
        }

        if (!string.IsNullOrWhiteSpace(
            place.Contacts.PhoneNumber))
        {
            PhoneButton.Text =
                $"📞 {place.Contacts.PhoneNumber}";
        }
        else
        {
            PhoneButton.IsVisible =
                false;
        }

        if (!string.IsNullOrWhiteSpace(
            place.Contacts.Facebook))
        {
            FacebookButton.IsVisible =
                true;
        }
        else
        {
            FacebookButton.IsVisible =
                false;
        }

        if (!string.IsNullOrWhiteSpace(
            place.Contacts.Instagram))
        {
            InstagramButton.IsVisible =
                true;
        }
        else
        {
            InstagramButton.IsVisible =
                false;
        }

        if (!string.IsNullOrWhiteSpace(
            place.Contacts.WhatsApp))
        {
            WhatsAppButton.IsVisible =
                true;
        }
        else
        {
            WhatsAppButton.IsVisible =
                false;
        }

        if (!string.IsNullOrWhiteSpace(
            place.Payments.GCashQrPhoto))
        {
            GcashQrImage.Source =
                ImageSource.FromFile(
                    place.Payments.GCashQrPhoto);
        }

        if (!string.IsNullOrWhiteSpace(
            place.Payments.MayaQrPhoto))
        {
            MayaQrImage.Source =
                ImageSource.FromFile(
                    place.Payments.MayaQrPhoto);
        }
    }

    private async void OnPhoneClicked(
        object sender,
        EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(
            place.Contacts.PhoneNumber))
        {
            await Launcher.Default.OpenAsync(
                $"tel:{place.Contacts.PhoneNumber}");
        }
    }

    private async void OnFacebookClicked(
        object sender,
        EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(
            place.Contacts.Facebook))
        {
            await Launcher.Default.OpenAsync(
                place.Contacts.Facebook);
        }
    }

    private async void OnInstagramClicked(
        object sender,
        EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(
            place.Contacts.Instagram))
        {
            await Launcher.Default.OpenAsync(
                place.Contacts.Instagram);
        }
    }

    private async void OnWhatsAppClicked(
        object sender,
        EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(
            place.Contacts.WhatsApp))
        {
            await Launcher.Default.OpenAsync(
                place.Contacts.WhatsApp);
        }
    }
}