﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RapidXamlToolkit.DragDrop;
using RapidXamlToolkit.Utils.IO;
using RapidXamlToolkit.VisualStudioIntegration;

namespace RapidXamlToolkit.Tests.DragDrop
{
    [TestClass]
    public class DropHandlerLogicTests : DropHandlerTestsBase
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "logger")]
        public void CheckConstructorRequiredParam_Logger()
        {
            var fileContents = " // Just a comment";

            (IFileSystemAbstraction _, IVisualStudioAbstractionAndDocumentModelAccess vsa) = this.GetVbAbstractions(fileContents);

            var _ = new DropHandlerLogic(null, vsa);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "vs")]
        public void CheckConstructorRequiredParam_VS()
        {
            var _ = new DropHandlerLogic(DefaultTestLogger.Create(), null);
        }
    }
}
