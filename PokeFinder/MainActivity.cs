using System;
using System.Net;
using System.IO;
using System.Text;

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

namespace PokeFinder
{

    class SubmitPokemonFragment : Fragment
    {

        
        public LatLong currentLocation;
        MapFragment _myMapFragment;
        GoogleMap map;
        GroundOverlay myOverlay;
        AutoCompleteTextView pokemonText;
        Button submitButton;
        XMLHook xmlHook;
        private System.Timers.Timer submitTimer;
        private int countdownSeconds = 60;
        bool canSubmit = true;

        bool _mapBuilt = false;
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
                Stream response = myClient.OpenRead("http://73.104.32.120/Pokefinder.php?name=" + thePokemon + "&location=" + loc);

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

        void BuildMap()
        {
            LatLng location = new LatLng(currentLocation.Latitude, currentLocation.Longitude);
            CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
            builder.Target(location);
            builder.Zoom(18);
            builder.Bearing(155);
            builder.Tilt(0);
            CameraPosition cameraPosition = builder.Build();
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);

            BitmapDescriptor image = BitmapDescriptorFactory.FromResource(Resource.Drawable.LocationMarker);
            GroundOverlayOptions groundOverlayOptions = new GroundOverlayOptions()
                .Position(location, 8, 8)
                .InvokeImage(image);

            MapFragment mapFrag = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.mapContainer);
            map = mapFrag.Map;
            if (map != null)
            {
                map.UiSettings.SetAllGesturesEnabled(false);
                map.MoveCamera(cameraUpdate);
                myOverlay = map.AddGroundOverlay(groundOverlayOptions);
            }
            _mapBuilt = true;

        }

        public void UpdateLocation(LatLong location)
        {
            currentLocation = location;

            if (!_mapBuilt)
                BuildMap();
        }
    }

    class FindPokemonFragment : Fragment
    {
        public LatLong currentLocation;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.FindPokemon, container, false);
            //var sampleTextView = view.FindViewById<TextView>(Resource.Id.sampleTextView);
            //sampleTextView.Text = "sample fragment text 2";
            currentLocation = new LatLong();
            return view;
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

        void AddTab(string tabText, Fragment view)
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
            };
            tab.TabUnselected += delegate (object sender, ActionBar.TabEventArgs e) {
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
            submitFragment.UpdateLocation(currentLocation);
        }

        public void OnConnectionSuspended(int i)
        {

        }
    }
}

