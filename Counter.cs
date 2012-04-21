using System;
using System.Linq;
using System.Reflection;

using Cirrus;
using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	public struct Counter<TValue>
		where TValue : struct, IConvertible
	{
		internal TValue value, delta;

		public Counter (TValue initValue)
		{
			this.value = initValue;
			this.delta = default (TValue);
		}

		public void ResetValue (TValue newValue)
		{
			this.value = newValue;
			this.delta = default (TValue);
		}

		public static implicit operator TValue (Counter<TValue> c)
		{
			return (TValue)Convert.ChangeType (Convert.ToInt64 (c.value) + Convert.ToInt64 (c.delta), typeof (TValue));
		}

		public static Counter<TValue> FromObject (object valueInstance)
		{
			return new Counter<TValue> ((TValue)Convert.ChangeType (valueInstance, typeof (TValue)));
		}

		static Counter ()
		{
			JsonAdapter.Register (typeof (Counter<TValue>), new CounterAdapter (typeof (Counter<TValue>), typeof (TValue)));
		}
	}

	class CounterAdapter : JsonAdapter {

		Type counterType, valueType;

		public CounterAdapter (Type counterType, Type valueType)
		{
			this.counterType = counterType;
			this.valueType = valueType;
		}

		public override Type GetValueType (object data, string key)
		{
			return valueType;
		}

		public override Future SetKey (object data, string key, object value)
		{
			throw new NotSupportedException ();
		}

		public override Future WriteJson (object data, JsonWriter writer)
		{
			throw new NotSupportedException ();
		}
	}
}

