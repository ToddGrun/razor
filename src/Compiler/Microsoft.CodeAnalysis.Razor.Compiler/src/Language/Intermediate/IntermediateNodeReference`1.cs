// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly struct IntermediateNodeReference<T>
    where T : IntermediateNode
{
    public T Node { get; }
    public IntermediateNode Parent { get; }

    public IntermediateNodeReference(T node, IntermediateNode parent)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(parent);

        Node = node;
        Parent = parent;
    }

    public void Deconstruct(out T node, out IntermediateNode parent)
    {
        node = Node;
        parent = Parent;
    }

    private int GetNodeIndexForMutation()
    {
        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index < 0)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(Node, Parent));
        }

        return index;
    }

    public IntermediateNodeReference<TNode> InsertAfter<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children.Insert(index + 1, node);
        return new(node, Parent);
    }

    public void InsertAfter<TNode>(IEnumerable<TNode> nodes)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndexForMutation();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(++index, node);
        }
    }

    public IntermediateNodeReference<TNode> InsertBefore<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children.Insert(index, node);
        return new(node, Parent);
    }

    public void InsertBefore<TNode>(IEnumerable<TNode> nodes)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndexForMutation();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(index++, node);
        }
    }

    public void Remove()
    {
        var index = GetNodeIndexForMutation();

        Parent.Children.RemoveAt(index);
    }

    public IntermediateNodeReference<TNode> Replace<TNode>(TNode node)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children[index] = node;
        return new(node, Parent);
    }

    private string GetDebuggerDisplay()
        => $"ref: {Parent.GetDebuggerDisplay()} - {Node.GetDebuggerDisplay()}";
}
