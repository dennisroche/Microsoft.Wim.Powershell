using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using Microsoft.Wim.Powershell.Tests;
using NUnit.Framework;

namespace Microsoft.Wim.Powershell.Tests
{
    [TestFixture]
    public class WriteWimImageTests
    {
        [Test]
        public void ShouldCreateCmdLet()
        {
            var cmd = new WriteWimImage();
            Assert.IsInstanceOf<Cmdlet>(cmd);
        }

        [Test]
        public void WriteImageTest()
        {
            var cmd = new WriteWimImage
            {
                WimPath = "Test.wim",
                TargetPath = @"C:\Test\"
            };

            var result = cmd.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
        }

    }
}
