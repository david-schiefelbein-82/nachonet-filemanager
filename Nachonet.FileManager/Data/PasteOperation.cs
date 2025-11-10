using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public class PasteOperation
    {
        private readonly SessionClipboard _clipboard;
        private readonly Task _task;
        private readonly string _destinationFolder;
        private readonly bool _overwrite;
        private readonly ClipboardManager _clipboardManager;

        private readonly string _title;
        private string _label;
        private readonly List<ClipboardResult> _results;
        private bool _finished;
        private Int64 _startTime;
        private Int64 _finishedTime;
        public string Id { get; }

        /// <summary>
        /// operation expires 1 minute after it finishes - give the web client time to get the status before removal
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return _finished && (Environment.TickCount64 - _finishedTime) > 60000;
            }
        }

        public PasteOperation(ClipboardManager clipboardManager, string operationId, SessionClipboard clipboard, string destinationFolder, bool overwrite)
        {
            _clipboardManager = clipboardManager;
            Id = operationId;
            _clipboard = clipboard;
            _destinationFolder = destinationFolder;
            _overwrite = overwrite;
            _finished = false;

            _title = _clipboard.Action switch
            {
                SessionFolderClipboardAction.Move => "Moving Files",
                SessionFolderClipboardAction.Copy => "Copying Files",
                _ => _clipboard.Action.ToString(),
            };
            _label = "preparing...";

            _results = [];
            _task = Run();
        }

        private async Task Run()
        {
            _startTime = Environment.TickCount64;
            foreach (var file in _clipboard.Files)
            {
                await Task.Run(() =>
                {
                    void action(SessionFolderClipboardAction clipboardAction, FolderPath path)
                    {
                        string verb = _clipboard.Action switch
                        {
                            SessionFolderClipboardAction.Move => "Moving",
                            SessionFolderClipboardAction.Copy => "Copying",
                            _ => _clipboard.Action.ToString(),
                        };
                        _label = string.Format("{0} {1}", verb, path.Name);
                    }
                    _results.Add(_clipboardManager.PasteClipboard(_clipboard.Action, file, _destinationFolder, _overwrite, action));
                });
            }

            _finishedTime = Environment.TickCount64;
            var duration = _finishedTime - _startTime;

            // artificial delay.
            // Ensure the operation takes at least 1 second.
            // this gives the web-ui time to show the modal before hiding it again
            const int MinTime = 1000;
            if (duration < MinTime)
            {
                await Task.Delay(MinTime - (int)duration);
                _finishedTime = Environment.TickCount64;
            }
            _label = "complete";
            _finished = true;
        }

        public async Task<FileOperationAsyncResultViewModel> Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var isTimeout = (await Task.WhenAny(_task, Task.Delay(timeout, cancellationToken)) != _task);
            if (isTimeout)
            {
                return new FileOperationAsyncResultViewModel(Id, ClipboardResultCode.InProgress, _title, _label, [.. _results]);
            }
            else
            {
                ClipboardResultCode resultCode = ClipboardResultCode.Success;
                ClipboardResult[] results = [.. _results];
                string message = _label;
                int failures = results.Count(x => !x.Success);

                if (failures == results.Length)
                    resultCode = ClipboardResultCode.Error;
                else if (failures > 0)
                    resultCode = ClipboardResultCode.Partial;

                if (failures > 0)
                {
                    var errorResult = results.First(x => !x.Success);
                    message = errorResult.Message;
                }


                return new FileOperationAsyncResultViewModel(Id, resultCode, _title, message, results);
            }
        }
    }
}