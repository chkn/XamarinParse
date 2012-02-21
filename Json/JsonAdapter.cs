using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Parse.Json {

	public abstract class JsonAdapter {

		static Dictionary<Type,JsonAdapter> adapters = new Dictionary<Type, JsonAdapter> ();

		// The default type that should be returned by GetValueType if there is no better alternative
		public static readonly Type DefaultType = typeof (Dictionary<string,object>);

		static JsonAdapter ()
		{
			Register (typeof (IDictionary<string,object>), DictionaryAdapter.Instance);
			Register (typeof (IList), ListAdapter.Instance);
		}

		public static void Register (Type type, JsonAdapter adapter)
		{
			lock (adapters)
				adapters [type] = adapter;
		}

		public static JsonAdapter ForType (Type type)
		{
			JsonAdapter adapter;
			if (adapters.TryGetValue (type, out adapter))
				return adapter;

			foreach (var pair in adapters) {
				if (pair.Key.IsAssignableFrom (type))
					return pair.Value;
			}

			adapter = PocoAdapter.Instance;
			Register (type, adapter);
			return adapter;
		}

		public abstract void SetKey (object data, string key, object value);
		public abstract Type GetValueType (object data, string key);
		public abstract void WriteJson (object data, JsonWriter writer);
	}

	public class DictionaryAdapter : JsonAdapter {
		public static readonly DictionaryAdapter Instance = new DictionaryAdapter ();

		protected DictionaryAdapter ()
		{
		}

		public override void SetKey (object data, string key, object value)
		{
			((IDictionary<string, object>)data) [key] = value;
		}

		public override Type GetValueType (object data, string key)
		{
			return data.GetType ();
		}

		public override void WriteJson (object data, JsonWriter writer)
		{
			writer.WriteObject ((IDictionary<string, object>)data);
		}
	}

	public class ListAdapter : JsonAdapter {
		public static readonly ListAdapter Instance = new ListAdapter ();

		protected ListAdapter ()
		{
		}

		public override void SetKey (object data, string key, object value)
		{
			var list  = (IList)data;
			var index = int.Parse (key);

			if (list.Count == index)
				list.Add (value);
			else if (list.Count > index)
				list [index] = value;
			else
				throw new IndexOutOfRangeException (key);
		}

		public override Type GetValueType (object data, string key)
		{
			var type = data.GetType ();

			// For arrays
			if (type.HasElementType)
				type = type.GetElementType ();

			// For IList<T>
			else if (type.IsGenericType)
				type = type.GetGenericArguments () [0];

			// FIXME: This will rarely work if neither of the above conditions are met
			return type;
		}

		public override void WriteJson (object data, JsonWriter writer)
		{
			writer.WriteArray ((IList)data);
		}
	}

	public class PocoAdapter : JsonAdapter {
		public static readonly PocoAdapter Instance = new PocoAdapter ();

		protected PocoAdapter ()
		{
		}

		public override void SetKey (object data, string key, object value)
		{
			var type = data.GetType ();
			var prop = type.GetProperty (key);
			if (prop != null) {
				prop.SetValue (data, value, new object [0]);
				return;
			}
			var field = type.GetField (key);
			if (field != null) {
				field.SetValue (data, value);
				return;
			}
		}

		public override Type GetValueType (object data, string key)
		{
			var type = data.GetType ();
			var prop = type.GetProperty (key);
			if (prop != null) {
				return prop.PropertyType;
			} else {
				var field = type.GetField (key);
				if (field != null)
					return field.FieldType;
			}
			return JsonAdapter.DefaultType;
		}

		public override void WriteJson (object data, JsonWriter writer)
		{
			var type = data.GetType ();
			writer.StartObject ();
			foreach (var prop in type.GetProperties (BindingFlags.Public | BindingFlags.Instance)) {
				if (prop.IsSpecialName)
					continue;
				writer.WriteKey (prop.Name);
				writer.WriteValue (prop.GetValue (data, null));
			}
			foreach (var field in type.GetFields (BindingFlags.Public | BindingFlags.Instance)) {
				if (field.IsSpecialName || field.IsNotSerialized)
					continue;
				writer.WriteKey (field.Name);
				writer.WriteValue (field.GetValue (data));
			}
			writer.EndObject ();
		}
	}
}

