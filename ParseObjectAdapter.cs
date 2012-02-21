using System;
using System.Globalization;
using System.Collections.Generic;

using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	// This class does the marshaling to/from json
	class ParseObjectAdapter : DictionaryAdapter {
		public static new readonly ParseObjectAdapter Instance = new ParseObjectAdapter ();

		private ParseObjectAdapter ()
		{
		}

		public override void WriteJson (object data, JsonWriter writer)
		{
			var po = (ParseObject)data;
			writer.StartObject ();

			lock (po.property_lock) {
				foreach (var key in po.updated_properties) {
					writer.WriteKey (key);

					var value = po.properties [key];
					if (value is DateTime) {
						throw new NotImplementedException ("Write DateTime");
					} else if (value is byte []) {
						throw new NotImplementedException ("Write byte [] data");
					} else if (value is ParseObject) {
						throw new NotImplementedException ("Write pointer to ParseObject");
					} else {
						writer.WriteValue (value);
					}
				}
			}

			writer.EndObject ();
		}

		public override void SetKey (object data, string key, object value)
		{
			var po = (ParseObject)data;
			switch (key) {

			// handle the special keys
			case "objectId" : po.Id = (string)value; return;
			case "createdAt": po.CreateDate = ParseDateTime ((string)value); return;
			case "updatedAt": po.ModifiedDate = ParseDateTime ((string)value); return;
			}

			// handle the special data types
			if (!HandleParseType (data, key, value))
				base.SetKey (data, key, value);
		}

		bool HandleParseType (object data, string key, object value)
		{
			object typeObj;
			string type;

			var hash = value as IDictionary<string,object>;
			if (hash == null || !hash.TryGetValue ("__type", out typeObj) || (type = typeObj as string) == null)
				return false;

			switch (type) {

			case "Date":
				throw new NotImplementedException ("Read DateTime");
				break;

			case "Bytes":
				throw new NotImplementedException ("Read byte []");
				break;

			case "Pointer":
				throw new NotImplementedException ("Read pointer to ParseObject");
				break;
			}

			return true;
		}

		// Parse-specific data types

		const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffK";

		static DateTime ParseDateTime (string value)
		{
			return DateTime.ParseExact (value, DATETIME_FORMAT, CultureInfo.InvariantCulture);
		}

		// Helper to update fields in an existing ParseObject

		public void Apply (ParseObject po, IDictionary<string,object> updates)
		{
			foreach (var update in updates)
				SetKey (po, update.Key, update.Value);
		}
	}
}