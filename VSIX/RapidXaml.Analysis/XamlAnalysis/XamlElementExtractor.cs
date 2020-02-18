﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Language.Xml;
using Microsoft.VisualStudio.Text;
using RapidXaml;
using RapidXamlToolkit.XamlAnalysis.Processors;

namespace RapidXamlToolkit.XamlAnalysis
{
    public static class XamlElementExtractor
    {
        public static bool Parse(ProjectType projectType, string fileName, ITextSnapshot snapshot, string xaml, List<(string element, XamlElementProcessor processor)> processors, TagList tags, List<TagSuppression> suppressions = null)
        {
            var elementsOfInterest = processors.Select(p => p.element).ToList();

            var elementsBeingTracked = new List<TrackingElement>();

            var everyElementProcessor = new EveryElementProcessor(projectType, SharedRapidXamlPackage.Logger);

            bool isIdentifyingElement = false;
            bool isClosingElement = false;
            bool inLineOpeningWhitespace = true;
            bool inComment = false;
            int currentElementStartPos = -1;

            var lastElementName = string.Empty;
            var currentElementName = new StringBuilder();
            var currentElementBody = new StringBuilder();
            var closingElementName = new StringBuilder();
            var lineIndent = new StringBuilder();

            for (int i = 0; i < xaml.Length; i++)
            {
                currentElementBody.Append(xaml[i]);

                for (var j = 0; j < elementsBeingTracked.Count; j++)
                {
                    elementsBeingTracked[j].ElementBody.Append(xaml[i]);
                }

                if (xaml[i] == '<')
                {
                    if (!inComment)
                    {
                        isIdentifyingElement = true;
                        inLineOpeningWhitespace = false;
                        currentElementStartPos = i;
                        lastElementName = currentElementName.ToString();
                        currentElementName.Clear();
                        currentElementBody = new StringBuilder("<");
                    }
                }
                else if (char.IsLetterOrDigit(xaml[i]) || xaml[i] == ':' || xaml[i] == '_')
                {
                    if (!inComment)
                    {
                        if (isIdentifyingElement)
                        {
                            currentElementName.Append(xaml[i]);
                        }
                        else if (isClosingElement)
                        {
                            closingElementName.Append(xaml[i]);
                        }
                    }

                    inLineOpeningWhitespace = false;
                }
                else if (xaml[i] == '\r' || xaml[i] == '\n')
                {
                    if (!isIdentifyingElement)
                    {
                        lineIndent.Clear();
                        inLineOpeningWhitespace = true;
                    }
                }
                else if (char.IsWhiteSpace(xaml[i]))
                {
                    if (isIdentifyingElement)
                    {
                        if (elementsOfInterest.Contains(currentElementName.ToString())
                         || elementsOfInterest.Contains(currentElementName.ToString().PartAfter(":")))
                        {
                            elementsBeingTracked.Add(
                                new TrackingElement
                                {
                                    StartPos = currentElementStartPos,
                                    ElementName = currentElementName.ToString(),
                                    ElementBody = new StringBuilder(currentElementBody.ToString()),
                                });
                        }
                    }

                    if (inLineOpeningWhitespace)
                    {
                        lineIndent.Append(xaml[i]);
                    }

                    isIdentifyingElement = false;
                }
                else if (xaml[i] == '/')
                {
                    isClosingElement = true;
                    closingElementName.Clear();
                    isIdentifyingElement = false;
                    inLineOpeningWhitespace = false;
                }
                else if (xaml[i] == '>')
                {
                    if (i > 2 && xaml.Substring(i - 2, 3) == "-->")
                    {
                        inComment = false;
                    }
                    else
                    {
                        inLineOpeningWhitespace = false;

                        if (isIdentifyingElement)
                        {
                            if (elementsOfInterest.Contains(currentElementName.ToString())
                             || elementsOfInterest.Contains(currentElementName.ToString().PartAfter(":")))
                            {
                                elementsBeingTracked.Add(
                                    new TrackingElement
                                    {
                                        StartPos = currentElementStartPos,
                                        ElementName = currentElementName.ToString(),
                                        ElementBody = new StringBuilder(currentElementBody.ToString()),
                                    });
                            }

                            isIdentifyingElement = false;
                        }

                        // closing blocks can be blank or named (e.g. ' />' or '</Grid>')
                        if (isClosingElement)
                        {
                            var nameOfInterest = closingElementName.ToString();

                            if (string.IsNullOrWhiteSpace(nameOfInterest))
                            {
                                nameOfInterest = currentElementName.ToString();
                            }
                            else if (nameOfInterest == lastElementName)
                            {
                                nameOfInterest = lastElementName;
                            }

                            var toProcess = elementsBeingTracked.Where(g => g.ElementName == nameOfInterest)
                                                                .OrderByDescending(f => f.StartPos)
                                                                .Select(e => e)
                                                                .FirstOrDefault();

                            if (!string.IsNullOrWhiteSpace(toProcess.ElementName))
                            {
                                var elementBody = toProcess.ElementBody.ToString();

                                // Do this here with values already calculated
                                everyElementProcessor.Process(fileName, toProcess.StartPos, elementBody, lineIndent.ToString(), snapshot, tags, suppressions);

                                foreach (var (element, processor) in processors)
                                {
                                    if (element == toProcess.ElementName
                                     || element == toProcess.ElementNameWithoutNamespace)
                                    {
                                        processor.Process(fileName, toProcess.StartPos, elementBody, lineIndent.ToString(), snapshot, tags, suppressions);
                                    }
                                }

                                elementsBeingTracked.Remove(toProcess);
                            }
                            else
                            {
                                if (!inComment)
                                {
                                    // Do this in the else so don't always have to calculate the substring.
                                    everyElementProcessor.Process(fileName, currentElementStartPos, xaml.Substring(currentElementStartPos, i - currentElementStartPos + 1), lineIndent.ToString(), snapshot, tags, suppressions);
                                }
                            }

                            // Reset this so know what we should be tracking
                            currentElementStartPos = -1;
                            isClosingElement = false;
                        }
                    }
                }
                else if (xaml[i] == '-')
                {
                    if (i >= 3 && xaml.Substring(i - 3, 4) == "<!--")
                    {
                        inComment = true;
                    }
                }
            }

            return true;
        }

