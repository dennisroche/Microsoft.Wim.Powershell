using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Wim.Powershell
{
    [Cmdlet(VerbsCommunications.Write, "WimImage")]
    public class WriteWimImage : Cmdlet
    {
        private bool _abortWrite;
        private ProgressRecord _progressRecord;

        private AsyncOperation _asyncOp;
        private AutoResetEvent _autoResetEvent;

        [Parameter(Mandatory = true)]
        [ValidatePattern(@"\.wim$")]
        public string WimPath { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TargetPath { get; set; }

        [Parameter]
        public string Edition { get; set; }

        protected override void BeginProcessing()
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            _asyncOp = AsyncOperationManager.CreateOperation(null);
            _autoResetEvent = new AutoResetEvent(false);
        }

        protected override void ProcessRecord()
        {
            var task = Task.Factory.StartNew(ApplyImage);

            do
            {
                Application.DoEvents();
            }
            while (!_autoResetEvent.WaitOne(250));

            Application.DoEvents();

            task.Wait();
        }

        private void ApplyImage()
        {
            using (var wimHandle = WimgApi.CreateFile(WimPath, WimFileAccess.Read, WimCreationDisposition.OpenExisting,
                    WimCreateFileOptions.None, WimCompressionType.None))
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

                var xpath = string.Format("//WIM/IMAGE[@INDEX={0}]/NAME", loadImageIndex);
                var imageNameNode = imageInformation.SelectSingleNode(xpath);
                if (imageNameNode == null)
                    throw new InvalidDataException("Unable to read WIM image name");

                _progressRecord = new ProgressRecord(0, string.Format("Applying Windows Image - {0}", imageNameNode.TypedValue), "Starting");

                WimgApi.RegisterMessageCallback(wimHandle, MessageCallback);

                try
                {
                    using (var imageHandle = WimgApi.LoadImage(wimHandle, loadImageIndex))
                        WimgApi.ApplyImage(imageHandle, TargetPath, WimApplyImageOptions.None);
                }
                finally
                {
                    _autoResetEvent.Set();
                    WimgApi.UnregisterMessageCallback(wimHandle, MessageCallback);
                }
            }
        }

        protected override void StopProcessing()
        {
            WriteDebug(string.Format("StopProcessing ThreadId: {0} - {1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name));

            _abortWrite = true;
            WriteWarning("Aborting Apply Image");
        }

        private WimMessageResult MessageCallback(WimMessageType messageType, object message, object userData)
        {
            if (messageType == WimMessageType.Process && _abortWrite)
                return WimMessageResult.Abort;

            switch (messageType)
            {
                case WimMessageType.Process:
                    _asyncOp.Post(WriteProcessAsync, message);
                    break;
                case WimMessageType.Progress:
                    _asyncOp.Post(WriteProgressAsync, message);
                    break;
                case WimMessageType.Warning:
                    _asyncOp.Post(WriteWarningAsync, message);
                    break;
                case WimMessageType.Error:
                    _asyncOp.Post(WriteErrorAsync, message);
                    break;
            }

            return WimMessageResult.Success;
        }

        private void WriteProgressAsync(object message)
        {
            var progressMessage = ((WimMessageProgress)message);
            _progressRecord.Activity = "Writing";
            _progressRecord.PercentComplete = progressMessage.PercentComplete;

            if (progressMessage.EstimatedTimeRemaining != TimeSpan.Zero)
                _progressRecord.SecondsRemaining = (int)progressMessage.EstimatedTimeRemaining.TotalSeconds;

            WriteProgress(_progressRecord);
        }

        private void WriteProcessAsync(object message)
        {
            _progressRecord.StatusDescription = ((WimMessageProcess)message).Path;
            WriteProgress(_progressRecord);
        }

        private void WriteWarningAsync(object message)
        {
            WriteWarning(((WimMessageWarning)message).Path);
        }

        private void WriteErrorAsync(object message)
        {
            var errorMessage = (WimMessageError)message;
            var errorRecord = new ErrorRecord(new Win32Exception(errorMessage.Win32ErrorCode), errorMessage.Path,
                ErrorCategory.WriteError, null);

            WriteError(errorRecord);
        }

    }
}
