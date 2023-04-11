using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using AndroidNetwork = Android.Net.Network;

namespace Sensus.Android.Probes.Network
{
	public class AndroidNetworkCallback : ConnectivityManager.NetworkCallback
	{
		private readonly AndroidListeningWlanProbe _probe;
		private string _previousBssid;

		public AndroidNetworkCallback(AndroidListeningWlanProbe probe)
		{
			_probe = probe;
			_previousBssid = null;
		}

		public async override void OnAvailable(AndroidNetwork network)
		{
			base.OnAvailable(network);

			if (Build.VERSION.SdkInt < BuildVersionCodes.S)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				WifiInfo wifiInfo = AndroidSensusServiceHelper.WifiManager.ConnectionInfo;
#pragma warning restore CS0618 // Type or member is obsolete
				await _probe.CreateDatumAsync(wifiInfo.BSSID, wifiInfo.Rssi);
			}
			else
			{
				NetworkCapabilities capabilities = AndroidSensusServiceHelper.ConnectivityManager.GetNetworkCapabilities(network);

				if (capabilities.TransportInfo is WifiInfo wifi && _previousBssid != wifi.BSSID)
				{
					await _probe.CreateDatumAsync(wifi.BSSID, wifi.Rssi);
				}
			}
		}

		public override void OnLost(AndroidNetwork network)
		{
			base.OnLost(network);

			_previousBssid = null;
		}
	}
}