        public static RapidXamlElement GetElement(string xamlElement)
        {
            RapidXamlElement GetElement(XmlDocumentSyntax docSyntax, string elementContent, string docString)
            {
                var xdoc = docSyntax?.RootSyntax;

                if (xdoc == null)
                {
                    return null;
                }

                var elementName = xdoc.Name;

                var result = RapidXamlElement.Build(elementName);

                var content = elementContent;

                foreach (var attr in xdoc.Attributes)
                {
                    result.AddAttribute(attr.Name, attr.Value);
                }

                foreach (var child in docSyntax.Body.ChildNodes)
                {
                    if (child == null | child is XmlElementStartTagSyntax | child is XmlElementEndTagSyntax)
                    {
                        continue;
                    }

                    // Self is counted as a child so skip that (based on start pos)
                    if (child is XmlElementSyntax childElement)
                    {
                        if (childElement.Name.StartsWith($"{elementName}."))
                        {
                            result.AddAttribute(
                                childElement.Name.Substring(elementName.Length + 1),
                                docString.Substring(childElement.Content.FullSpan.Start, childElement.Content.FullSpan.Length));

                            var childAsString = docString.Substring(childElement.Start, childElement.Width);

                            if (content.StartsWith(childAsString))
                            {
                                content = content.Substring(childAsString.Length);
                            }
                        }
                        else
                        {
                            var childContentSpan = childElement.Content.FullSpan;
                            var childContent = docString.Substring(childContentSpan.Start, childContentSpan.Length);

                            var childDoc = Parser.ParseText(elementContent);

                            result.AddChild(GetElement(childDoc, childContent, elementContent));
                        }
                    }
                    else if (child is XmlEmptyElementSyntax selfClosingChild)
                    {
                        var toAdd = RapidXamlElement.Build(selfClosingChild.Name);

                        foreach (var attr in selfClosingChild.AttributesNode)
                        {
                            toAdd.AddAttribute(attr.Name, attr.Value);
                        }

                        result.AddChild(toAdd);
                    }
                    else if (child is SyntaxNode node)
                    {
                        foreach (var nodeChild in node.ChildNodes)
                        {
                            if (nodeChild is XmlTextTokenSyntax)
                            {
                                continue;
                            }

                            if (nodeChild is XmlElementSyntax ncElement)
                            {
                                if (ncElement.Name.StartsWith($"{elementName}."))
                                {
                                    result.AddAttribute(
                                        ncElement.Name.Substring(elementName.Length + 1),
                                        docString.Substring(ncElement.Content.FullSpan.Start, ncElement.Content.FullSpan.Length));

                                    var childAsString = docString.Substring(ncElement.Start, ncElement.Width);

                                    if (content.TrimStart().StartsWith(childAsString.TrimStart()))
                                    {
                                        content = content.TrimStart().Substring(childAsString.Length).Trim();
                                    }
                                }
                                else
                                {
                                    var childContentSpan = ncElement.Content.FullSpan;
                                    var childContent = docString.Substring(childContentSpan.Start, childContentSpan.Length);

                                    var childString = docString.Substring(ncElement.Start, ncElement.Width);

                                    var childDoc = Parser.ParseText(childString);

                                    result.AddChild(GetElement(childDoc, childContent, docString.Substring(nodeChild.Start, nodeChild.Width)));
                                }
                            }
                            else if (nodeChild is XmlEmptyElementSyntax ncSelfClosing)
                            {
                                var nodeToAdd = RapidXamlElement.Build(ncSelfClosing.Name);

                                foreach (var attr in ncSelfClosing.AttributesNode)
                                {
                                    nodeToAdd.AddAttribute(attr.Name, attr.Value);
                                }

                                result.AddChild(nodeToAdd);
                            }
                        }
                    }
                }

                result.SetContent(content);

                return result;
            }

            //// TODO: Cache these responses to avoid repeated parsing
            var documentSyntax = Parser.ParseText(xamlElement);

            var contentSpan = documentSyntax.RootSyntax.Content.FullSpan;

            var innerContent = xamlElement.Substring(contentSpan.Start, contentSpan.Length);

            return GetElement(documentSyntax, innerContent, xamlElement);
        }

        private struct TrackingElement
        {
            public int StartPos { get; set; }

            public string ElementName { get; set; }

            public string ElementNameWithoutNamespace
            {
                get
                {
                    return this.ElementName.PartAfter(":");
                }
            }

            public StringBuilder ElementBody { get; set; }
        }
    }
}
