using System.Collections.Generic;
using TC1.Core.BinaryObject;

namespace TC1.Studio.Services;

public interface IUndoableCommand
{
    string Name { get; }
    void Execute();
    void Undo();
}

public class CommandStack
{
    private readonly LinkedList<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxUndo = 100;

    public event Action Changed;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string UndoName => _undoStack.Count > 0 ? _undoStack.Last.Value.Name : "";
    public string RedoName => _redoStack.Count > 0 ? _redoStack.Peek().Name : "";

    public void Execute(IUndoableCommand cmd)
    {
        cmd.Execute();
        _undoStack.AddLast(cmd);
        _redoStack.Clear();
        if (_undoStack.Count > MaxUndo)
            _undoStack.RemoveFirst();
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack.Last.Value;
        _undoStack.RemoveLast();
        cmd.Undo();
        _redoStack.Push(cmd);
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.AddLast(cmd);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        Changed?.Invoke();
    }
}

public class SetFieldCommand : IUndoableCommand
{
    private readonly BinaryObjectFile _file;
    private readonly Node _node;
    private readonly uint _fieldHash;
    private readonly byte[] _oldValue;
    private readonly byte[] _newValue;

    public string Name { get; }

    public SetFieldCommand(BinaryObjectFile file, Node node, uint fieldHash, byte[] oldValue, byte[] newValue, string fieldName)
    {
        _file = file;
        _node = node;
        _fieldHash = fieldHash;
        _oldValue = oldValue;
        _newValue = newValue;
        Name = $"Set {fieldName}";
    }

    public void Execute()
    {
        _node.Fields[_fieldHash] = _newValue;
    }

    public void Undo()
    {
        _node.Fields[_fieldHash] = _oldValue;
    }
}

public class AddFieldCommand : IUndoableCommand
{
    private readonly Node _node;
    private readonly uint _fieldHash;
    private readonly byte[] _value;

    public string Name { get; }

    public AddFieldCommand(Node node, uint fieldHash, byte[] value, string fieldName)
    {
        _node = node;
        _fieldHash = fieldHash;
        _value = value;
        Name = $"Add {fieldName}";
    }

    public void Execute()
    {
        _node.Fields[_fieldHash] = _value;
    }

    public void Undo()
    {
        _node.Fields.Remove(_fieldHash);
    }
}

public class RemoveFieldCommand : IUndoableCommand
{
    private readonly Node _node;
    private readonly uint _fieldHash;
    private readonly byte[] _oldValue;

    public string Name { get; }

    public RemoveFieldCommand(Node node, uint fieldHash, byte[] oldValue, string fieldName)
    {
        _node = node;
        _fieldHash = fieldHash;
        _oldValue = oldValue;
        Name = $"Remove {fieldName}";
    }

    public void Execute()
    {
        _node.Fields.Remove(_fieldHash);
    }

    public void Undo()
    {
        _node.Fields[_fieldHash] = _oldValue;
    }
}
