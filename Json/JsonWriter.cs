using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Parse.Json {

	public static class JsonExtensions {

		public static string ToJson (this object foo)
		{
			var writer = new JsonWriter ();
			foo.ToJson (writer);
			return writer.ToString ();
		}

		public static void ToJson (this object foo, JsonWriter writer)
		{
			writer.WriteValue (foo);
		}
	}
	public class JsonWriter {

		protected StringBuilder buffer;
		protected bool next_needs_comma;

		public JsonWriter ()
		{
			this.buffer = new StringBuilder ();
			this.next_needs_comma = false;
		}

		void WriteCommaIfNecessary ()
		{
			if (next_needs_comma) {
				buffer.Append (',');
				next_needs_comma = false;
			}
		}

		public void WriteObject (IEnumerable<KeyValuePair<string,object>> pairs)
		{
			WriteCommaIfNecessary ();
			buffer.Append ('{');

			foreach (var kv in pairs) {
				WriteKey (kv.Key);
				WriteValue (kv.Value);
			}

			buffer.Append ('}');
		}

		public void WriteArray (IEnumerable values)
		{
			WriteCommaIfNecessary ();
			buffer.Append ('[');

			foreach (var value in values)
				WriteValue (value);

			buffer.Append (']');
		}

		public void StartObject ()
		{
			WriteCommaIfNecessary ();
			buffer.Append ('{');
		}

		public void WriteKey (string key)
		{
			WriteCommaIfNecessary ();
			WriteString (key);
			buffer.Append (':');
			next_needs_comma = false;
		}

		public void WriteString (string str)
		{
			WriteCommaIfNecessary ();
			buffer.Append ('"');
			for (var i = 0; i < str.Length; i++) {
				var next = str [i];
				switch (next) {

				case '\a' : buffer.Append ("\\a"); break;
				case '\b' : buffer.Append ("\\b"); break;
				case '\f' : buffer.Append ("\\f"); break;
				case '\n' : buffer.Append ("\\n"); break;
				case '\r' : buffer.Append ("\\r"); break;
				case '\t' : buffer.Append ("\\t"); break;
				case '\v' : buffer.Append ("\\v"); break;
				case '\\' : buffer.Append ("\\\\"); break;
				default   : buffer.Append (next); break;
				}
			}
			buffer.Append ('"');
			next_needs_comma = true;
		}

		public void WriteValue (object value)
		{
			WriteCommaIfNecessary ();
			if (value == null) {
				buffer.Append ("null");

			} else if (value is string) {
				WriteString ((string)value);

			} else if (value is ValueType) {
				// FIXME: enums?
				buffer.Append (value.ToString ().ToLowerInvariant ());

			} else {
				JsonAdapter.ForType (value.GetType ()).WriteJson (value, this);
			}
			next_needs_comma = true;
		}

		public void EndObject ()
		{
			buffer.Append ('}');
			next_needs_comma = true;
		}

		public override string ToString ()
		{
			return buffer.ToString ();
		}
	}
}

