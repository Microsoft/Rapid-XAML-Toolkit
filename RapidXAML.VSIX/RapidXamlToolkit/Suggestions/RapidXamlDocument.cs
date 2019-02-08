﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Suggestions;

namespace RapidXamlToolkit.Tagging
{
    public class RapidXamlDocument
    {
        public RapidXamlDocument()
        {
            this.SuggestionTags = new List<IRapidXamlTag>();
        }

        public string RawText { get; set; }

        public List<IRapidXamlTag> SuggestionTags { get; set; }

        public static RapidXamlDocument Create(ITextSnapshot snapshot)
        {
            var result = new RapidXamlDocument();

            try
            {
                var text = snapshot.GetText();
                result.RawText = text;

                // TODO: offload the creation of tags to separate classes for handling each XAML element
                // Register handlers and the elements they are looking for

                // walk the file until find an element that's registered
                // Keep walking until get to the end of that element
                // pass the text of the element (and position offset) to the handler
                // handler processes the passed text and may add items to the SuggestionTags



                var count = 0;

                var rowDefIndex = text.IndexOf("<RowDefinition");
                while (rowDefIndex >= 0)
                {
                    var endPos = text.IndexOf('>', rowDefIndex);

                    var tag = new InsertRowDefinitionTag
                    {
                        Span = new Span(rowDefIndex, endPos - rowDefIndex),
                        RowId = count,
                    };

                    result.SuggestionTags.Add(tag);

                    count = count + 1;

                    rowDefIndex = text.IndexOf("<RowDefinition", endPos);
                }


                var tbIndex = text.IndexOf("<TextBlock Text=\"");

                if (tbIndex >= 0)
                {
                    var tbEnd = text.IndexOf(">", tbIndex);

                    var line = snapshot.GetLineFromPosition(tbIndex);
                    var col = tbEnd - line.Start.Position;

                    result.SuggestionTags.Add(new HardCodedStringTag
                    {
                        Span = new Span(tbIndex, tbEnd - tbIndex),
                        Line = line.LineNumber,
                        Column = col,
                        Snapshot = snapshot,
                    });
                }
            }
            catch (Exception e)
            {
                result.SuggestionTags.Add(new UnexpectedErrorTag
                {
                    Span = new Span(0, 0),
                    Message = "Unexpected error occurred while parsing XAML. Please log an issue to https://github.com/Microsoft/Rapid-XAML-Toolkit/issues Reason: " + e,
                });
            }

            return result;
        }
    }

    public static class XamlElementExtractor
    {
        public static void Parse(string xaml, List<(string element, XamlElementProcessor procesor)> processors, List<IRapidXamlTag> tags)
        {
            var elements = processors.Select(p => p.element).ToList();

            // walk the file until find an element that's registered
            // Keep walking until get to the end of that element
            // pass the text of the element (and position offset) to the handler
            // handler processes the passed text and may add items to the SuggestionTags

            foreach (var processor in processors)
            {
                if (processor.element == foundElementName)
                {
                    processor.procesor.Process(foundElementStartPos, foundElement, tags);
                }
            }
        }
    }

    public abstract class XamlElementProcessor
    {
        public abstract void Process(int offset, string xamlElement, List<IRapidXamlTag> tags);
    }
}
