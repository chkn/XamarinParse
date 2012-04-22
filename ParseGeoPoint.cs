using System;

namespace Xamarin.Parse {
	public class ParseGeoPoint {

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

	}
}

