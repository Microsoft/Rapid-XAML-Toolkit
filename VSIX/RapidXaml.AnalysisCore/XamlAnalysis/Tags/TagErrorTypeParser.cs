﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using RapidXamlToolkit.Logging;

namespace RapidXamlToolkit.XamlAnalysis.Tags
{
    public static class TagErrorTypeParser
    {
        public static bool TryParse(string value, ILogger logger, out TagErrorType tagErrorType)
        {
            try
            {
                // Could potentially happen with a corrupt/invalid settings file.
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Most tags are warnings so return that but caller shouldn't use as this returns false.
                    tagErrorType = TagErrorType.Warning;
                    return false;
                }

                switch (value.Trim().ToLowerInvariant())
                {
                    case "error":
                        tagErrorType = TagErrorType.Error;
                        break;
                    case "warning":
                        tagErrorType = TagErrorType.Warning;
                        break;
                    case "suggestion":
                        tagErrorType = TagErrorType.Suggestion;
                        break;
                    case "hidden":
                    case "none":
                        tagErrorType = TagErrorType.Hidden;
                        break;
                    default:
                        tagErrorType = TagErrorType.Hidden;
                        return false;
                }

                return true;
            }
            catch (System.Exception exc)
            {
                logger?.RecordException(exc);

                tagErrorType = TagErrorType.Hidden;
                return false;
            }
        }
    }
}
