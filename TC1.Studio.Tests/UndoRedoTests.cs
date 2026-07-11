using TC1.Core.BinaryObject;
using TC1.Studio.Services;
using FluentAssertions;

namespace TC1.Studio.Tests;

public class UndoRedoTests
{
    private static (CommandStack, Node) CreateStack()
    {
        var stack = new CommandStack();
        var node = new Node { NameHash = 1 };
        node.Fields[0xAAAAAAAA] = BitConverter.GetBytes(1.0f);
        return (stack, node);
    }

    [Fact]
    public void Initial_state_cannot_undo_redo()
    {
        var (stack, _) = CreateStack();
        stack.CanUndo.Should().BeFalse();
        stack.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Execute_command_enables_undo()
    {
        var (stack, node) = CreateStack();
        var cmd = new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test");

        stack.Execute(cmd);

        stack.CanUndo.Should().BeTrue();
        stack.CanRedo.Should().BeFalse();
        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().BeApproximately(2.0f, 0.001f);
    }

    [Fact]
    public void Undo_restores_old_value()
    {
        var (stack, node) = CreateStack();
        var cmd = new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test");
        stack.Execute(cmd);

        stack.Undo();

        stack.CanUndo.Should().BeFalse();
        stack.CanRedo.Should().BeTrue();
        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void Redo_restores_new_value()
    {
        var (stack, node) = CreateStack();
        var cmd = new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test");
        stack.Execute(cmd);
        stack.Undo();

        stack.Redo();

        stack.CanUndo.Should().BeTrue();
        stack.CanRedo.Should().BeFalse();
        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().BeApproximately(2.0f, 0.001f);
    }

    [Fact]
    public void New_command_after_undo_clears_redo()
    {
        var (stack, node) = CreateStack();
        stack.Execute(new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test"));
        stack.Undo();

        stack.Execute(new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(3.0f), "Test"));

        stack.CanRedo.Should().BeFalse();
        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().BeApproximately(3.0f, 0.001f);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var (stack, node) = CreateStack();
        stack.Execute(new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test"));

        stack.Clear();

        stack.CanUndo.Should().BeFalse();
        stack.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Hundred_operations_undo_redo_all()
    {
        var (stack, node) = CreateStack();
        var file = new BinaryObjectFile { Root = node };

        for (int i = 0; i < 100; i++)
        {
            stack.Execute(new SetFieldCommand(file, node, 0xAAAAAAAA,
                BitConverter.GetBytes((float)i), BitConverter.GetBytes((float)(i + 1)),
                $"Step {i}"));
        }

        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().Be(100.0f);

        for (int i = 0; i < 100; i++)
            stack.Undo();

        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().Be(0.0f);

        for (int i = 0; i < 100; i++)
            stack.Redo();

        BitConverter.ToSingle(node.Fields[0xAAAAAAAA]).Should().Be(100.0f);
    }

    [Fact]
    public void Hundred_operations_different_fields()
    {
        var stack = new CommandStack();
        var node = new Node { NameHash = 1 };
        var file = new BinaryObjectFile { Root = node };

        for (uint i = 0; i < 100; i++)
        {
            node.Fields[i] = BitConverter.GetBytes(0.0f);
            stack.Execute(new SetFieldCommand(file, node, i,
                BitConverter.GetBytes(0.0f), BitConverter.GetBytes((float)i),
                $"Field {i}"));
        }

        for (uint i = 0; i < 100; i++)
            BitConverter.ToSingle(node.Fields[i]).Should().Be((float)i);

        for (int i = 99; i >= 0; i--)
            stack.Undo();

        for (uint i = 0; i < 100; i++)
            BitConverter.ToSingle(node.Fields[i]).Should().Be(0.0f);
    }

    [Fact]
    public void Undo_on_empty_stack_does_nothing()
    {
        var stack = new CommandStack();
        stack.Undo();
        stack.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Redo_on_empty_stack_does_nothing()
    {
        var stack = new CommandStack();
        stack.Redo();
        stack.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Changed_event_fires()
    {
        var (stack, node) = CreateStack();
        var fired = false;
        stack.Changed += () => fired = true;

        stack.Execute(new SetFieldCommand(new BinaryObjectFile { Root = node }, node, 0xAAAAAAAA,
            BitConverter.GetBytes(1.0f), BitConverter.GetBytes(2.0f), "Test"));

        fired.Should().BeTrue();
    }
}
