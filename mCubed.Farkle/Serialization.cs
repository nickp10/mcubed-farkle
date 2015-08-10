using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace mCubed.Farkle
{
	/// <summary>
	/// The serialization class for handling the serialization and deserialization process of .NET objects to and from XML
	/// </summary>
	public class Serialization : INotifyPropertyChanged
	{
		#region Static Members

		static readonly string _assembly = "Ser.Assembly";
		static readonly string _generic = "Ser.Generic";
		static readonly string _exceptionValue = "Ser.ExceptionValue";
		static readonly string _ienumerable = "Ser.IEnumerableItems";
		static readonly string _cachedValue = "Ser.Cache";
		static readonly Serialization _instance = new Serialization();

		/// <summary>
		/// Get the main serialization instance
		/// </summary>
		public static Serialization Instance { get { return _instance; } }

		/// <summary>
		/// Get the latest serialize or deserialized object from the main serialization instance
		/// </summary>
		/// <typeparam name="T">The type of object that will be returned</typeparam>
		/// <returns>The object that was used for the latest serialization or deserialization</returns>
		public static T GetObject<T>()
		{
			return (T)Instance.Object;
		}

		/// <summary>
		/// Set the object that will be used for the next serialization process
		/// </summary>
		/// <param name="obj">The object to set it to</param>
		public static void SetObject(object obj)
		{
			Instance.Object = obj;
		}

		/// <summary>
		/// Initialize the main serializiation instance with the file path for where the XML file is located when deserializing or where it should be located when serializing
		/// </summary>
		/// <param name="filePath">The file path for the XML file</param>
		public static void Initialize(string filePath)
		{
			Instance.FilePath = filePath;
		}

		/// <summary>
		/// Initialize the main serialization instance with the file path for the XML file and deserialize the file
		/// </summary>
		/// <param name="filePath">The file path for the XML file</param>
		/// <returns>The object that was deserialized or null if the process failed</returns>
		public static object InitializeAndLoad(string filePath)
		{
			return InitializeAndLoad<object>(filePath, null);
		}

		/// <summary>
		/// Initialize the main serialization instance with the file path for the XML file, deserialize the file, and return the given default object if the process failed
		/// </summary>
		/// <typeparam name="T">The type of object being deserialized</typeparam>
		/// <param name="filePath">The file path for the XML file</param>
		/// <param name="defaultObject">The default object to be returned if the deserialization process fails</param>
		/// <returns>The object that was deserialized or the default object if the process failed</returns>
		public static T InitializeAndLoad<T>(string filePath, T defaultObject)
		{
			Initialize(filePath);
			return Load(defaultObject);
		}

		/// <summary>
		/// Deserialize the XML file into an object
		/// </summary>
		/// <returns>The object that was deserialized from the file</returns>
		public static object Load()
		{
			return Load<object>(null);
		}

		/// <summary>
		/// Deserialize the XML file into an object and return the given default object if the process failed
		/// </summary>
		/// <typeparam name="T">The type of object being deserialized</typeparam>
		/// <param name="defaultObject">The default object to be returned if the deserialization process fails</param>
		/// <returns>The object that was deserialized or the default object if the process failed</returns>
		public static T Load<T>(T defaultObject)
		{
			object obj = Instance.Deserialize();
			return (T)(Instance.Object = (obj is T ? obj : defaultObject));
		}

		/// <summary>
		/// Serialize the latest serialized or deserialized object into an XML file
		/// </summary>
		/// <returns>True if the process succeeded with no errors, or false otherwise</returns>
		public static bool Save()
		{
			return Instance.Serialize();
		}

		/// <summary>
		/// Serialize the given object into an XML file
		/// </summary>
		/// <param name="obj">The object to serialize into the file</param>
		/// <returns>True if the process succeeded with no errors, or false otherwise</returns>
		public static bool Save(object obj)
		{
			return Instance.Serialize(obj);
		}

		#endregion

		#region SerializationCache

		class SerializationCache
		{
			public static int CacheCounter;
			public object NetObject { get; set; }
			public string CacheID { get; set; }
			public XElement XmlObject { get; set; }
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(params string[] properties)
		{
			if (PropertyChanged != null)
				properties.ToList().ForEach(prop => PropertyChanged(this, new PropertyChangedEventArgs(prop)));
		}

		#endregion

		#region Properties

		object _object;
		/// <summary>
		/// Get or set the object being used for the serialization or deserialization process
		/// </summary>
		public object Object
		{
			get { return _object; }
			set
			{
				if (_object != value)
				{
					_object = CanCreateObject(value) ? value : null;
					OnPropertyChanged("Object");
				}
			}
		}
		string _filePath;
		/// <summary>
		/// Get or set the file path to where the XML file is located or where it will be located
		/// </summary>
		public string FilePath
		{
			get { return _filePath; }
			set
			{
				if (_filePath != value)
				{
					_filePath = value;
					OnPropertyChanged("FilePath");
				}
			}
		}
		string AltFilePath
		{
			get { return Path.Combine(Path.GetTempPath(), Path.GetFileName(FilePath) ?? "libSerialization.xml"); }
		}
		List<SerializationCache> _saveCache = new List<SerializationCache>();
		List<SerializationCache> _loadCache = new List<SerializationCache>();
		bool _loading;

		#endregion

		#region Deserialization Members

		/// <summary>
		/// Deserialize the XML file into an object
		/// </summary>
		/// <returns>The object that was deserialized from the file</returns>
		public object Deserialize()
		{
			// Clear the cache
			if (_loading)
				return null;
			_loading = true;
			_loadCache.Clear();

			// Load the file and deserialize it
			XDocument doc = LoadFile(FilePath) ?? LoadFile(AltFilePath);
			Object = doc == null ? null : Deserialize(doc.Root);
			_loading = false;
			return Object;
		}

		/// <summary>
		/// Deserialize a given XML element into an object
		/// </summary>
		/// <param name="element">The XML element to deserialize</param>
		/// <returns>The object that was deserialized</returns>
		private object Deserialize(XElement element)
		{
			// Check the cache
			if (element.Name == _cachedValue)
			{
				var cache = _loadCache.SingleOrDefault(c => c.CacheID == element.Value);
				if (cache != null)
					return cache.NetObject;
			}

			// Create the root object
			object obj = null;
			Type type = LoadType(element);
			if (element.Attribute(_exceptionValue) != null)
				obj = Parse(element.Attribute(_exceptionValue).Value, type);
			else if (element.Element(_ienumerable) != null)
				obj = CreateEnumerable(element);
			else
				obj = Activator.CreateInstance(type);
			if (obj == null)
				return null;

			// Add the object to the cache
			if ((string)element.Attribute(_cachedValue) != null)
				_loadCache.Add(new SerializationCache { CacheID = (string)element.Attribute(_cachedValue), NetObject = obj, XmlObject = element });

			// Get the valid properties
			var valid = SerializeProperties(obj);

			// Check if its a key value pair
			if (type.FullName.StartsWith("System.Collections.Generic.KeyValuePair`2"))
			{
				object key = Deserialize(valid, GetXmlObject(element, "Key"), null);
				object value = Deserialize(valid, GetXmlObject(element, "Value"), null);
				if (key != null && value != null)
					return type.GetConstructor(new Type[] { key.GetType(), value.GetType() }).Invoke(new object[] { key, value });
			}

			// Go through the element's attributes and elements
			foreach (object var in element.Attributes().Cast<object>().Concat(element.Elements().Cast<object>()))
				Deserialize(valid, var, obj);
			return obj;
		}

		/// <summary>
		/// Deserialize a given attribute or element and set it as the property of a given object
		/// </summary>
		/// <param name="valid">The valid list of properties that can be set</param>
		/// <param name="xmlObject">The XML object that is being deserialized</param>
		/// <param name="netObject">The deserialized object will be set to the corresponding property value of this given .NET object</param>
		/// <returns>The object that was deserialized from the XML object</returns>
		private object Deserialize(IEnumerable<PropertyInfo> valid, object xmlObject, object netObject)
		{
			// Cast the XML object and check it
			XAttribute attr = xmlObject as XAttribute;
			XElement ele = xmlObject as XElement;
			if (attr == null && (ele == null || !ele.HasElements)) return null;

			// Get the valid properties and check it
			var prop = valid.SingleOrDefault(p => p.Name == (attr == null ? ele.Name.LocalName : attr.Name.LocalName));
			if (prop == null) return null;

			// Deserialize and return the value
			object obj = attr == null ? Deserialize(ele.Elements().First()) : Parse(attr.Value, prop.PropertyType);
			if (netObject != null)
				SetValue(netObject, obj, prop);
			return obj;
		}

		/// <summary>
		/// Get the XML object out of an XML element given the attribute or element name
		/// </summary>
		/// <param name="element">The element to find the attributes and elements of</param>
		/// <param name="property">The attribute or element name to find</param>
		/// <returns>The XML object from the given element with the given property name</returns>
		private object GetXmlObject(XElement element, string property)
		{
			return (object)element.Element(property) ?? element.Attribute(property);
		}

		/// <summary>
		/// Load the file located at the given path
		/// </summary>
		/// <param name="filePath">The path to the file to load</param>
		/// <returns>The XML document parsed from the loaded file</returns>
		private XDocument LoadFile(string filePath)
		{
			try { return XDocument.Load(filePath); }
			catch { return null; }
		}

		#endregion

		#region Serialization Members

		/// <summary>
		/// Serialize the object into a file
		/// </summary>
		/// <returns>True if the process succeeded with no errors, or false otherwise</returns>
		public bool Serialize()
		{
			return Serialize(Object);
		}

		/// <summary>
		/// Serialize the object into a file
		/// </summary>
		/// <param name="obj">The object to be serialized</param>
		/// <returns>True if the process succeeded with no errors, or false otherwise</returns>
		public bool Serialize(object obj)
		{
			// Clear the cache
			_saveCache.Clear();
			SerializationCache.CacheCounter = 0;

			// Serialize the given root
			if ((Object = obj) != null)
			{
				XDocument doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), Serializer(Object));
				return SaveFile(doc, FilePath) || SaveFile(doc, AltFilePath);
			}

			// Delete any created files since the serialization failed
			if (File.Exists(FilePath))
				File.Delete(FilePath);
			if (File.Exists(AltFilePath))
				File.Delete(AltFilePath);
			return false;
		}

		/// <summary>
		/// Serialize a given object into an XML element
		/// </summary>
		/// <param name="obj">The object to serialize</param>
		/// <returns>The XML serialization of the object</returns>
		private XElement Serializer(object obj)
		{
			// Check the cache
			var cache = _saveCache.SingleOrDefault(c => c.NetObject == obj);
			if (cache != null)
			{
				if ((string)cache.XmlObject.Attribute(_cachedValue) == null)
					cache.XmlObject.Add(new XAttribute(_cachedValue, cache.CacheID));
				return new XElement(_cachedValue, cache.CacheID);
			}

			// Create the element with its assembly and generics
			Type type = obj.GetType();
			var element = SaveType(type, type.FullName, false);
			if (IsAttribute(type))
				element.Add(new XAttribute(_exceptionValue, obj.ToString()));

			// Add object to the cache
			_saveCache.Add(new SerializationCache { CacheID = (++SerializationCache.CacheCounter).ToString(), NetObject = obj, XmlObject = element });

			// Serialize the properties
			foreach (var property in SerializeProperties(obj))
			{
				object value = GetValue(obj, property);
				if (value == null) continue;
				if (IsAttribute(property.PropertyType))
					element.Add(new XAttribute(property.Name, value.ToString()));
				else
					element.Add(new XElement(property.Name, Serializer(value)));
			}

			// Serialize the IEnumerable
			if (obj is System.Collections.IEnumerable && !(obj is string))
			{
				element.Add(SaveType(EnumerableType(type), _ienumerable, true));
				foreach (object item in (System.Collections.IEnumerable)obj)
					element.Element(_ienumerable).Add(Serializer(item));
			}

			return element;
		}

		/// <summary>
		/// Save the XML document to the given file path
		/// </summary>
		/// <param name="doc">The XML document to save</param>
		/// <param name="filePath">The path to save the file to</param>
		/// <returns>True if the file was saved successfully, or false otherwise</returns>
		private bool SaveFile(XDocument doc, string filePath)
		{
			try { doc.Save(filePath); return true; }
			catch { return false; }
		}

		#endregion

		#region Reflection Members

		private object Parse(string value, Type type)
		{
			var method = type.GetMethod("Parse", new[] { typeof(string) });
			if (method != null)
				return method.Invoke(null, new[] { value });
			if (type.IsEnum)
				return Enum.Parse(type, value);
			return value;
		}

		private object CreateEnumerable(XElement element)
		{
			Type ienumType = LoadType(element.Element(_ienumerable));
			Type eleType = LoadType(element);
			object ienum = element.Element(_ienumerable).Elements().Select(e => Deserialize(e));
			ienum = typeof(Enumerable).GetMethod("OfType").MakeGenericMethod(ienumType).Invoke(null, new object[] { ienum });
			ienum = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(ienumType).Invoke(null, new object[] { ienum });
			if (CanCreateObject(null, eleType))
			{
				try
				{
					var obj = Activator.CreateInstance(eleType);
					var inter = eleType.GetInterfaces().FirstOrDefault(i => i.FullName.StartsWith("System.Collections.Generic.ICollection`1"));
					if (inter != null)
					{
						var method = inter.GetMethod("Add", new Type[] { ienumType });
						foreach (object item in ((System.Collections.IEnumerable)ienum))
							method.Invoke(obj, new object[] { item });
					}
					ienum = obj;
				}
				catch { }
			}
			return ienum;
		}

		private Type EnumerableType(Type type)
		{
			var inter = type.GetInterfaces().SingleOrDefault(i => i.FullName.StartsWith("System.Collections.Generic.IEnumerable`1"));
			return inter == null ? typeof(object) : inter.GetGenericArguments().Single();
		}

		private Type LoadType(XElement element)
		{
			string assembly = (string)element.Attribute(_assembly);
			string typeName = (string)element.Attribute(_generic) ?? element.Name.LocalName;
			return assembly == null ? Type.GetType(typeName) : Assembly.Load(assembly).GetType(typeName);
		}

		private XElement SaveType(Type type, string name, bool forceGen)
		{
			var element = new XElement(name.Replace('+', '.').Replace("[", "").Replace("]", "").Split('`')[0]);
			if (type.Assembly.FullName != GetType().Assembly.FullName)
				element.Add(new XAttribute(_assembly, type.Assembly.FullName));
			if (forceGen || type.IsGenericType || type.IsNested || type.IsArray)
				element.Add(new XAttribute(_generic, type.FullName));
			return element;
		}

		private IEnumerable<PropertyInfo> SerializeProperties(object obj)
		{
			// Get the valid properties (public get/set and recreatable instance)
			var props = obj.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite && p.GetGetMethod() != null && p.GetSetMethod() != null);
			props = props.Where(p => IsAttribute(p.PropertyType) || CanCreateObject(GetValue(obj, p), p.PropertyType));

			// Filter out the properties according to user preference
			if (obj is ISerializable)
			{
				var valid = ((ISerializable)obj).GetSerializationList();
				props = props.Where(p => valid.Contains(p.Name));
			}

			// Add some known properties
			if (obj.GetType().FullName.StartsWith("System.Collections.Generic.KeyValuePair`2"))
				props = props.Concat(new[] { obj.GetType().GetProperty("Key"), obj.GetType().GetProperty("Value") });
			return props.ToList();
		}

		private bool CanCreateObject(object obj)
		{
			return obj == null ? false : CanCreateObject(obj, obj.GetType());
		}

		private bool CanCreateObject(object obj, Type type)
		{
			try
			{
				return obj is System.Collections.IEnumerable || type.GetInterface("System.Collections.IEnumerable") != null ||
					type == typeof(System.Collections.IEnumerable) || Activator.CreateInstance(obj == null ? type : obj.GetType()) != null;
			}
			catch { return false; }
		}

		private bool IsAttribute(Type type)
		{
			return type.IsEnum || type == typeof(string) || type.GetMethod("Parse", new Type[] { typeof(string) }) != null;
		}

		private object GetValue(object obj, PropertyInfo property)
		{
			return property.GetIndexParameters().Length == 0 ? property.GetValue(obj, null) : null;
		}

		private void SetValue(object obj, object value, PropertyInfo property)
		{
			if (property.GetIndexParameters().Length == 0)
				property.SetValue(obj, value, null);
		}

		#endregion
	}

	/// <summary>
	/// Implement this interface for further control of which valid properties will be serialized for a given object
	/// </summary>
	public interface ISerializable
	{
		IEnumerable<string> GetSerializationList();
	}
}
