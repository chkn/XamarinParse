using System;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	// Subclassing:
	//    If you want to add any C# properties that are saved to Parse,
	//    use the this[] indexer syntax for get/set.
	public class ParseObject : IDictionary<string,object>, INotifyPropertyChanged {

		// the API path to normal parse objects
		internal const string CLASSES_PATH = "classes";

		public string Id {
			get { return (string) this ["objectId"]; }
			internal set {
				lock (property_lock) {
					properties ["objectId"] = value;
					ClassPath += "/" + value;
				}
				FirePropertyChanged ("Id");
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

		internal object property_lock;
		// ^ Must be locked on every access to either of the below:
		internal Dictionary<string,object> properties;
		internal HashSet<string> updated_properties;

		public ParseObject (string className)
			: this (new[] { CLASSES_PATH, className })
		{
		}

		protected ParseObject (string [] apiPathParts)
		{
			this.ClassPath = string.Join ("/", apiPathParts);
			this.property_lock = new object ();
			this.properties = new Dictionary<string, object> ();
			this.updated_properties = new HashSet<string> ();
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

		public Task Save () {
			if (Id == null)
				return CallAndApply (Parse.Verb.POST); // create
			else
				return CallAndApply (Parse.Verb.PUT); // update
		}

		public Task Delete ()
		{
			return CallAndApply (Parse.Verb.DELETE, false);
		}

		Task CallAndApply (Parse.Verb verb, bool sendData = true)
		{
			return Parse.ApiCall (verb, ClassPath, sendData? this.ToJson () : null).ContinueWith (task => {
				Parse.ThrowIfNecessary (task);
				var status = (int)task.Result.StatusCode;
				if (status != 204 && status != 205) {
					var dict = JsonReader.Read<Dictionary<string,object>> (task.Result.GetResponseStream ());
					ParseObjectAdapter.Instance.Apply (this, dict);
				}
				task.Result.Close ();
			});
		}

		void FirePropertyChanged (string propName)
		{
			var propChanged = PropertyChanged;
			if (propChanged != null)
				propChanged (this, new PropertyChangedEventArgs (propName));
		}

		static ParseObject ()
		{
			JsonAdapter.Register (typeof (ParseObject), ParseObjectAdapter.Instance);
		}
	}
}

