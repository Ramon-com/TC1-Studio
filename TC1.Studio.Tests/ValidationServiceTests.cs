using TC1.Core.BinaryObject;
using TC1.Studio.Services;
using FluentAssertions;

namespace TC1.Studio.Tests;

public class ValidationServiceTests
{
    [Fact]
    public void Null_root_returns_empty()
    {
        var svc = new ValidationService();
        var results = svc.Validate(null);
        results.Should().BeEmpty();
    }

    [Fact]
    public void Empty_file_no_warnings()
    {
        var bo = new BinaryObjectFile();
        var svc = new ValidationService();
        var results = svc.Validate(bo);
        results.Should().BeEmpty();
    }

    [Fact]
    public void NaN_float_triggers_warning()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = BitConverter.GetBytes(float.NaN);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().Contain(r =>
            r.Severity == ValidationSeverity.Warning &&
            r.Message.Contains("NaN"));
    }

    [Fact]
    public void Infinity_float_triggers_warning()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = BitConverter.GetBytes(float.PositiveInfinity);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().Contain(r => r.Message.Contains("Infinity"));
    }

    [Fact]
    public void Normal_float_no_warning()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = BitConverter.GetBytes(3.14f);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().BeEmpty();
    }

    [Fact]
    public void NaN_double_triggers_warning()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = BitConverter.GetBytes(double.NaN);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().Contain(r => r.Message.Contains("NaN"));
    }

    [Fact]
    public void Null_field_data_triggers_error()
    {
        var bo = new BinaryObjectFile();
        bo.Root.Fields[1] = null;

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().Contain(r =>
            r.Severity == ValidationSeverity.Error &&
            r.Message.Contains("Null"));
    }

    [Fact]
    public void Many_children_triggers_size_warning()
    {
        var bo = new BinaryObjectFile();
        for (int i = 0; i < 501; i++)
            bo.Root.Children.Add(new Node { NameHash = (uint)i });

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().Contain(r => r.Message.Contains("children") && r.Message.Contains("corruption"));
    }

    [Fact]
    public void Circular_reference_detected()
    {
        var bo = new BinaryObjectFile();
        var nodeA = new Node { NameHash = 1 };
        var nodeB = new Node { NameHash = 2 };
        nodeA.Children.Add(nodeB);
        nodeB.Children.Add(nodeA); // circular
        bo.Root.Children.Add(nodeA);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        // Should not stack overflow - should detect circular reference
        results.Should().Contain(r => r.Message.Contains("Circular"));
    }

    [Fact]
    public void Valid_file_no_warnings()
    {
        var bo = new BinaryObjectFile();
        bo.Root.NameHash = 0x2002CFD9;
        bo.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(1.0f);
        bo.Root.Fields[0xBBBBBBBB] = BitConverter.GetBytes(0.5f);
        var child = new Node { NameHash = 0x12345678 };
        child.Fields[0xCCCCCCCC] = new byte[] { 0x01 };
        bo.Root.Children.Add(child);

        var svc = new ValidationService();
        var results = svc.Validate(bo);

        results.Should().BeEmpty();
    }
}
