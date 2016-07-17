﻿using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Location;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Locations;
using Android.Gms.Ads;
//using Android.Support.V4.App;

namespace PokeFinder
{



    class SubmitPokemonFragment : Fragment
    {

        
        public LatLong currentLocation;
        MapFragment _myMapFragment;
        public MapFragment mapFrag;
        GoogleMap map;
        GroundOverlay myOverlay;
        AutoCompleteTextView pokemonText;
        Button submitButton;
        XMLHook xmlHook;
        private System.Timers.Timer submitTimer;
        private int countdownSeconds = 60;
        bool canSubmit = true;
        bool hasGPS = false;
        public bool initialized = false;
        public bool selected = true;
        



        public bool _mapBuilt = false;
        //message from kyle.. FUCK YES DUDE.
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            
            
            var view = inflater.Inflate(Resource.Layout.SubmitPokemon, container, false);
            //var sampleTextView = view.FindViewById<TextView>(Resource.Id.sampleTextView);
            //sampleTextView.Text = "sample fragment text";

            

            currentLocation = new LatLong();
            pokemonText = view.FindViewById<AutoCompleteTextView>(Resource.Id.pokemonNameText1);
            submitButton = view.FindViewById<Button>(Resource.Id.submitButton);
            
           // Console.WriteLine("View Created.");

            submitButton.Click += delegate
            {
                SubmitPokemon();
            };

            xmlHook = new XMLHook();
            string[] pokemonList = xmlHook.GetPokemonArray(base.Activity.Assets);
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleDropDownItem1Line, pokemonList);
            pokemonText.Adapter = adapter;


            GoogleMapOptions mapOptions = new GoogleMapOptions()
                .InvokeMapType(GoogleMap.MapTypeTerrain)
                .InvokeZoomControlsEnabled(false)
                .InvokeCompassEnabled(false);

            _myMapFragment = MapFragment.NewInstance(mapOptions);
            FragmentTransaction tx = FragmentManager.BeginTransaction();
            tx.Add(Resource.Id.mapContainer, _myMapFragment);
            tx.Commit();
            

            

            MapsInitializer.Initialize(this.Activity);
            
            //if (initialized)
            //  BuildMap();

            var ad = new AdView(this.Activity);
            ad.AdSize = AdSize.SmartBanner;
            ad.AdUnitId = "ca-app-pub-4107702687832237/3970765307";
            var requestbuilder = new AdRequest.Builder();
            ad.LoadAd(requestbuilder.Build());
            var layout = view.FindViewById<LinearLayout>(Resource.Id.submitAdLayout);
            layout.AddView(ad);

