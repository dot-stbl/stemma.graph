// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Abstractions.State;

/// <summary>
///     Marks a partial state class for source generation of channel schema, Update, and ToWrites.
///     Generation is optional — fluent channel APIs work without this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GraphStateAttribute : Attribute;
