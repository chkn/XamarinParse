using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Util;

namespace Xamarin.Parse.Json {

	public abstract class JsonAdapter {

		static readonly OrderedDictionary<Type,JsonAdapter> adapters = new OrderedDictionary<Type,JsonAdapter> ();

		// The default type that should be returned by GetValueType if there is no better alternative
		public static readonly Type DefaultType = typeof (Dictionary<string,object>);

		static JsonAdapter ()
		{
#if MONOTOUCH
			List<KeyValuePair<Type,JsonAdapter>> foo = null;
#endif
			Register (typeof (IDictionary<string,object>), new DictionaryAdapter());
			Register (typeof (ParseObject), new ParseObjectAdapter ());
		}

		public static void Register (Type type, JsonAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException ("adapter");
			lock (adapters)
				adapters.Insert (0, type, adapter);
		}

		public static JsonAdapter ForType (Type type)
		{
			if (type == null)
				return null;

			JsonAdapter adapter;
			if (adapters.TryGetValue (type, out adapter))
				return adapter;

			foreach (var pair in adapters) {
				if (pair.Key.IsAssignableFrom (type))
					return pair.Value;
			}

			if (typeof (IList).IsAssignableFrom (type))
				return new ListAdapter (type.HasElementType? type.GetElementType () : null);

			adapter = PocoAdapter.Instance;
			Register (type, adapter);
			return adapter;
		}

		public abstract Future SetKey (object data, string key, object value);
		public abstract Type GetValueType (object data, string key);
		public abstract Future WriteJson (object data, JsonWriter writer);
	}

	public class DictionaryAdapter : JsonAdapter {

		protected internal DictionaryAdapter ()
		{
		}

		public override Future SetKey (object data, string key, object value)
		{
			((IDictionary<string, object>)data) [key] = value;
			return Future.Fulfilled;
		}

		public override Type GetValueType (object data, string key)
		{
			return data.GetType ();
		}

		public override Future WriteJson (object data, JsonWriter writer)
		{
			return writer.WriteObject ((IDictionary<string, object>)data);
		}
	}

	public class ListAdapter : JsonAdapter {

		Type elementType;

		protected internal ListAdapter (Type elementType)
		{
			this.elementType = elementType;
		}

		public override Future SetKey (object data, string key, object value)
		{
			var list  = (IList)data;
			var index = int.Parse (key);

			if (list.Count == index)
				list.Add (value);
			else if (list.Count > index)
				list [index] = value;
			else
				throw new IndexOutOfRangeException (key);
			
			return Future.Fulfilled;
		}

		public override Type GetValueType (object data, string key)
		{
			if (elementType != null)
				return elementType;

			var type = data.GetType ();

			// For arrays
			if (type.HasElementType)
				type = type.GetElementType ();

			// For IList<T>
			else if (type.IsGenericType)
				type = type.GetGenericArguments () [0];

			// FIXME: This is a bit shaky?
			return DefaultType;
		}

		public override Future WriteJson (object data, JsonWriter writer)
		{
			return writer.WriteArray ((IList)data);
		}
	}

	public class PocoAdapter : JsonAdapter {
		public static readonly PocoAdapter Instance = new PocoAdapter ();

		protected PocoAdapter ()
		{
		}

		public override Future SetKey (object data, string key, object value)
		{
			var type = data.GetType ();
			var prop = type.GetProperty (key);
			if (prop != null) {
				prop.SetValue (data, value, new object [0]);
				return Future.Fulfilled;
			}
			var field = type.GetField (key);
			if (field != null)
				field.SetValue (data, value);
			return Future.Fulfilled;
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

		public override Future WriteJson (object data, JsonWriter writer)
		{
			var type = data.GetType ();
			writer.StartObject ();
			foreach (var prop in type.GetProperties (BindingFlags.Public | BindingFlags.Instance)) {
				if (prop.IsSpecialName)
					continue;
				writer.WriteKey (prop.Name);
				writer.WriteValue (prop.GetValue (data, null)).Wait ();
			}
			foreach (var field in type.GetFields (BindingFlags.Public | BindingFlags.Instance)) {
				if (field.IsSpecialName || field.IsNotSerialized)
					continue;
				writer.WriteKey (field.Name);
				writer.WriteValue (field.GetValue (data)).Wait ();
			}
			writer.EndObject ();
			return Future.Fulfilled;
		}
	}
}

