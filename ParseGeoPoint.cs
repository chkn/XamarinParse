using System;

namespace Xamarin.Parse {

	[ParseType ("GeoPoint")]
	public sealed class ParseGeoPoint {

		internal bool modified;
		double latitude, longitude;

		public double Latitude  {
			get { return latitude; }
			set {
				if (latitude != value) {
					modified = true;
					latitude = value;
				}
			}
		}

		public double Longitude {
			get { return longitude; }
			set {
				if (longitude != value) {
					modified = true;
					longitude = value;
				}
			}
		}

		public double MetersFrom (ParseGeoPoint other)
		{
			if (other == null)
				throw new ArgumentNullException ("other");

			return MeterDistance (latitude, longitude, other.latitude, other.longitude);
		}

		public static double MeterDistance (double lat1, double lon1, double lat2, double lon2)
		{
			var R = 6371; // km
			var dLat = (lat2 - lat1) * Math.PI / 180;
			var dLon = (lon2 - lon1) * Math.PI / 180;
			lat1 = lat1 * Math.PI / 180;
			lat2 = lat2 * Math.PI / 180;

			var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
			    Math.Sin(dLon/2) * Math.Sin(dLon/2) * Math.Cos(lat1) * Math.Cos(lat2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
			return (R * c) * 1000;
		}

		public ParseGeoPoint (double latitude, double longitude)
		{
			this.latitude = latitude;
			this.longitude = longitude;
		}
	}
}

