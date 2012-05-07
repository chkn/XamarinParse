using System;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {
	
	[AttributeUsage (AttributeTargets.Property)]
	public class ParseKeyAttribute : Attribute {
		public string Key { get; set; }
		public ParseKeyAttribute () { }
		public ParseKeyAttribute (string key) { this.Key = key; }
	}

	// used to annotate special parse types
	[AttributeUsage (AttributeTargets.Class)]
	public class ParseTypeAttribute : Attribute {
		public string Name { get; set; }
		public bool Inline { get; set; }
		public ParseTypeAttribute (string name) { this.Name = name; }
		public ParseTypeAttribute () { }
	}

	// Subclassing:
	//    If you want to add any C# properties that are saved to Parse, either:
	//      a. use the this[] indexer syntax for get/set OR
	//      b. annotate your auto-generated properties with the ParseKeyAttribute
	public class ParseObject : IDictionary<string,object>, INotifyPropertyChanged {

		// the API path to normal parse objects
		internal const string CLASSES_PATH = "classes";
		
		public string ObjectId {
			get { return (string) this ["objectId"]; }
			internal protected set {
				lock (property_lock) {
					properties ["objectId"] = value;
					ClassPath += "/" + value;
				}
				FirePropertyChanged ("ObjectId");
			}
		}
		public DateTime CreateDate {
			get { return (DateTime) this ["createdAt"]; }
			internal set {
				lock (property_lock)
					properties ["createdAt"] = value;
				FirePropertyChanged ("CreateDate");
			}
		}
		public DateTime ModifiedDate {
			get { return (DateTime) this ["updatedAt"]; }
			internal set {
				lock (property_lock)
					properties ["updatedAt"] = value;
				FirePropertyChanged ("ModifiedDate");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public string ClassPath { get; private set; }

		internal string pointerClassName;

		internal object property_lock;
		// ^ Must be locked on every access to either of the below:
		internal Dictionary<string,object> properties;
		internal HashSet<string> updated_properties;
		internal Dictionary<string,PropertyInfo> property_keys;
		
		public ParseObject (string className)
			: this (new[] { CLASSES_PATH, className })
		{
			if (className.StartsWith ("_"))
				throw new ArgumentException ("Class names cannot start with '_'");
			this.pointerClassName = className;
		}

		protected ParseObject (string [] apiPathParts)
		{
			this.ClassPath = string.Join ("/", apiPathParts);
			this.property_lock = new object ();
			this.properties = new Dictionary<string, object> ();
			this.updated_properties = new HashSet<string> ();
			
			var type = GetType ();
			if (type == typeof (ParseObject))
				return;

			foreach (var prop in type.GetProperties (BindingFlags.Public | BindingFlags.Instance)) {
				var attr = prop.GetCustomAttributes (typeof (ParseKeyAttribute), true).Cast<ParseKeyAttribute> ().SingleOrDefault ();
				if (attr == null)
					continue;
			
				if (property_keys == null)
					property_keys = new Dictionary<string, PropertyInfo> ();
				
				property_keys.Add (attr.Key ?? prop.Name, prop);
			}
		}

		public void Add (KeyValuePair<string, object> item)
		{
			Add (item.Key, item.Value);
		}

		public virtual void Add (string key, object value)
		{
			lock (property_lock) {
				properties.Add (key, value);
				updated_properties.Add (key);
			}
			FirePropertyChanged (key);
		}

		public void Clear ()
		{
			throw new NotSupportedException ();
		}

		public bool Contains (KeyValuePair<string, object> item)
		{
			lock (property_lock)
				return ((IDictionary<string,object>)properties).Contains (item);
		}

		public bool ContainsKey (string key)
		{
			lock (property_lock)
				return properties.ContainsKey (key);
		}

		public void CopyTo (KeyValuePair<string, object>[] array, int arrayIndex)
		{
			lock (property_lock)
				((IDictionary<string,object>)properties).CopyTo (array, arrayIndex);
		}

		public int Count {
			get { return properties.Count; }
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator ()
		{
			return properties.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return properties.GetEnumerator ();
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public virtual object this [string key] {
			get {
				object value;
				lock (property_lock) {
					if (properties.TryGetValue (key, out value))
						return value;
				}
				return null;
			}
			set {
				object oldValue;
				lock (property_lock) {
					if (!properties.TryGetValue (key, out oldValue) ||
					    (value != null && !value.Equals (oldValue)) ||
					    (oldValue != null && !oldValue.Equals (value))) {

						properties [key] = value;
						updated_properties.Add (key);
						
						PropertyInfo prop;
						if (property_keys != null && property_keys.TryGetValue (key, out prop))
							prop.SetValue (this, value, null);
						FirePropertyChanged (key);
					}
				}
			}
		}

		public ICollection<string> Keys {
			get { return properties.Keys; }
		}

		public ICollection<object> Values {
			get { return properties.Values; }
		}

		public bool Remove (KeyValuePair<string, object> item)
		{
			return Remove (item.Key);
		}

		public virtual bool Remove (string key)
		{
			bool result;
			lock (property_lock) {
				if (result = properties.Remove (key)) {
					updated_properties.Add (key);
					FirePropertyChanged (key);
				}
			}
			return result;
		}

		public bool TryGetValue (string key, out object value)
		{
			lock (property_lock)
				return properties.TryGetValue (key, out value);
		}

		public Future Save ()
		{
			if (ObjectId == null)
				return CallAndApply (Parse.Verb.POST); // create
			else
				return CallAndApply (Parse.Verb.PUT); // update
		}

		public Future Delete ()
		{
			return CallAndApply (Parse.Verb.DELETE, false);
		}

		public Future Refresh ()
		{
			return CallAndApply (Parse.Verb.GET, false, true);
		}

		Future CallAndApply (Parse.Verb verb, bool sendData = true, bool sendId = false)
		{
			var data = sendData? this.ToJson ().Wait () : null;
			if (string.IsNullOrEmpty (data) || data == "{}")
				return Future.Fulfilled;

			var response = Parse.ApiCall (verb, ClassPath + (sendId? "/" + ObjectId : ""), data).Wait ();
			Parse.ThrowIfNecessary (response);

			var status = (int)response.StatusCode;
			if (status != 204 && status != 205) {
				var dict = JsonReader.Read<ParseObject> (response.GetResponseStream (), GetType ()).Wait ();
				ParseObjectAdapter.Instance.Apply (this, dict);
			}
			response.Close ();

			updated_properties.Clear ();
			return Future.Fulfilled;
		}

		void FirePropertyChanged (string propName)
		{
			var propChanged = PropertyChanged;
			if (propChanged != null)
				propChanged (this, new PropertyChangedEventArgs (propName));
		}
	}
}

