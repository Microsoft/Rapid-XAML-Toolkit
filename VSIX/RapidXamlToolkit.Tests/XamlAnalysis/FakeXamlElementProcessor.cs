﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using RapidXamlToolkit.VisualStudioIntegration;
using RapidXamlToolkit.XamlAnalysis;
using RapidXamlToolkit.XamlAnalysis.Processors;

namespace RapidXamlToolkit.Tests.XamlAnalysis
{
    // Used to record/track/assert the values passed to a processor during testing
    public class FakeXamlElementProcessor : XamlElementProcessor
    {
        public FakeXamlElementProcessor()
            : base(new ProcessorEssentialsForSimpleTests())
        {
        }

        public bool ProcessCalled { get; set; } = false;

        public int ProcessCalledCount { get; private set; } = 0;

        public string XamlElement { get; set; } = string.Empty;

        public int Offset { get; set; } = -1;

        public Dictionary<int, int> AllOffsets { get; private set; } = new Dictionary<int, int>();

        public Dictionary<int, string> AllXamlElements { get; private set; } = new Dictionary<int, string>();

        public override void Process(string fileName, int offset, string xamlElement, string linePadding, ITextSnapshotAbstraction snapshot, TagList tags, List<TagSuppression> suppressions = null, Dictionary<string, string> xlmns = null)
        {
            this.ProcessCalled = true;
            this.ProcessCalledCount += 1;

            this.Offset = offset;
            this.AllOffsets.Add(this.ProcessCalledCount, offset);

            this.XamlElement = xamlElement;
            this.AllXamlElements.Add(this.ProcessCalledCount, xamlElement);
        }
    }
}
