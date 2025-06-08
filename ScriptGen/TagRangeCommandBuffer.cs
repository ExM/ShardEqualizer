using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.ScriptGen;

public class TagRangeCommandBuffer: IDisposable
{
	private readonly CommandPlanWriter _commandWriter;
	private readonly CollectionNamespace _collection;
	private readonly HashSet<Command> _addCommands = new ();
	private readonly HashSet<Command> _removeCommands = new ();

	public TagRangeCommandBuffer(CommandPlanWriter commandWriter, CollectionNamespace collection)
	{
		_commandWriter = commandWriter;
		_collection = collection;
	}
		
	public void AddTagRange(BsonBound min, BsonBound max, TagIdentity tag)
	{
		var cmd = new Command(min, max, tag);

		if (_removeCommands.Contains(cmd))
			_removeCommands.Remove(cmd);
		else
			_addCommands.Add(cmd);
	}
		
	public void RemoveTagRange(BsonBound min, BsonBound max, TagIdentity tag) 
	{
		var cmd = new Command(min, max, tag);
			
		if (_addCommands.Contains(cmd))
			_addCommands.Remove(cmd);
		else
			_removeCommands.Add(cmd);
	}
		
	public void Clear()
	{
		_removeCommands.Clear();
		_addCommands.Clear();
	}

	public void Flush()
	{
		foreach(var cmd in _removeCommands.OrderBy(c => c.Min))
			_commandWriter.RemoveTagRange(_collection, cmd.Min, cmd.Max, cmd.Tag);
			
		foreach(var cmd in _addCommands.OrderBy(c => c.Min))
			_commandWriter.AddTagRange(_collection, cmd.Min, cmd.Max, cmd.Tag);

		Clear();
	}
	
	public void Dispose()
	{
		Flush();
	}

	private class Command: IEquatable<Command>
	{
		public BsonBound Min { get; }
		public BsonBound Max { get; }
		public TagIdentity Tag { get; }

		public Command(BsonBound min, BsonBound max, TagIdentity tag)
		{
			Min = min;
			Max = max;
			Tag = tag;
		}

		public bool Equals(Command? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Min.Equals(other.Min) && Max.Equals(other.Max) && Tag.Equals(other.Tag);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Command) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Min.GetHashCode();
				hashCode = (hashCode * 397) ^ Max.GetHashCode();
				hashCode = (hashCode * 397) ^ Tag.GetHashCode();
				return hashCode;
			}
		}
	}
}