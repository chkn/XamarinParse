using System;
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

		private ParseObjectAdapter ()
		{
		}

		public override Type GetValueType (object data, string key)
		{
			PropertyInfo prop;
			var po = (ParseObject)data;
			if (po.property_keys != null && po.property_keys.TryGetValue (key, out prop))
				return prop.PropertyType;
			
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
						throw new NotImplementedException ("Write DateTime");
					} else if (value is byte []) {
						throw new NotImplementedException ("Write byte [] data");
					} else if (value is ParseObject) {
						var obj = (ParseObject)value;
						obj.Save ().Wait ();
						writer.StartObject ();
						writer.WriteKey ("__type");
						writer.WriteString ("Pointer");
						writer.WriteKey ("className");
						writer.WriteString (obj.pointerClassName);
						writer.WriteKey ("objectId");
						writer.WriteString (obj.ObjectId);
						writer.EndObject ();
					} else {
						writer.WriteValue (value).Wait ();
					}
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
			base.SetKey (data, key, parseValue);

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
				throw new NotImplementedException ("Read DateTime");
				break;

			case "Bytes":
				throw new NotImplementedException ("Read byte []");
				break;

			case "Pointer":
				throw new NotImplementedException ("Read pointer to ParseObject");
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

		// Helper to update fields in an existing ParseObject

		public void Apply (ParseObject po, IDictionary<string,object> updates)
		{
			foreach (var update in updates)
				SetKey (po, update.Key, update.Value);
		}
	}
}