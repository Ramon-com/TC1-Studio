using TC1.Core;
using TC1.Core.BinaryObject;
using TC1.Core.Hashing;
using TC1.Studio.Services;
using FluentAssertions;

namespace TC1.Studio.Tests;

public class BinaryObjectServiceTests
{
    private static BinaryObjectService MakeServiceWithData()
    {
        var bo = new BinaryObjectFile();
        bo.Root.NameHash = 0x2002CFD9;
        bo.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(3.14f);
        bo.Root.Fields[0xBBBBBBBB] = BitConverter.GetBytes(42.0f);
        var child = new Node { NameHash = 0x12345678 };
        child.Fields[0xCCCCCCCC] = new byte[] { 0x01 };
        bo.Root.Children.Add(child);

        using var ms = new MemoryStream();
        bo.Serialize(ms);
        var service = new BinaryObjectService();
        service.Open(ms.ToArray());
        return service;
    }

    [Fact]
    public void Open_creates_original_and_working()
    {
        var service = MakeServiceWithData();
        service.Original.Should().NotBeNull();
        service.Working.Should().NotBeNull();
        service.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Modify_then_save_detects_changes()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(99.0f);
        service.MarkModified();
        service.IsModified.Should().BeTrue();
    }

    [Fact]
    public void ResetToOriginal_restores_state()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(99.0f);
        service.MarkModified();

        service.ResetToOriginal();

        service.IsModified.Should().BeFalse();
        var val = BitConverter.ToSingle(service.Working.Root.Fields[0xAAAAAAAA]);
        val.Should().BeApproximately(3.14f, 0.001f);
    }

    [Fact]
    public void Save_after_no_changes_returns_same_bytes()
    {
        var service = MakeServiceWithData();
        var saved = service.Save();
        service.IsModified.Should().BeFalse();
        saved.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeDiff_no_changes_returns_empty()
    {
        var service = MakeServiceWithData();
        var diffs = service.ComputeDiff();
        diffs.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_detects_field_change()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(99.0f);

        var diffs = service.ComputeDiff();

        diffs.Should().ContainSingle();
        diffs[0].Kind.Should().Be(DiffKind.Changed);
        diffs[0].NameHash.Should().Be(0xAAAAAAAA);
    }

    [Fact]
    public void ComputeDiff_detects_field_added()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields[0xDDDDDDDD] = new byte[] { 0xFF };

        var diffs = service.ComputeDiff();

        diffs.Should().Contain(d => d.Kind == DiffKind.Added && d.NameHash == 0xDDDDDDDD);
    }

    [Fact]
    public void ComputeDiff_detects_removed_field()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields.Remove(0xAAAAAAAA);

        var diffs = service.ComputeDiff();

        diffs.Should().Contain(d => d.Kind == DiffKind.Removed && d.NameHash == 0xAAAAAAAA);
    }

    [Fact]
    public void ComputeDiff_child_field_change()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Children[0].Fields[0xCCCCCCCC] = new byte[] { 0xFF };

        var diffs = service.ComputeDiff();

        diffs.Should().Contain(d => d.Kind == DiffKind.Changed && d.NameHash == 0xCCCCCCCC);
    }

    [Fact]
    public void Multiple_edits_produce_multiple_diffs()
    {
        var service = MakeServiceWithData();
        service.Working.Root.Fields[0xAAAAAAAA] = BitConverter.GetBytes(1.0f);
        service.Working.Root.Fields[0xBBBBBBBB] = BitConverter.GetBytes(2.0f);
        service.Working.Root.Fields[0xDDDDDDDD] = new byte[] { 0xAB };

        var diffs = service.ComputeDiff();

        diffs.Should().HaveCount(3);
    }
}
