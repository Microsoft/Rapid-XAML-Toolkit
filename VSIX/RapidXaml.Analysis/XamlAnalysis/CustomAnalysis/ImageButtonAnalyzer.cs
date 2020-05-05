﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using RapidXaml;
using RapidXamlToolkit.Resources;

namespace RapidXamlToolkit.XamlAnalysis.CustomAnalysis
{
    public class ImageButtonAnalyzer : ICustomAnalyzer
    {
        public string TargetType() => "ImageButton";

        public AnalysisActions Analyze(RapidXamlElement element)
        {
            // TODO: ISSUE#137 add check for accessible property describing the image
            return AnalysisActions.None;
        }
    }
}