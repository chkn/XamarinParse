using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.Parse.Json {

	public sealed class JsonReader {

		TextReader input;
		bool eof;
		uint row, col;

		public static T Read<T> (string fileName)
		{
			return Read<T> (new FileStream (fileName, FileMode.Open));
		}

		public static T Read<T> (Stream stream)
		{
			return JsonReader.Parse<T> (new StreamReader (stream));
		}

		public static T Parse<T> (string json)
		{
			return JsonReader.Parse<T> (new StringReader (json));
		}

		public static T Parse<T> (TextReader json)
		{
			var jso = new JsonReader (json);
			return (T)Convert.ChangeType (jso.Parse (typeof (T)), typeof (T));
		}

		private JsonReader (TextReader input)
		{
			this.input = input;
		}

		object Parse (Type typeHint)
		{
			var la = Peek ();
			
			/* value:
			 *     string
			 *     number
			 *     object
			 *     array
			 *     true
			 *     false
			 *     null
			 */
		
			if (la == '"')
				return ParseString ();
			if (char.IsDigit (la) || la == '-')
				return ParseNumber (typeHint);
			if (la == '{')
				return ParseObject (typeHint);
			if (la == '[')
				return ParseArray (typeHint);
			if (la == 't') {
				Consume ("true");
				return true;
			}
			if (la == 'f') {
				Consume ("false");
				return false;
			}
			if (la == 'n') {
				Consume ("null");
				return null;
			}
			
			Err ("unexpected '{0}'", la);
			return null;
		}
		
		string ParseString ()
		{
			var next = Peek ();
			var sb = new StringBuilder ();
			var escaped = false;

			int unicodeShift = -1;
			int unicodeSeq = 0;
			
			if (next != '"')
				Err ("expected string");
			
			Consume ();
			next = Consume ();
			
			while (next != '"' || escaped) {

				if (!escaped && next == '\\') {
				
					escaped = true;

				} else if (unicodeShift >= 0) {

					if (char.IsDigit (next))
						unicodeSeq |= unchecked((int)(next - 48)) << unicodeShift;
					else if (char.IsLetter (next))
						unicodeSeq |= unchecked((int)(char.ToUpperInvariant (next) - 65) + 10) << unicodeShift;
					else
						Err ("malformed Unicode escape sequence");

					unicodeShift -= 4;
					if (unicodeShift < 0) {
						sb.Append ((char)unicodeSeq);
						unicodeSeq = 0;
					}

				} else if (escaped) {

					if (next == 'u') {

						unicodeShift = 12;

					} else {

						switch (next) {
						
						case 'a' : next = '\a'; break;
						case 'b' : next = '\b'; break;
						case 'f' : next = '\f'; break;
						case 'n' : next = '\n'; break;
						case 'r' : next = '\r'; break;
						case 't' : next = '\t'; break;
						case 'v' : next = '\v'; break;
						}

						sb.Append (next);
					}

					escaped = false;
					
				} else {
				
					sb.Append (next);
				}
				
				next = Consume ();
			}

			if (unicodeShift >= 0)
				Err ("malformed Unicode escape sequence");
			
			return sb.ToString ();
		}
		
		object ParseNumber (Type typeHint)
		{
			/* number:
			 *    int
			 *    int frac
			 *    int exp
			 *    int frac exp 
			 */
			
			var next = Peek ();
			var sb = new StringBuilder ();
			
			while (char.IsDigit (next) || next == '-' || next == '.' || next == 'e' || next == 'E') {
				Consume ();
				sb.Append (next);
				next = Peek (true);
			}

			var result = double.Parse (sb.ToString ());
			var resultType = GetWorkingType (typeHint);

			if (IsNumeric (resultType))
				return Convert.ChangeType (result, resultType);

			return result;
		}
		
		object ParseObject (Type typeHint)
		{
			var adapter = JsonAdapter.ForType (typeHint);
			var next    = Peek ();
			if (next != '{')
				Err ("expected object literal");
			
			Consume ();
			var result = Activator.CreateInstance (typeHint);

			next = Peek ();
			while (next != '}') {

				var key = ParseString ();
				Consume (':');

				var value = Parse (adapter.GetValueType (result, key));
				adapter.SetKey (result, key, value);

				next = Peek ();
				if (next != '}')
					Consume (',');
			}
			
			Consume ('}');
			return result;
		}
		
		object ParseArray (Type typeHint)
		{
			var adapter = JsonAdapter.ForType (typeHint);
			var next = Peek ();
			if (next != '[')
				Err ("expected array literal");
			
			Consume ();
			var result = Activator.CreateInstance (GetWorkingType (typeHint));

			int i = 0;
			next = Peek ();
			while (next != ']') {
			
				var value = Parse (adapter.GetValueType (result, i.ToString ()));
				adapter.SetKey (result, i.ToString (), value);

				i++;
				next = Peek ();
				if (next != ']')
					Consume (',');
			}
			Consume (']');

			if (typeHint.IsArray) {
				typeHint = typeHint.GetElementType ();
				var list  = (IList)result;
				var array = Array.CreateInstance (typeHint, i);
				for (var j = 0; j < i; j++)
					array.SetValue (Convert.ChangeType (list [j], typeHint), j);
				return array;
			}
			return result;
		}
		
		// scanner primitives:
		
		void Consume (string expected)
		{
			for (var i = 0; i < expected.Length; i++) {
				
				var actual = Peek (true);
				if (eof || actual != expected [i])
					Err ("expected '{0}'", expected);
				
				Consume ();
			}
		}
		
		void Consume (char expected)
		{
			var actual = Peek ();
			if (eof || actual != expected)
				Err ("expected '{0}'", expected);
			
			while (Consume () != actual)
				{ col++; /* eat whitespace */ }
			col++;
		}
		
		char Consume ()
		{
			var r = input.Read ();
			if (r == -1) {
				Err ("unexpected EOF");
			}
		
			col++;
			return (char)r;
		}
		
		char Peek ()
		{
			return Peek (false);
		}
		
		char Peek (bool whitespaceSignificant)
		{
		top:
			var p = input.Peek ();
			if (p == -1) {
				eof = true;
				return (char)0;
			}
		
			var la = (char)p;
			
			if (!whitespaceSignificant) {
				if (la == '\r') {
					input.Read ();
					if (((char)input.Peek ()) == '\n')
						input.Read ();
					
					col = 1;
					row++;
					goto top;
				}
			
				if (la == '\n') {
					input.Read ();
					col = 1;
					row++;
					goto top;
				}
				
				if (char.IsWhiteSpace (la)) {
					Consume ();
					goto top;
				}
			}
			
			return la;
		}

		static Type GetWorkingType (Type typeHint)
		{
			if (typeHint.IsArray)
				typeHint = typeof (ArrayList);
			if (typeHint.IsGenericType && typeHint.GetGenericTypeDefinition () == typeof (Nullable<>))
				return typeHint.GetGenericArguments () [0];
			return typeHint;
		}

		static bool IsNumeric (Type typ)
		{
			switch ((int)Type.GetTypeCode (typ))
			{
			case 3:
			case 6:
			case 7:
			case 9:
			case 11:
			case 13:
			case 14:
			case 15:
			   return true;
			};
			return false;
		}

		void Err (string message, params object [] args)
		{
			Consume ();
			throw new JsonParseException (row, col - 1, string.Format (message, args));
		}
		
	}
	
	public class JsonParseException : Exception {

		public uint Row { get; private set; }
		public uint Column { get; private set; }
		public string InnerMessage { get; private set; }

		public JsonParseException (uint row, uint col, string message)
			: base (string.Format ("At ({0},{1}): {2}", row, col, message))
		{
			this.Row = row;
			this.Column = col;
			this.InnerMessage = message;
		}
	}
}