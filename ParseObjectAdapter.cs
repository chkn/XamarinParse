using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	// This class does the marshaling to/from json
	class ParseObjectAdapter : DictionaryAdapter {
		public static readonly ParseObjectAdapter Instance = new ParseObjectAdapter ();

		internal ParseObjectAdapter ()
		{
		}

		public override Type GetValueType (object data, string key)
		{
			PropertyInfo prop;
			var po = (ParseObject)data;

			if (po.property_keys != null && po.property_keys.TryGetValue (key, out prop)) {
				var type = prop.PropertyType;
				var attr = type.GetCustomAttributes (typeof (ParseTypeAttribute), false);
				if (attr != null && attr.Length != 0)
					return typeof (Dictionary<string,object>);

				return prop.PropertyType;
			}
			return base.GetValueType (data, key);
		}
		
		public override Future WriteJson (object data, JsonWriter writer)
		{
			var po = (ParseObject)data;

			lock (po.property_lock) {
				
				// fold in our [ParseKey] properties
				if (po.property_keys != null) {
					foreach (var kv in po.property_keys) {
						object dictValue = null;
						po.properties.TryGetValue (kv.Key, out dictValue);
						var propValue = kv.Value.GetValue (data, null);
						if ((propValue != null && !propValue.Equals (dictValue)) ||
						    (propValue == null && dictValue != null)) {
							po.properties [kv.Key] = propValue;
							po.updated_properties.Add (kv.Key);
						}
					}
				}
				
				if (po.updated_properties.Count == 0)
					return Future.Fulfilled;
				
				writer.StartObject ();
		
				foreach (var key in po.updated_properties) {
					writer.WriteKey (key);

					var value = po.properties [key];
					if (value is DateTime) {
						writer.StartObject ();
						writer.WriteKey ("__type");
						writer.WriteString ("Date");
						writer.WriteKey ("iso");
						writer.WriteString (ToString ((DateTime)value));
						writer.EndObject ();
						continue;

					} else if (value is byte []) {
						throw new NotImplementedException ("Write byte [] data");

					} else if (value is ParseObject) {
						var obj = (ParseObject)value;
						ParseTypeAttribute pta = null;
						if ((pta = (ParseTypeAttribute)obj.GetType ().GetCustomAttributes (typeof (ParseTypeAttribute), true).SingleOrDefault ()) == null
						    || !pta.Inline)
						{
							obj.Save ().Wait ();
							writer.StartObject ();
							writer.WriteKey ("__type");
							writer.WriteString ("Pointer");
							writer.WriteKey ("className");
							writer.WriteString (obj.pointerClassName);
							writer.WriteKey ("objectId");
							writer.WriteString (obj.ObjectId);
							writer.EndObject ();
							continue;
						}

					} else if (value is ParseFile) {
						var file = (ParseFile)value;
						file.Save ().Wait ();
						writer.StartObject ();
						writer.WriteKey ("__type");
						writer.WriteString ("File");
						writer.WriteKey ("name");
						writer.WriteString (file.RemoteName);
						writer.EndObject ();
						continue;

					} else if (value is ParseGeoPoint) {
						var geo = (ParseGeoPoint)value;
						writer.StartObject ();
						writer.WriteKey ("__type");
						writer.WriteString ("GeoPoint");
						writer.WriteKey ("latitude");
						writer.WriteValue (geo.Latitude);
						writer.WriteKey ("longitude");
						writer.WriteValue (geo.Longitude);
						writer.EndObject ();
						continue;
					}

					writer.WriteValue (value).Wait ();
				}

				writer.EndObject ();			
			}
			
			return Future.Fulfilled;
		}

		public override Future SetKey (object data, string key, object value)
		{
			switch (key) {

			// handle the special keys
			case "objectId" : ((ParseObject)data).ObjectId = (string)value; return Future.Fulfilled;
			case "createdAt": ((ParseObject)data).CreateDate = ParseDateTime ((string)value); return Future.Fulfilled;
			case "updatedAt": ((ParseObject)data).ModifiedDate = ParseDateTime ((string)value); return Future.Fulfilled;
			}

			// handle the special data types
			var parseObject = (ParseObject)data;
			var parseValue = HandleParseType (data, key, value).Wait ();
			base.SetKey (data, key, parseValue).Wait ();

			PropertyInfo prop;
			if (parseObject.property_keys != null && parseObject.property_keys.TryGetValue (key, out prop))
				prop.SetValue (parseObject, parseValue, null);

			return Future.Fulfilled;
		}

		Future<object> HandleParseType (object data, string key, object value)
		{
			object typeObj;
			string type;

			var hash = value as IDictionary<string,object>;
			if (hash == null || !hash.TryGetValue ("__type", out typeObj) || (type = typeObj as string) == null)
				return value;

			switch (type) {

			case "Date":
				return ParseDateTime ((string)hash ["iso"]);
				break;

			case "Bytes":
				throw new NotImplementedException ("Read byte []");
				break;

			case "Pointer":
				throw new NotImplementedException ("Read pointer to ParseObject");
				break;

			case "File":
				return new ParseFile {
					RemoteName = (string)hash ["name"],
					RemoteURL = (string)hash ["url"]
				};
				break;

			case "GeoPoint":
				return new ParseGeoPoint ((double)hash ["latitude"], (double)hash ["longitude"]);
				break;
			}
			
			return value;
		}

		// Parse-specific data types

		const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffK";

		static DateTime ParseDateTime (string value)
		{
			return DateTime.ParseExact (value, DATETIME_FORMAT, CultureInfo.InvariantCulture);
		}

		static string ToString (DateTime value)
		{
			return value.ToString (DATETIME_FORMAT);
		}

		// Helper to update fields in an existing ParseObject

		public void Apply (ParseObject po, IDictionary<string,object> updates)
		{
			foreach (var update in updates)
				SetKey (po, update.Key, update.Value);
		}
	}
}