            return view;
        }

        void ResetTimer()
        {
            canSubmit = false;
            submitTimer = new System.Timers.Timer();
            submitTimer.Interval = 1000;
            submitTimer.Elapsed += ResetSubmit;
            countdownSeconds = 60;
            submitTimer.Enabled = true;
        }

        void ResetSubmit(object sender, System.Timers.ElapsedEventArgs e)
        {
            countdownSeconds--;

            if(countdownSeconds == 0)
            {
                canSubmit = true;
                submitTimer.Stop();
            }
        }

        void SubmitPokemon()
        {
            if(!hasGPS)
            {
                Toast.MakeText(base.Activity, "Currently unable to provide GPS Coordinates. Please try again.", ToastLength.Short).Show();
                return;
            }

            if (!canSubmit)
            {
                Toast.MakeText(base.Activity, "You must wait " + countdownSeconds.ToString() + " seconds before submitting another pokemon.", ToastLength.Short).Show();
                return;
            }
               


            Console.WriteLine("submitting.");
            string[] arr = xmlHook.GetPokemonArray(base.Activity.Assets);
            string thePokemon = pokemonText.Text;

            if(Array.Exists(arr, element => element == thePokemon))
            {
                string loc = currentLocation.Latitude.ToString() + "," + currentLocation.Longitude.ToString();            
                WebClient myClient = new WebClient();
                Stream response = myClient.OpenRead("http://www.pokefindergo.com/Pokefinder.php?name=" + thePokemon + "&location=" + loc);

                StreamReader reader = new StreamReader(response);
                string text = reader.ReadToEnd();
                Toast.MakeText(base.Activity, text, ToastLength.Short).Show();
                if (text == "Success!")
                    ResetTimer();

                response.Close();



            }
            else
            {
                Console.WriteLine("Pokemon doesn't exist.");
                Toast.MakeText(base.Activity, "The pokemon you entered does not exist!", ToastLength.Short).Show();
            }
        }

        public void BuildMap()
        {
            Console.WriteLine("Building Map.");
            LatLng location = new LatLng(currentLocation.Latitude, currentLocation.Longitude);
            CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
            builder.Target(location);
            builder.Zoom(18);
            builder.Bearing(155);
            builder.Tilt(0);
            CameraPosition cameraPosition = builder.Build();
            
            

            BitmapDescriptor image = BitmapDescriptorFactory.FromResource(Resource.Drawable.LocationMarker);
            GroundOverlayOptions groundOverlayOptions = new GroundOverlayOptions()
                .Position(location, 8, 8)
                .InvokeImage(image);

            //if(mapFrag == null)

            if (mapFrag == null)
                mapFrag = (MapFragment)this.FragmentManager.FindFragmentById(Resource.Id.mapContainer);
            
            map = mapFrag.Map;
            if (map != null)
            {
               
                Console.WriteLine("Not Null");
                map.UiSettings.SetAllGesturesEnabled(false);
                CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
                map.MoveCamera(cameraUpdate);
                myOverlay = map.AddGroundOverlay(groundOverlayOptions);
            }
            _mapBuilt = true;

        }

        public void UpdateLocation(LatLong location)
        {
            if(location == null)
            {
                Toast.MakeText(base.Activity, "Unable to get GPS Coordinates", ToastLength.Short).Show();
                return;
            }
           
            currentLocation = location;

            hasGPS = true;

            if (!_mapBuilt)
                BuildMap();
        }
    }

    class FindPokemonFragment : Fragment
    {
        public LatLong currentLocation;
        public Spinner raritySpinner;
        View theView;
        LinearLayout theLayout;
        List<View> viewsToClear;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.FindPokemon, container, false);
            //var sampleTextView = view.FindViewById<TextView>(Resource.Id.sampleTextView);
            //sampleTextView.Text = "sample fragment text 2";
            currentLocation = new LatLong();
            raritySpinner = view.FindViewById<Spinner>(Resource.Id.firstSpinner);
            raritySpinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
            viewsToClear = new List<View>();

            var items = new List<string>() { "Find...", "Specific Pokemon", "Rarest Pokemon", "All Pokemon" };
            var adapter = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleSpinnerItem, items);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            raritySpinner.Adapter = adapter;

            theView = view;
            return view;
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            CreateView();
        }

        public void CreateView()
        {
            theLayout = theView.FindViewById<LinearLayout>(Resource.Id.findLayout);
            

            string selection = raritySpinner.SelectedItem.ToString();
            
            if(selection == "Find...")
            {
                ClearView();
                var ad = new AdView(this.Activity);
                ad.AdSize = AdSize.SmartBanner;
                ad.AdUnitId = "ca-app-pub-4107702687832237/3970765307";
                var requestbuilder = new AdRequest.Builder();
                ad.LoadAd(requestbuilder.Build());
                theLayout.AddView(ad);
                viewsToClear.Add(ad);
            }
            else if(selection == "Specific Pokemon")
            {
                ClearView();
                base.Activity.RunOnUiThread(() =>
                {
                    BuildSpecific();
                });
                
            }
            else if(selection == "Rarest Pokemon")
            {
                ClearView();
                BuildRarest();
            }
            else
            {
                ClearView();
                BuildAll();
            }
        }

        void ClearView()
        {
            foreach(View v in viewsToClear)
            {
                theLayout.RemoveView(v);
            }

            viewsToClear.Clear();
            viewsToClear.TrimExcess();
        }

        void BuildSpecific()
        {
            AutoCompleteTextView pokemonText = new AutoCompleteTextView(base.Activity);
            pokemonText.Hint = "Pokemon Name";

            XMLHook xmlHook = new XMLHook();
            string[] pokemonList = xmlHook.GetPokemonArray(base.Activity.Assets);
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleDropDownItem1Line, pokemonList);
            pokemonText.Adapter = adapter;

            theLayout.AddView(pokemonText);
            viewsToClear.Add(pokemonText);




            BuildGeneric();

        }

        void BuildAll()
        {
            BuildGeneric();
        }

        void BuildRarest()
        {
            EditText topPokemon = new EditText(base.Activity);
            topPokemon.Hint = "Find top '*' rarest pokemon.";
            theLayout.AddView(topPokemon);
            viewsToClear.Add(topPokemon);
            BuildGeneric();
        }

        void BuildGeneric()
        {
            LinearLayout radiusLayout = new LinearLayout(base.Activity);
            radiusLayout.Orientation = Orientation.Horizontal;
            LinearLayout.LayoutParams radiuslayoutparams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
            radiuslayoutparams.TopMargin = 150;
            radiusLayout.LayoutParameters = radiuslayoutparams;

            EditText radiusText = new EditText(base.Activity);
            radiusText.Hint = "Within Radius...";
            Spinner radiusSpinner = new Spinner(base.Activity);
            var items = new List<string>() { "Miles", "Kilometers" };
            var rAdapter = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleSpinnerItem, items);
            rAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            radiusSpinner.Adapter = rAdapter;

            radiusLayout.AddView(radiusText);
            radiusLayout.AddView(radiusSpinner);
            theLayout.AddView(radiusLayout);
            viewsToClear.Add(radiusLayout);

            LinearLayout timeLayout1 = new LinearLayout(base.Activity);
            timeLayout1.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams timelayoutparams1 = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
            timelayoutparams1.TopMargin = 150;
            timeLayout1.LayoutParameters = timelayoutparams1;

            Spinner timeSpinner = new Spinner(base.Activity);
            //timeSpinner.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            var timeItems = new List<string>() { "All Time", "Specific Time" };
            var tAdapter = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleSpinnerItem, timeItems);
            tAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            timeSpinner.Adapter = tAdapter;

            LinearLayout timeLayout = new LinearLayout(base.Activity);
            timeLayout.Orientation = Orientation.Horizontal;
            LinearLayout.LayoutParams timelayoutparams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            timeLayout.LayoutParameters = timelayoutparams;
            timeLayout.Tag = "timelayout";

            EditText timePicker = new EditText(base.Activity);
            timePicker.Hint = "Time...";
            Spinner timeSpinner1 = new Spinner(base.Activity);
            //timeSpinner.LayoutParameters = new ViewGroup.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            var timeItems1 = new List<string>() { "Minutes", "Hours", "Days" };
            var tAdapter1 = new ArrayAdapter<string>(base.Activity, Android.Resource.Layout.SimpleSpinnerItem, timeItems1);
            tAdapter1.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            timeSpinner1.Adapter = tAdapter1;

            timeLayout.AddView(timePicker);
            timeLayout.AddView(timeSpinner1);

            timeSpinner.ItemSelected += (object s, AdapterView.ItemSelectedEventArgs e) =>
            {
                if (timeSpinner.SelectedItem.ToString() == "Specific Time")
                {
                    timeLayout1.AddView(timeLayout);
                }
                else
                {
                    if (timeLayout1.FindViewWithTag("timelayout") != null)
                    {
                        timeLayout1.RemoveView(timeLayout);
                    }
                }
            };

            timeLayout1.AddView(timeSpinner);
            theLayout.AddView(timeLayout1);
            viewsToClear.Add(timeLayout1);

            Button findButton = new Button(base.Activity);
            findButton.Text = "Find";
            LinearLayout.LayoutParams btnparams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
            btnparams.TopMargin = 150;
            findButton.LayoutParameters = btnparams;

            findButton.Click += delegate
            {
                FindPokemon();
            };

            theLayout.AddView(findButton);
            viewsToClear.Add(findButton);

            var ad = new AdView(this.Activity);
            ad.AdSize = AdSize.SmartBanner;
            ad.AdUnitId = "ca-app-pub-4107702687832237/3970765307";
            var requestbuilder = new AdRequest.Builder();
            ad.LoadAd(requestbuilder.Build());
            theLayout.AddView(ad);
            viewsToClear.Add(ad);
        }

        public void FindPokemon()
        {

        }

        public void UpdateLocation(LatLong location)
        {
            currentLocation = location;
        }
    }

    class LatLong
    {
        public double Latitude, Longitude;

        public LatLong(double lat, double longi)
        {
            Latitude = lat;
            Longitude = longi;
        }

        public LatLong()
        {
        }
    }

    [Activity(Label = "PokeFinder", MainLauncher = true, Icon = "@drawable/LocationMarker")]
    public class MainActivity : Activity, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener, Android.Gms.Location.ILocationListener 
    {

        
        GoogleApiClient apiClient;
        LocationRequest locRequest;
        LatLong currentLocation;
        SubmitPokemonFragment submitFragment;
        FindPokemonFragment findFragment;
        Button btnBugSubmit;
        EditText messageText;
        EditText emailText;
        AlertDialog bugAlert;
        bool submitMade = false;
        bool findMade = false;

        bool _isGooglePlayServicesInstalled;

        protected override void OnCreate(Bundle bundle)
        {
            
            base.OnCreate(bundle);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            currentLocation = new LatLong();
            this.ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            submitFragment = new SubmitPokemonFragment();
            findFragment = new FindPokemonFragment();

            
            AddTab("Submit Pokemon", submitFragment);
            AddTab("Find Pokemon", findFragment);

            _isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();

            if (_isGooglePlayServicesInstalled)
            {
                // pass in the Context, ConnectionListener and ConnectionFailedListener
                apiClient = new GoogleApiClient.Builder(this, this, this)
                    .AddApi(LocationServices.API).Build();

                apiClient.Connect();
                // generate a location request that we will pass into a call for location updates
                locRequest = new LocationRequest();
            }
            else
            {
               // Log.Error("OnCreate", "Google Play Services is not installed");
                Toast.MakeText(this, "Google Play Services is not installed", ToastLength.Long).Show();
                Finish();
            }

        }

        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        void SendEmail()
        {
            string replyEmail = emailText.Text;
            string message = messageText.Text;

            EmailHook hook = new EmailHook(replyEmail, message);
            if(hook.SendMessage(this))
            {
                Toast.MakeText(this, "Bug Report successfully sent.", ToastLength.Short).Show();
                bugAlert.Dismiss();
            }
            
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);

            

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.report_bug:
                    BuildEmailAlert();
                    return true;
                case Resource.Id.help:
                    //do something
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        void BuildEmailAlert()
        {

            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            var alertView = LayoutInflater.Inflate(Resource.Layout.EmailForm, null);
            alert.SetView(alertView);
            alert.SetTitle("Report Bug");

            btnBugSubmit = alertView.FindViewById<Button>(Resource.Id.btnBugSubmit);
            messageText = alertView.FindViewById<EditText>(Resource.Id.messageText);
            emailText = alertView.FindViewById<EditText>(Resource.Id.emailText);

            bugAlert = alert.Show();

            

            btnBugSubmit.Click += delegate
            {
                if (!IsValidEmail(emailText.Text))
                {
                    Toast.MakeText(this, "You must supply a valid email address.", ToastLength.Short).Show();
                    return;
                }
                SendEmail();
            };
        }

        void AddTab(string tabText, Android.App.Fragment view)
        {
            var tab = this.ActionBar.NewTab();
            tab.SetText(tabText);
            
            //tab.SetIcon(Resource.Drawable.ic_tab_white);

            // must set event handler before adding tab
            tab.TabSelected += delegate (object sender, ActionBar.TabEventArgs e)
            {
                

                var fragment = this.FragmentManager.FindFragmentById(Resource.Id.fragmentContainer);
                if (fragment != null)
                    e.FragmentTransaction.Remove(fragment);
                e.FragmentTransaction.Add(Resource.Id.fragmentContainer, view);

                if (tabText == "Submit Pokemon" && submitMade == true)
                {
                    submitFragment.selected = true;
                    return;
                }
                else if (tabText == "Find Pokemon" && findMade == true)
                {
                    return;
                }
                    
                if (tabText == "Submit Pokemon")
                    submitMade = true;
                else
                    findMade = true;
            };
            tab.TabUnselected += delegate (object sender, ActionBar.TabEventArgs e) {
                if(tabText == "Submit Pokemon")
                {
                    submitFragment.selected = false;
                    submitFragment.mapFrag = null;
                    submitFragment._mapBuilt = false;
                }
                return;
                e.FragmentTransaction.Remove(view);
            };

            this.ActionBar.AddTab(tab);
        }

        bool IsGooglePlayServicesInstalled()
        {
            int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                //Log.Info("MainActivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                //Log.Error("ManActivity", "There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString);

                // Show error dialog to let user debug google play services
            }
            return false;
        }

        protected override void OnResume()
        {
            base.OnResume();

            apiClient.Connect();

            BuildLocation();
        }

        async void BuildLocation()
        {
            if (apiClient.IsConnected)
            {
                //button2.Text = "Requesting Location Updates";

                // Setting location priority to PRIORITY_HIGH_ACCURACY (100)
                locRequest.SetPriority(100);

                // Setting interval between updates, in milliseconds
                // NOTE: the default FastestInterval is 1 minute. If you want to receive location updates more than 
                // once a minute, you _must_ also change the FastestInterval to be less than or equal to your Interval
                locRequest.SetFastestInterval(500);
                locRequest.SetInterval(1000);

                Console.WriteLine("Request priority set to status code {0}, interval set to {1} ms",
                    locRequest.Priority.ToString(), locRequest.Interval.ToString());

                // pass in a location request and LocationListener
                await LocationServices.FusedLocationApi.RequestLocationUpdates(apiClient, locRequest, this);
                // In OnLocationChanged (below), we will make calls to update the UI
                // with the new location data
            }
        }

        protected override async void OnPause()
        {
            base.OnPause();
            //Log.Debug("OnPause", "OnPause called, stopping location updates");

            if (apiClient.IsConnected)
            {
                // stop location updates, passing in the LocationListener
                await LocationServices.FusedLocationApi.RemoveLocationUpdates(apiClient, this);

                apiClient.Disconnect();
            }
        }


        //Interface Methods

        public void OnConnected(Bundle bundle)
        {
            // This method is called when we connect to the LocationClient. We can start location updated directly form
            // here if desired, or we can do it in a lifecycle method, as shown above 

            // You must implement this to implement the IGooglePlayServicesClientConnectionCallbacks Interface
            Console.WriteLine("Connected to GPS.");

            BuildLocation();
        }

        public void OnDisconnected()
        {
            // This method is called when we disconnect from the LocationClient.

            // You must implement this to implement the IGooglePlayServicesClientConnectionCallbacks Interface
            Console.WriteLine("Disconnected from GPS.");
        }

        public void OnConnectionFailed(ConnectionResult bundle)
        {
            // This method is used to handle connection issues with the Google Play Services Client (LocationClient). 
            // You can check if the connection has a resolution (bundle.HasResolution) and attempt to resolve it

            // You must implement this to implement the IGooglePlayServicesClientOnConnectionFailedListener Interface
            Console.WriteLine("Connection failed: " + bundle.ErrorMessage);
        }

        public void OnLocationChanged(Location location)
        {
            // This method returns changes in the user's location if they've been requested

            // You must implement this to implement the Android.Gms.Locations.ILocationListener Interface
            Console.WriteLine("Location updated.");

            //latitudeText.Text = "Latitude: " + location.Latitude;
            //longitudeText.Text = "Longitude: " + location.Longitude.ToString();
            //providerText.Text = "Provider: " + location.Provider.ToString();

            currentLocation = new LatLong(location.Latitude, location.Longitude);
            findFragment.UpdateLocation(currentLocation);

            if(submitFragment.selected)
            submitFragment.UpdateLocation(currentLocation);
        }

        public void OnConnectionSuspended(int i)
        {

        }
    }
}

