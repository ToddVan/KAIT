using System;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json
{
    /// <summary>
    /// This convert can be sued on any interface definition to instruct the JSON
    /// serializer to use a specific concrete class when de-serializing the instance.
    /// </summary>
    /// <typeparam name="TInterface">The Type that was serialized into the JSON text.</typeparam>
    /// <typeparam name="TConcrete">The Tyep that specifies the class that will be created.</typeparam>
    internal class InterfaceToConcreteConverter<TInterface, TConcrete> : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>Returns true if this instance can convert the specified object type, false otherwise.</returns>
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TInterface));
        }

        /// <summary>
        /// Gets a value indicating whether this Newtonsoft.Json.JsonConverter can read.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this Newtonsoft.Json.JsonConverter can write
        /// JSON.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The Newtonsoft.Json.JsonReader to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object returnValue = null;

            if (objectType == typeof(TInterface))
            {
                // ***
                // *** Create the concrete type
                // ***
                returnValue = Activator.CreateInstance<TConcrete>();

                // ***
                // *** Deserialize the object to a temporary instance
                // ***
                var jsonObject = JObject.Load(reader);

                // ***
                // *** Populate the object
                // ***
                serializer.Populate(jsonObject.CreateReader(), returnValue);
            }

            return returnValue;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The Newtonsoft.Json.JsonWriter to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // ***
            // *** This method will not be called because CanWrite is returning false. JSON.Net
            // *** will use default logic to serial the object. This is Ok because it will never
            // *** be given an instance of an interface; only concrete classes
            // ***
            throw new NotImplementedException();
        }
    }
}
