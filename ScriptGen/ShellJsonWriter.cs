using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ShardEqualizer.ScriptGen;

public class ShellJsonWriter : BsonWriter
{
	public ShellJsonWriter(TextWriter writer) : base(new StubSettings())
	{
		_textWriter = writer ?? throw new ArgumentNullException(nameof(writer));
		_context = new JsonWriterContext(null, ContextType.TopLevel);
		State = BsonWriterState.Initial;
	}

	private readonly TextWriter _textWriter;
	private JsonWriterContext? _context;

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
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		
		_textWriter.Flush();
	}

	public override void WriteBinaryData(BsonBinaryData binaryData)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		var subType = binaryData.SubType;
		var bytes = binaryData.Bytes;

		var guidRepresentation = subType == BsonBinarySubType.UuidStandard
			? GuidRepresentation.Standard
			: GuidRepresentation.CSharpLegacy; //TODO to config

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

	public override void WriteBoolean(bool value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write(value ? "true" : "false");

		State = GetNextState();
	}

	public override void WriteBytes(byte[] bytes)
	{
		WriteBinaryData(new BsonBinaryData(bytes, BsonBinarySubType.Binary));
	}

	public override void WriteDateTime(long value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);

		// use ISODate for values that fall within .NET's DateTime range, and "new Date" for all others
		if (value >= BsonConstants.DateTimeMinValueMillisecondsSinceEpoch &&
		    value <= BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
		{
			var utcDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(value);
			_textWriter.Write("ISODate(\"{0:yyyy-MM-ddTHH:mm:ss.FFFZ}\")", utcDateTime);
		}
		else
		{
			_textWriter.Write("new Date({0})", value);
		}

		State = GetNextState();
	}

	public override void WriteDecimal128(Decimal128 value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);

		_textWriter.Write("NumberDecimal(\"{0}\")", value.ToString());

		State = GetNextState();
	}

	public override void WriteDouble(double value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		// if string representation looks like an integer add ".0" so that it looks like a double
		var stringRepresentation = JsonConvert.ToString(value);
		if (Regex.IsMatch(stringRepresentation, @"^[+-]?\d+$"))
		{
			stringRepresentation += ".0";
		}

		WriteNameHelper(Name);

		_textWriter.Write(stringRepresentation);

		State = GetNextState();
	}

	public override void WriteEndArray()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value]);

		base.WriteEndArray();
		_textWriter.Write("]");

		_context = _context!.ParentContext;
		State = GetNextState();
	}

	public override void WriteEndDocument()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Name]);

		base.WriteEndDocument();
		_textWriter.Write(" }");

		if (_context!.ContextType == ContextType.ScopeDocument)
		{
			_context = _context.ParentContext;
			WriteEndDocument();
		}
		else
		{
			_context = _context.ParentContext;
		}

		State = GetNextState();
	}

	public override void WriteInt32(int value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);

		_textWriter.Write("NumberInt({0})", value);

		State = GetNextState();
	}

	public override void WriteInt64(long value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);

		_textWriter.Write("NumberLong(\"{0}\")", value);

		State = GetNextState();
	}

	public override void WriteJavaScript(string code)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("{{ \"$code\" : \"{0}\" }}", EscapedString(code));

		State = GetNextState();
	}

	public override void WriteJavaScriptWithScope(string code)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteStartDocument();
		WriteName("$code");
		WriteString(code);
		WriteName("$scope");

		State = BsonWriterState.ScopeDocument;
	}

	public override void WriteMaxKey()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("MaxKey");

		State = GetNextState();
	}

	public override void WriteMinKey()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("MinKey");

		State = GetNextState();
	}

	public override void WriteNull()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("null");

		State = GetNextState();
	}

	public override void WriteObjectId(ObjectId objectId)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("ObjectId(\"{0}\")", objectId.ToString());

		State = GetNextState();
	}

	public override void WriteRegularExpression(BsonRegularExpression regex)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		var pattern = regex.Pattern;
		var options = regex.Options;

		WriteNameHelper(Name);
		var escapedPattern = (pattern == "") ? "(?:)" : pattern.Replace("/", @"\/");
		_textWriter.Write("/{0}/{1}", escapedPattern, options);

		State = GetNextState();
	}

	public override void WriteStartArray()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		base.WriteStartArray();
		WriteNameHelper(Name);
		_textWriter.Write("[");

		_context = new JsonWriterContext(_context, ContextType.Array);
		State = BsonWriterState.Value;
	}

	public override void WriteStartDocument()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial, BsonWriterState.ScopeDocument]);

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

	public override void WriteString(string value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		WriteQuotedString(value);

		State = GetNextState();
	}

	public override void WriteSymbol(string value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("{{ \"$symbol\" : \"{0}\" }}", EscapedString(value));

		State = GetNextState();
	}

	public override void WriteTimestamp(long value)
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		var secondsSinceEpoch = (uint)((value >> 32) & 0xffffffff);
		var increment = (uint)(value & 0xffffffff);

		WriteNameHelper(Name);
		_textWriter.Write("Timestamp({0}, {1})", secondsSinceEpoch, increment);

		State = GetNextState();
	}

	public override void WriteUndefined()
	{
		ObjectDisposedException.ThrowIf(Disposed, typeof(ShellJsonWriter));
		AssertStates([BsonWriterState.Value, BsonWriterState.Initial]);

		WriteNameHelper(Name);
		_textWriter.Write("undefined");

		State = GetNextState();
	}

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
		if (_context == null)
			return BsonWriterState.Done;
		
		return _context.ContextType is ContextType.Array or ContextType.TopLevel 
			? BsonWriterState.Value 
			: BsonWriterState.Name;
	}

	private string GuidToString(BsonBinarySubType subType, byte[] bytes, GuidRepresentation guidRepresentation)
	{
		if (bytes.Length != 16)
			throw new ArgumentException($"Length of binary subtype {subType} must be 16, not {bytes.Length}.");

		if (subType == BsonBinarySubType.UuidLegacy && guidRepresentation == GuidRepresentation.Standard)
			throw new ArgumentException("GuidRepresentation for binary subtype UuidLegacy must not be Standard.");

		if (subType == BsonBinarySubType.UuidStandard && guidRepresentation != GuidRepresentation.Standard)
			throw new ArgumentException($"GuidRepresentation for binary subtype UuidStandard must be Standard, not {guidRepresentation}.");

		if (guidRepresentation == GuidRepresentation.Unspecified)
		{
			var s = BsonUtils.ToHexString(bytes);
			var parts = new[]
			{
				s.Substring(0, 8),
				s.Substring(8, 4),
				s.Substring(12, 4),
				s.Substring(16, 4),
				s.Substring(20, 12)
			};
			return $"HexData({(int)subType}, \"{string.Join("-", parts)}\")";
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
			return $"{uuidConstructorName}(\"{guid.ToString()}\")";
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
		switch (_context!.ContextType)
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
	
	private void AssertStates(BsonWriterState[] states, [CallerMemberName] string memberName = "")
	{
		if (states.All(state => State != state))
			ThrowInvalidState(memberName, states);
	}

	internal class JsonWriterContext
	{
		internal JsonWriterContext(JsonWriterContext? parentContext, ContextType contextType)
		{
			ParentContext = parentContext;
			ContextType = contextType;
		}

		internal JsonWriterContext? ParentContext { get; }

		internal ContextType ContextType { get; }

		internal bool HasElements { get; set; } = false;
	}

	public static string AsJson(BsonDocument obj,
		IBsonSerializer? serializer = null,
		Action<BsonSerializationContext.Builder>? configurator = null)
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

	private class StubSettings : BsonWriterSettings
	{
		protected override BsonWriterSettings CloneImplementation() => new StubSettings();
	}
}