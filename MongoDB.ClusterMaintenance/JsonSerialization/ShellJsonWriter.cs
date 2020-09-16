using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.ClusterMaintenance.JsonSerialization
{
	public class ShellJsonWriter : BsonWriter
	{
		public ShellJsonWriter(TextWriter writer) : base(new JsonWriterSettings(){Indent = false, OutputMode = JsonOutputMode.Shell})
		{
			_textWriter = writer ?? throw new ArgumentNullException(nameof(writer));
			_context = new JsonWriterContext(null, ContextType.TopLevel);
			State = BsonWriterState.Initial;
		}

		private readonly TextWriter _textWriter;
		private JsonWriterContext _context;

		public override long Position => 0L;

		public override void Close()
		{
			if (State == BsonWriterState.Closed)
				return;
			Flush();
			_context = null;
			State = BsonWriterState.Closed;
		}

		public override void Flush()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			_textWriter.Flush();
		}

		public override void WriteBinaryData(BsonBinaryData binaryData)
		{
			if (Disposed)
				throw new ObjectDisposedException("JsonWriter");

			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
				ThrowInvalidState("WriteBinaryData", BsonWriterState.Value, BsonWriterState.Initial);

			var subType = binaryData.SubType;
			var bytes = binaryData.Bytes;
#pragma warning disable 618
			GuidRepresentation guidRepresentation;
			if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
			{
				guidRepresentation = binaryData.GuidRepresentation;
			}
			else
			{
				guidRepresentation = subType == BsonBinarySubType.UuidStandard
					? GuidRepresentation.Standard
					: GuidRepresentation.Unspecified;
			}
#pragma warning restore 618

			WriteNameHelper(Name);

			switch (subType)
			{
				case BsonBinarySubType.UuidLegacy:
				case BsonBinarySubType.UuidStandard:
					_textWriter.Write(GuidToString(subType, bytes, guidRepresentation));
					break;

				default:
					_textWriter.Write("new BinData({0}, \"{1}\")", (int) subType, Convert.ToBase64String(bytes));
					break;
			}

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON Boolean to the writer.
		/// </summary>
		/// <param name="value">The Boolean value.</param>
		public override void WriteBoolean(bool value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteBoolean", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write(value ? "true" : "false");

			State = GetNextState();
		}

		/// <summary>
		/// Writes BSON binary data to the writer.
		/// </summary>
		/// <param name="bytes">The bytes.</param>
		public override void WriteBytes(byte[] bytes)
		{
			WriteBinaryData(new BsonBinaryData(bytes, BsonBinarySubType.Binary));
		}

		/// <summary>
		/// Writes a BSON DateTime to the writer.
		/// </summary>
		/// <param name="value">The number of milliseconds since the Unix epoch.</param>
		public override void WriteDateTime(long value)
		{
			if (Disposed)
			{
				throw new ObjectDisposedException("JsonWriter");
			}

			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteDateTime", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);

			// use ISODate for values that fall within .NET's DateTime range, and "new Date" for all others
			if (value >= BsonConstants.DateTimeMinValueMillisecondsSinceEpoch &&
			    value <= BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
			{
				var utcDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(value);
				_textWriter.Write("ISODate(\"{0}\")", utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
			}
			else
			{
				_textWriter.Write("new Date({0})", value);
			}

			State = GetNextState();
		}

		/// <inheritdoc />
		public override void WriteDecimal128(Decimal128 value)
		{
			if (Disposed)
			{
				throw new ObjectDisposedException("JsonWriter");
			}

			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState(nameof(WriteDecimal128), BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);

			_textWriter.Write("NumberDecimal(\"{0}\")", value.ToString());

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON Double to the writer.
		/// </summary>
		/// <param name="value">The Double value.</param>
		public override void WriteDouble(double value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteDouble", BsonWriterState.Value, BsonWriterState.Initial);
			}

			// if string representation looks like an integer add ".0" so that it looks like a double
			var stringRepresentation = JsonConvert.ToString(value);
			if (Regex.IsMatch(stringRepresentation, @"^[+-]?\d+$"))
			{
				stringRepresentation += ".0";
			}

			WriteNameHelper(Name);

			_textWriter.Write("NumberDouble({0})", stringRepresentation);

			State = GetNextState();
		}

		/// <summary>
		/// Writes the end of a BSON array to the writer.
		/// </summary>
		public override void WriteEndArray()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value)
			{
				ThrowInvalidState("WriteEndArray", BsonWriterState.Value);
			}

			base.WriteEndArray();
			_textWriter.Write("]");

			_context = _context.ParentContext;
			State = GetNextState();
		}

		/// <summary>
		/// Writes the end of a BSON document to the writer.
		/// </summary>
		public override void WriteEndDocument()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Name)
			{
				ThrowInvalidState("WriteEndDocument", BsonWriterState.Name);
			}

			base.WriteEndDocument();
			_textWriter.Write(" }");

			if (_context.ContextType == ContextType.ScopeDocument)
			{
				_context = _context.ParentContext;
				WriteEndDocument();
			}
			else
			{
				_context = _context.ParentContext;
			}

			State = _context == null
				? BsonWriterState.Done
				: GetNextState();
		}

		/// <summary>
		/// Writes a BSON Int32 to the writer.
		/// </summary>
		/// <param name="value">The Int32 value.</param>
		public override void WriteInt32(int value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteInt32", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);

			_textWriter.Write("NumberInt({0})", value);

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON Int64 to the writer.
		/// </summary>
		/// <param name="value">The Int64 value.</param>
		public override void WriteInt64(long value)
		{
			if (Disposed)
			{
				throw new ObjectDisposedException("JsonWriter");
			}

			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteInt64", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);

			if (value >= int.MinValue && value <= int.MaxValue)
			{
				_textWriter.Write("NumberLong({0})", value);
			}
			else
			{
				_textWriter.Write("NumberLong(\"{0}\")", value);
			}

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON JavaScript to the writer.
		/// </summary>
		/// <param name="code">The JavaScript code.</param>
		public override void WriteJavaScript(string code)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteJavaScript", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("{{ \"$code\" : \"{0}\" }}", EscapedString(code));

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
		/// </summary>
		/// <param name="code">The JavaScript code.</param>
		public override void WriteJavaScriptWithScope(string code)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteJavaScriptWithScope", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteStartDocument();
			WriteName("$code");
			WriteString(code);
			WriteName("$scope");

			State = BsonWriterState.ScopeDocument;
		}

		/// <summary>
		/// Writes a BSON MaxKey to the writer.
		/// </summary>
		public override void WriteMaxKey()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteMaxKey", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("MaxKey");

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON MinKey to the writer.
		/// </summary>
		public override void WriteMinKey()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteMinKey", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("MinKey");

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON null to the writer.
		/// </summary>
		public override void WriteNull()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteNull", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("null");

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON ObjectId to the writer.
		/// </summary>
		/// <param name="objectId">The ObjectId.</param>
		public override void WriteObjectId(ObjectId objectId)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteObjectId", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("ObjectId(\"{0}\")", objectId.ToString());

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON regular expression to the writer.
		/// </summary>
		/// <param name="regex">A BsonRegularExpression.</param>
		public override void WriteRegularExpression(BsonRegularExpression regex)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteRegularExpression", BsonWriterState.Value, BsonWriterState.Initial);
			}

			var pattern = regex.Pattern;
			var options = regex.Options;

			WriteNameHelper(Name);
			var escapedPattern = (pattern == "") ? "(?:)" : pattern.Replace("/", @"\/");
			_textWriter.Write("/{0}/{1}", escapedPattern, options);

			State = GetNextState();
		}

		/// <summary>
		/// Writes the start of a BSON array to the writer.
		/// </summary>
		public override void WriteStartArray()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteStartArray", BsonWriterState.Value, BsonWriterState.Initial);
			}

			base.WriteStartArray();
			WriteNameHelper(Name);
			_textWriter.Write("[");

			_context = new JsonWriterContext(_context, ContextType.Array);
			State = BsonWriterState.Value;
		}

		/// <summary>
		/// Writes the start of a BSON document to the writer.
		/// </summary>
		public override void WriteStartDocument()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial && State != BsonWriterState.ScopeDocument)
			{
				ThrowInvalidState("WriteStartDocument", BsonWriterState.Value, BsonWriterState.Initial, BsonWriterState.ScopeDocument);
			}

			base.WriteStartDocument();
			if (State == BsonWriterState.Value || State == BsonWriterState.ScopeDocument)
			{
				WriteNameHelper(Name);
			}
			_textWriter.Write("{");

			var contextType = (State == BsonWriterState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
			_context = new JsonWriterContext(_context, contextType);
			State = BsonWriterState.Name;
		}

		/// <summary>
		/// Writes a BSON String to the writer.
		/// </summary>
		/// <param name="value">The String value.</param>
		public override void WriteString(string value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteString", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			WriteQuotedString(value);

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON Symbol to the writer.
		/// </summary>
		/// <param name="value">The symbol.</param>
		public override void WriteSymbol(string value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteSymbol", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("{{ \"$symbol\" : \"{0}\" }}", EscapedString(value));

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON timestamp to the writer.
		/// </summary>
		/// <param name="value">The combined timestamp/increment value.</param>
		public override void WriteTimestamp(long value)
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteTimestamp", BsonWriterState.Value, BsonWriterState.Initial);
			}

			var secondsSinceEpoch = (uint)((value >> 32) & 0xffffffff);
			var increment = (uint)(value & 0xffffffff);

			WriteNameHelper(Name);
			_textWriter.Write("Timestamp({0}, {1})", secondsSinceEpoch, increment);

			State = GetNextState();
		}

		/// <summary>
		/// Writes a BSON undefined to the writer.
		/// </summary>
		public override void WriteUndefined()
		{
			if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
			if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
			{
				ThrowInvalidState("WriteUndefined", BsonWriterState.Value, BsonWriterState.Initial);
			}

			WriteNameHelper(Name);
			_textWriter.Write("undefined");

			State = GetNextState();
		}

		// protected methods
		/// <summary>
		/// Disposes of any resources used by the writer.
		/// </summary>
		/// <param name="disposing">True if called from Dispose.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				try
				{
					Close();
				}
				catch { } // ignore exceptions
			}
			base.Dispose(disposing);
		}

		// private methods
		private string EscapedString(string value)
		{
			if (value.All(c => !NeedsEscaping(c)))
			{
				return value;
			}

			var sb = new StringBuilder(value.Length);

			foreach (char c in value)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						switch (CharUnicodeInfo.GetUnicodeCategory(c))
						{
							case UnicodeCategory.UppercaseLetter:
							case UnicodeCategory.LowercaseLetter:
							case UnicodeCategory.TitlecaseLetter:
							case UnicodeCategory.OtherLetter:
							case UnicodeCategory.DecimalDigitNumber:
							case UnicodeCategory.LetterNumber:
							case UnicodeCategory.OtherNumber:
							case UnicodeCategory.SpaceSeparator:
							case UnicodeCategory.ConnectorPunctuation:
							case UnicodeCategory.DashPunctuation:
							case UnicodeCategory.OpenPunctuation:
							case UnicodeCategory.ClosePunctuation:
							case UnicodeCategory.InitialQuotePunctuation:
							case UnicodeCategory.FinalQuotePunctuation:
							case UnicodeCategory.OtherPunctuation:
							case UnicodeCategory.MathSymbol:
							case UnicodeCategory.CurrencySymbol:
							case UnicodeCategory.ModifierSymbol:
							case UnicodeCategory.OtherSymbol:
								sb.Append(c);
								break;
							default:
								sb.AppendFormat("\\u{0:x4}", (int)c);
								break;
						}
						break;
				}
			}

			return sb.ToString();
		}

		private BsonWriterState GetNextState()
		{
			if (_context.ContextType == ContextType.Array || _context.ContextType == ContextType.TopLevel)
			{
				return BsonWriterState.Value;
			}
			else
			{
				return BsonWriterState.Name;
			}
		}

		private string GuidToString(BsonBinarySubType subType, byte[] bytes, GuidRepresentation guidRepresentation)
		{
			if (bytes.Length != 16)
			{
				var message = string.Format("Length of binary subtype {0} must be 16, not {1}.", subType, bytes.Length);
				throw new ArgumentException(message);
			}
			if (subType == BsonBinarySubType.UuidLegacy && guidRepresentation == GuidRepresentation.Standard)
			{
				throw new ArgumentException("GuidRepresentation for binary subtype UuidLegacy must not be Standard.");
			}
			if (subType == BsonBinarySubType.UuidStandard && guidRepresentation != GuidRepresentation.Standard)
			{
				var message = string.Format("GuidRepresentation for binary subtype UuidStandard must be Standard, not {0}.", guidRepresentation);
				throw new ArgumentException(message);
			}

			if (guidRepresentation == GuidRepresentation.Unspecified)
			{
				var s = BsonUtils.ToHexString(bytes);
				var parts = new string[]
				{
					s.Substring(0, 8),
					s.Substring(8, 4),
					s.Substring(12, 4),
					s.Substring(16, 4),
					s.Substring(20, 12)
				};
				return string.Format("HexData({0}, \"{1}\")", (int)subType, string.Join("-", parts));
			}
			else
			{
				string uuidConstructorName;
				switch (guidRepresentation)
				{
					case GuidRepresentation.CSharpLegacy: uuidConstructorName = "CSUUID"; break;
					case GuidRepresentation.JavaLegacy: uuidConstructorName = "JUUID"; break;
					case GuidRepresentation.PythonLegacy: uuidConstructorName = "PYUUID"; break;
					case GuidRepresentation.Standard: uuidConstructorName = "UUID"; break;
					default: throw new BsonInternalException("Unexpected GuidRepresentation");
				}
				var guid = GuidConverter.FromBytes(bytes, guidRepresentation);
				return string.Format("{0}(\"{1}\")", uuidConstructorName, guid.ToString());
			}
		}

		private bool NeedsEscaping(char c)
		{
			switch (c)
			{
				case '"':
				case '\\':
				case '\b':
				case '\f':
				case '\n':
				case '\r':
				case '\t':
					return true;

				default:
					switch (CharUnicodeInfo.GetUnicodeCategory(c))
					{
						case UnicodeCategory.UppercaseLetter:
						case UnicodeCategory.LowercaseLetter:
						case UnicodeCategory.TitlecaseLetter:
						case UnicodeCategory.OtherLetter:
						case UnicodeCategory.DecimalDigitNumber:
						case UnicodeCategory.LetterNumber:
						case UnicodeCategory.OtherNumber:
						case UnicodeCategory.SpaceSeparator:
						case UnicodeCategory.ConnectorPunctuation:
						case UnicodeCategory.DashPunctuation:
						case UnicodeCategory.OpenPunctuation:
						case UnicodeCategory.ClosePunctuation:
						case UnicodeCategory.InitialQuotePunctuation:
						case UnicodeCategory.FinalQuotePunctuation:
						case UnicodeCategory.OtherPunctuation:
						case UnicodeCategory.MathSymbol:
						case UnicodeCategory.CurrencySymbol:
						case UnicodeCategory.ModifierSymbol:
						case UnicodeCategory.OtherSymbol:
							return false;

						default:
							return true;
					}
			}
		}

		private void WriteNameHelper(string name)
		{
			switch (_context.ContextType)
			{
				case ContextType.Array:
					// don't write Array element names in Json
					if (_context.HasElements)
					{
						_textWriter.Write(", ");
					}
					break;
				case ContextType.Document:
				case ContextType.ScopeDocument:
					if (_context.HasElements)
					{
						_textWriter.Write(",");
					}

					_textWriter.Write(" ");

					WriteQuotedString(name);
					_textWriter.Write(" : ");
					break;
				case ContextType.TopLevel:
					break;
				default:
					throw new BsonInternalException("Invalid ContextType.");
			}

			_context.HasElements = true;
		}

		private void WriteQuotedString(string value)
		{
			_textWriter.Write("\"");
			_textWriter.Write(EscapedString(value));
			_textWriter.Write("\"");
		}

		internal class JsonWriterContext
		{
			// private fields

			// constructors
			internal JsonWriterContext(JsonWriterContext parentContext, ContextType contextType)
			{
				ParentContext = parentContext;
				ContextType = contextType;
			}

			// internal properties
			internal JsonWriterContext ParentContext { get; }

			internal ContextType ContextType { get; }

			internal bool HasElements { get; set; } = false;
		}

		public static string AsJson(BsonDocument obj,
			IBsonSerializer serializer = null,
			Action<BsonSerializationContext.Builder> configurator = null)
		{
			var nominalType = typeof(BsonDocument);
			var args = new BsonSerializationArgs(nominalType, false, false);

			serializer ??= BsonSerializer.LookupSerializer(nominalType);

			using var stringWriter = new StringWriter();
			using var jsonWriter = new ShellJsonWriter(stringWriter);

			var root = BsonSerializationContext.CreateRoot(jsonWriter, configurator);
			serializer.Serialize(root, args, obj);
			return stringWriter.ToString();
		}
	}
}
