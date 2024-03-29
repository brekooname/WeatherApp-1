﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using Xamarin.Essentials;

namespace WeatherApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SearchPage : ContentPage
    {
        public static ObservableCollection<GooglePlaceAutoCompletePrediction> PlacesList { get; set; }
        public SearchPage(MainPage main, bool addLocation = false)
        {
            InitializeComponent();
            mp = main;
            AddLocation = addLocation;
            if (addLocation)
                FindMyLocation.IsVisible = false;
            else
                FindMyLocation.IsVisible = true;

            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                InternetConnection.IsVisible = false;
                FindMyLocation.IsEnabled = true;
            }
            else
            {
                FindMyLocation.IsEnabled = false;
                InternetConnection.IsVisible = true;    
            }
        }
        private MainPage mp;
        private bool AddLocation;
        private async void PlaceSearch_Completed(object sender, EventArgs e)
        {
            Entry searchtext = (Entry)sender;
            string url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={searchtext.Text}&key={Constants.GoogleMapsKey}";

            var current = Connectivity.NetworkAccess;
            if (current == NetworkAccess.Internet)
            {
                API_Response data = await API_Caller.Get(url);

                if (data.Successuful)
                {
                    var info = JsonConvert.DeserializeObject<GooglePlaceAutoCompleteResult>(data.Response);

                    PlacesList = new ObservableCollection<GooglePlaceAutoCompletePrediction>(info.AutoCompletePlaces);
                    PlacesSearchListView.ItemsSource = PlacesList;
                }
            }
            
        }

        private async void viewCell_Tapped(object sender, EventArgs e)
        {
            GooglePlace info = null;
            ViewCell vc = (ViewCell)sender;
            GooglePlaceAutoCompletePrediction SelectedPlace = (GooglePlaceAutoCompletePrediction)vc.BindingContext;
            string url = $"https://maps.googleapis.com/maps/api/place/details/json?placeid={SelectedPlace.PlaceId}&key={Constants.GoogleMapsKey}";

            API_Response data = await API_Caller.Get(url);

            if (data.Successuful)
            {
                info = new GooglePlace(JObject.Parse(data.Response));
                if (!AddLocation)
                {
                    MainPage.longitude = info.Longitude.ToString();
                    MainPage.latitude = info.Latitude.ToString();
                    MainPage.placeName1 = info.Name1;
                    MainPage.currentLocation = false;
                    mp.GetCurrentWeather();
                    await Navigation.PopAsync();
                }
                else
                {
                    Location location = new Location() { latitude = info.Latitude.ToString(),longitude = info.Longitude.ToString(), name = info.Name1 };
                    if (!BookmarkedPage.LocationList.Any(n=> n.name == location.name ))
                    {
                        BookmarkedPage.LocationList.Add(location);
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Location is already in your list.", "OK");
                    }
                }
            }
        }

        private void CloseSearch_Clicked(object sender, EventArgs e)
        {
             Navigation.PopAsync();
        }

        private void FindMyLocation_Clicked(object sender, EventArgs e)
        {
            mp.GetCurrentLocation();
            Navigation.PopAsync();
        }
    }
}