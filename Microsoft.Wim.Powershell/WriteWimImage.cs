using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.Wim.Powershell
{
    [Cmdlet(VerbsCommunications.Write, "WimImage")]
    public class WriteWimImage : Cmdlet
    {
        private bool _abortWrite;

        [Parameter(Mandatory = true)]
        [ValidatePattern(@"\.wim$")]
        public string WimPath { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TargetPath { get; set; }

        [Parameter]
        public string Edition { get; set; }

        private WimMessageResult MessageCallback(WimMessageType messageType, object message, object userData)
        {
            if (messageType == WimMessageType.Process)
            {
                if (_abortWrite)
                    return WimMessageResult.Abort;
            }
            else if (messageType == WimMessageType.Progress)
            {
                var progressMessage = ((WimMessageProgress)message);

                var progressRecord = (ProgressRecord)userData;
                progressRecord.PercentComplete = progressMessage.PercentComplete;
                progressRecord.SecondsRemaining = (int)progressMessage.EstimatedTimeRemaining.TotalSeconds;

                WriteProgress(progressRecord);
            }
            else if (messageType == WimMessageType.Warning)
            {
                WriteWarning(((WimMessageWarning)message).Path);
            }
            else if (messageType == WimMessageType.Error)
            {
                var errorMessage = (WimMessageError)message;
                var errorRecord = new ErrorRecord(new Win32Exception(errorMessage.Win32ErrorCode), errorMessage.Path,
                    ErrorCategory.WriteError, null);

                WriteError(errorRecord);
            }

            return WimMessageResult.Success;
        }

        protected override void ProcessRecord()
        {
            using (var wimHandle = WimgApi.CreateFile(WimPath,
                WimFileAccess.Read,
                WimCreationDisposition.OpenExisting,
                WimCreateFileOptions.None,
                WimCompressionType.None))
            {
                WimgApi.SetTemporaryPath(wimHandle, Path.GetTempPath());

                var loadImageIndex = 1;
                var imageInformation = WimgApi.GetImageInformation(wimHandle).CreateNavigator();
                if (imageInformation == null)
                    throw new NullReferenceException("Unable to get Image Information");

                var imageCount = WimgApi.GetImageCount(wimHandle);
                if (imageCount > 1 && !string.IsNullOrWhiteSpace(Edition))
                {
                    loadImageIndex = 1;
                }

                var imageName = imageInformation.SelectSingleNode("IMAGE[@index='" + loadImageIndex + "']");

                var progressRecord = new ProgressRecord(0, string.Format("Applying Windows Image - {0}", imageName), "Starting");
                WimgApi.RegisterMessageCallback(wimHandle, MessageCallback, progressRecord);

                try
                {
                    using (var imageHandle = WimgApi.LoadImage(wimHandle, loadImageIndex))
                        WimgApi.ApplyImage(imageHandle, TargetPath, WimApplyImageOptions.None);
                }
                finally
                {
                    WimgApi.UnregisterMessageCallback(wimHandle, MessageCallback);
                }

            }
        }

        protected override void StopProcessing()
        {
            base.StopProcessing();
            _abortWrite = true;

            WriteWarning("Aborting Apply Image");
        }

    }
}
