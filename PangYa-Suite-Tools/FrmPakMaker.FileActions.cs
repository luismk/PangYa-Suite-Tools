using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;

namespace PangYa_Suite_Tools;

public partial class FrmPakMaker
{
    private readonly List<(ToolStripItem Preview, ToolStripItem Edit, ToolStripItem Stop)> _fileActions = [];
    private PakFileEditSession? _fileEdit;
    private FrmIFFManager? _iffEditWindow;
    private System.Windows.Forms.Timer? _fileEditTimer;
    private bool _checkingFileEdit;
    private bool _reloadingEditedPak;
    private PakFileEditSession.ArchiveStamp[]? _loadedArchiveStamp;
    private sealed class ArchiveChangedDuringEditException : IOException;

    private PakFileEntry? SelectedArchiveFile => GetSingleSelectedArchiveFile(lstEntries.SelectedItems.Count == 1);

    private PakFileEntry? GetSingleSelectedArchiveFile(bool singleSelection) => singleSelection &&
        lstEntries.SelectedItems[0].Tag is PakFileEntry { Type: not PakFileEntryType.Directory } entry
            ? entry : null;

    private void AddFileActions(ToolStrip strip)
    {
        bool menu = strip is ContextMenuStrip;
        ToolStripItem preview = AddAction(ToolbarIconKind.Preview, async (_, _) => await PreviewSelectedFileAsync());
        ToolStripItem edit = AddAction(ToolbarIconKind.Edit, async (_, _) => await EditSelectedFileAsync());
        ToolStripItem stop = AddAction(ToolbarIconKind.Stop, async (_, _) => await EndFileEditAsync(showWhenClean: true));
        _fileActions.Add((preview, edit, stop));
        if (menu)
            ((ContextMenuStrip)strip).Opening += (_, _) => UpdateFileActions(menu: true);
        UpdateFileActions(menu: menu);

        ToolStripItem AddAction(ToolbarIconKind icon, EventHandler click)
        {
            if (!menu)
                return AddOperationButton(strip, click, icon, requiresLoadedPak: false);
            var item = new ToolStripMenuItem { Image = CreateToolbarIcon(icon) };
            item.Click += click;
            strip.Items.Add(item);
            return item;
        }
    }

    private void LocalizeFileActions()
    {
        foreach (var (preview, edit, stop) in _fileActions)
        {
            preview.Text = Strings.PakFile_Preview;
            edit.Text = Strings.PakFile_Edit;
            stop.Text = Strings.PakFile_StopEdit;
        }
    }

    private void UpdateFileActions(bool? singleSelection = null, bool menu = false)
    {
        bool available = !_operationInProgress && !_checkingFileEdit;
        bool selected = available && _currentReader != null &&
            GetSingleSelectedArchiveFile(singleSelection ?? lstEntries.SelectedItems.Count == 1) != null;
        foreach (var (preview, edit, stop) in _fileActions)
        {
            if (menu != (preview is ToolStripMenuItem))
                continue;

            preview.Enabled = selected;
            edit.Enabled = selected;
            stop.Available = _fileEdit != null;
            stop.Enabled = available && _fileEdit != null;
        }
    }

    private async Task PreviewSelectedFileAsync()
    {
        if (_operationInProgress || _currentReader is not { } reader || SelectedArchiveFile is not { } entry)
            return;

        using CancellationTokenSource operation = BeginOperation();
        PakFilePreviewData? preview = null;
        try
        {
            string extension = Path.GetExtension(entry.Name);
            if (extension.Equals(".iff", StringComparison.OrdinalIgnoreCase))
            {
                await PreviewIffAsync(reader, entry, operation.Token);
                return;
            }
            if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                await PreviewWaveAsync(reader, entry, operation.Token);
                return;
            }
            preview = await Task.Run(() => PakFilePreviewData.Load(reader, entry, operation.Token));
            if (!IsDisposed && !operation.IsCancellationRequested)
            {
                using var dialog = new PakFilePreviewWindow(entry.Name, preview);
                dialog.ShowDialog(this);
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowFileMessage(string.Format(Strings.PakFile_PreviewUnavailable, ex.Message));
        }
        finally
        {
            preview?.Dispose();
            if (!IsDisposed)
                EndOperation(operation);
        }
    }

    private async Task PreviewIffAsync(PakReader reader, PakFileEntry entry, CancellationToken token)
    {
        string fileName = Path.GetFileName(entry.Name);
        if (!IsValidLeafFileName(fileName))
            throw new InvalidDataException();
        string? path = null;
        try
        {
            path = await ExtractPreviewFileAsync(reader, entry, fileName, token);
            token.ThrowIfCancellationRequested();
            if (IsDisposed)
                return;

            using var editor = new FrmIFFManager(entry.Name, archivePreview: true);
            await editor.LoadPakEntryAsync(path, token);
            if (!IsDisposed && !token.IsCancellationRequested)
                editor.ShowDialog(this);
        }
        finally
        {
            if (path != null)
                CleanupWorkingCopy(path);
        }
    }

    private async Task PreviewWaveAsync(PakReader reader, PakFileEntry entry, CancellationToken token)
    {
        const int maxPreviewBytes = 64 * 1024 * 1024;
        if (entry.Size > maxPreviewBytes || entry.CompressSize > maxPreviewBytes)
            throw new InvalidDataException(Strings.PakFile_PreviewLimit);

        string path = await ExtractPreviewFileAsync(reader, entry, "audio.wav", token);
        try
        {
            if (!IsDisposed && !token.IsCancellationRequested)
            {
                using var player = new PakWavePlayerWindow(entry.Name, path);
                player.ShowDialog(this);
            }
        }
        finally
        {
            CleanupWorkingCopy(path);
        }
    }

    private static Task<string> ExtractPreviewFileAsync(PakReader reader, PakFileEntry entry, string fileName,
        CancellationToken token) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        string directory = Directory.CreateTempSubdirectory("PangyaPakPreview-").FullName;
        string path = Path.Combine(directory, fileName);
        try
        {
            reader.ExtractEntry(entry, path, writeManifest: false);
            return path;
        }
        catch
        {
            CleanupWorkingCopy(path);
            throw;
        }
    }, token);

    private async Task EditSelectedFileAsync()
    {
        if (_operationInProgress || _checkingFileEdit || _currentReader is not { } reader ||
            SelectedArchiveFile is not { } entry)
            return;
        int ordinal = reader.Entries.IndexOf(entry);
        if (!await ConfirmEndFileEditAsync() || IsDisposed || _operationInProgress)
            return;
        // Stopping the previous session may have rebuilt the same archive.
        if (!ReferenceEquals(_currentReader, reader))
        {
            if (_currentReader is not { } reloaded || reloaded.ArchivePath != reader.ArchivePath ||
                (uint)ordinal >= (uint)reloaded.Entries.Count)
                return;
            reader = reloaded;
            entry = reloaded.Entries[ordinal];
        }

        using CancellationTokenSource operation = BeginOperation();
        string? workingPath = null;
        try
        {
            var loadedStamp = _loadedArchiveStamp;
            var prepared = await Task.Run(() =>
            {
                var archive = PakFileEditSession.CaptureArchive(reader);
                if (loadedStamp == null || !loadedStamp.SequenceEqual(archive))
                    throw new ArchiveChangedDuringEditException();
                string name = GetWorkingCopyName(reader.ArchivePath, entry.Name);
                string directory = Directory.CreateTempSubdirectory("PangyaPakEdit-").FullName;
                string path = Path.Combine(directory, name);
                try
                {
                    operation.Token.ThrowIfCancellationRequested();
                    reader.ExtractEntry(entry, path, writeManifest: false);
                    if (!archive.SequenceEqual(PakFileEditSession.CaptureArchive(reader)))
                        throw new ArchiveChangedDuringEditException();
                    using var input = File.OpenRead(path);
                    return (Path: path, Archive: archive, Hash: SHA256.HashData(input));
                }
                catch
                {
                    CleanupWorkingCopy(path);
                    throw;
                }
            });
            workingPath = prepared.Path;
            operation.Token.ThrowIfCancellationRequested();
            var session = new PakFileEditSession(reader, ordinal, workingPath, prepared.Archive, prepared.Hash);
            _fileEdit = session;
            _fileEditTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _fileEditTimer.Tick += async (_, _) => await CheckFileEditAsync();
            _fileEditTimer.Start();
            if (!await OpenEditWorkingCopyAsync(session, operation))
                DisposeFileEdit(cleanup: true);
        }
        catch (Exception ex)
        {
            DisposeFileEdit(cleanup: true);
            if (workingPath != null)
                CleanupWorkingCopy(workingPath);
            if (!IsDisposed && ex is not OperationCanceledException)
                ShowFileMessage(ex is ArchiveChangedDuringEditException ? Strings.PakFile_ArchiveReload :
                    string.Format(Strings.PakFile_EditFailed, entry.Name, ex.Message));
        }
        finally
        {
            if (!IsDisposed)
                EndOperation(operation);
        }
    }

    private async Task<bool> OpenEditWorkingCopyAsync(PakFileEditSession session, CancellationTokenSource operation)
    {
        string name = session.Reader.Entries[session.Ordinal].Name;
        if (!Path.GetExtension(name).Equals(".iff", StringComparison.OrdinalIgnoreCase))
        {
            if (!LaunchEditor(session.WorkingPath))
                return false;
            lblStatus.Text = string.Format(Strings.PakFile_EditWatching, name);
            return true;
        }

        using var editor = new FrmIFFManager(name, archivePreview: false);
        await editor.LoadPakEntryAsync(session.WorkingPath, operation.Token);
        operation.Token.ThrowIfCancellationRequested();
        if (IsDisposed)
            return false;
        lblStatus.Text = string.Format(Strings.PakFile_EditWatching, name);
        _iffEditWindow = editor;
        try
        {
            EndOperation(operation);
            editor.ShowDialog(this);
            return true;
        }
        finally { _iffEditWindow = null; }
    }

    private async Task ReopenFileEditAsync(PakFileEditSession session)
    {
        if (IsDisposed || _operationInProgress || !ReferenceEquals(_fileEdit, session))
            return;

        using CancellationTokenSource operation = BeginOperation();
        try
        {
            if (!File.Exists(session.WorkingPath))
                throw new FileNotFoundException(null, session.WorkingPath);
            await OpenEditWorkingCopyAsync(session, operation);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && ex is not OperationCanceledException)
                ShowFileMessage(string.Format(Strings.PakFile_EditFailed, session.WorkingPath, ex.Message));
        }
        finally
        {
            if (!IsDisposed)
                EndOperation(operation);
        }
    }

    internal static string GetWorkingCopyName(string archivePath, string entryName)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string archive = Path.GetFileNameWithoutExtension(archivePath);
        string file = Path.GetFileName(entryName.Replace('/', '\\'));
        string name = new string($"{archive}-{file}".Select(character =>
            Array.IndexOf(invalid, character) >= 0 ? '_' : character).ToArray()).TrimEnd('.', ' ');
        const int maxLength = 180;
        if (name.Length <= maxLength)
            return name;

        string extension = Path.GetExtension(name);
        if (extension.Length > 24)
            extension = string.Empty;
        int stemLength = maxLength - extension.Length;
        if (char.IsHighSurrogate(name[stemLength - 1]))
            stemLength--;
        return name[..stemLength].TrimEnd('.', ' ') + extension;
    }

    private async Task CheckFileEditAsync()
    {
        if (_fileEdit is not { IsReady: true } session || _checkingFileEdit || _operationInProgress ||
            _iffEditWindow?.IsSavingPakWorkingCopy == true)
            return;

        _checkingFileEdit = true;
        UpdateFileActions();
        PakFileEditSession.Revision? revision = null;
        try
        {
            int attemptedGeneration = session.Generation;
            try
            {
                revision = await Task.Run(session.CaptureRevision);
            }
            catch (IOException)
            {
                return; // A save can leave the file temporarily missing or locked.
            }
            catch
            {
                session.SkipGeneration(attemptedGeneration);
                throw;
            }
            if (IsDisposed || !ReferenceEquals(_fileEdit, session) || _operationInProgress ||
                _iffEditWindow?.IsSavingPakWorkingCopy == true ||
                session.Generation != revision.Generation)
                return;

            if (!ReferenceEquals(_currentReader, session.Reader) ||
                !await Task.Run(session.MatchesArchive))
            {
                if (ReferenceEquals(_fileEdit, session))
                    EndStaleFileEdit(session);
                return;
            }
            if (IsDisposed || !ReferenceEquals(_fileEdit, session) || _operationInProgress ||
                _iffEditWindow?.IsSavingPakWorkingCopy == true ||
                session.Generation != revision.Generation)
                return;

            if (!session.Observe(revision))
                return;

            PakFileEntry entry = session.Reader.Entries[session.Ordinal];
            if (session.Reader.IsPspArchive && revision.Length != entry.Size)
            {
                ShowFileMessage(string.Format(Strings.PakFile_PspEditSize, entry.Size));
                return;
            }

            if (ConfirmRevision(entry.Name))
                await ApplyFileRevisionAsync(session, revision);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowFileMessage(string.Format(Strings.PakFile_EditFailed, session.WorkingPath, ex.Message));
        }
        finally
        {
            if (revision != null)
                PakFileEditSession.TryDelete(revision.Path);
            _checkingFileEdit = false;
            if (!IsDisposed)
                UpdateFileActions();
        }
    }

    private async Task<bool> ApplyFileRevisionAsync(PakFileEditSession session, PakFileEditSession.Revision revision)
    {
        using CancellationTokenSource operation = BeginOperation();
        try
        {
            var options = BuildRebuildOptionsForCurrentPak();
            var updatedStamp = await Task.Run(() =>
            {
                void ValidateArchive()
                {
                    if (!session.MatchesArchive())
                        throw new ArchiveChangedDuringEditException();
                }
                ValidateArchive();
                PakManager.ReplaceEntry(session.Reader, session.Ordinal, revision.Path, options,
                    message => AppLogger.Instance.Log("PAK Writer", message),
                    (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK), operation.Token,
                    ValidateArchive);
                return PakFileEditSession.CaptureArchive(session.Reader);
            });
            if (IsDisposed)
                return false;
            session.AppliedHash = revision.Hash;
            session.Archive = updatedStamp;
            _reloadingEditedPak = true;
            try
            {
                ReloadPak(session.Reader.ArchivePath, options.FileNameEncoding, _knownCompressionLevel);
            }
            finally
            {
                _reloadingEditedPak = false;
            }
            if (_currentReader is not { } updatedReader || session.Ordinal >= updatedReader.Entries.Count ||
                _loadedArchiveStamp == null || !updatedStamp.SequenceEqual(_loadedArchiveStamp))
            {
                EndStaleFileEdit(session);
                return false;
            }
            session.Reader = updatedReader;
            foreach (ListViewItem item in lstEntries.Items)
            {
                if (!ReferenceEquals(item.Tag, updatedReader.Entries[session.Ordinal]))
                    continue;
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                break;
            }
            lblStatus.Text = Strings.PakMaker_PAKUpdatedSuccessfully;
            return true;
        }
        catch (ArchiveChangedDuringEditException)
        {
            EndStaleFileEdit(session);
            return false;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { return false; }
        finally
        {
            if (!IsDisposed)
            {
                HideProgress();
                EndOperation(operation);
            }
        }
    }

    private bool ConfirmRevision(string name) =>
        ShowFileEditDialog(Strings.PakFile_UpdateTitle, string.Format(Strings.PakFile_UpdateRevision, name),
            stopping: false, canUpdate: true) == DialogResult.Yes;

    private DialogResult ShowFileEditDialog(string title, string message, bool stopping, bool canUpdate, bool canReopen = false)
    {
        using var dialog = new Form
        {
            Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
            ShowInTaskbar = false, MinimizeBox = false, MaximizeBox = false
        };
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Padding = new Padding(12)
        };
        layout.Controls.Add(new Label
        {
            Text = message, AutoSize = true, MaximumSize = new Size(480, 0)
        });
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        var icons = new List<Bitmap>();
        if (canUpdate)
            AddButton(Strings.PakFile_UpdateTitle, DialogResult.Yes, ToolbarIconKind.Update);
        if (canReopen)
            AddButton(Strings.PakFile_Reopen, DialogResult.Retry, ToolbarIconKind.Folder);
        if (stopping)
            AddButton(Strings.PakFile_StopEdit, DialogResult.OK, ToolbarIconKind.Stop);
        Button cancel = AddButton(stopping ? Strings.Common_Cancel : Strings.PakFile_KeepEditing,
            DialogResult.Cancel, ToolbarIconKind.Remove);
        layout.Controls.Add(buttons);
        dialog.Controls.Add(layout);
        dialog.CancelButton = cancel;
        dialog.ActiveControl = cancel;
        try { return dialog.ShowDialog(_iffEditWindow ?? (IWin32Window)this); }
        finally
        {
            foreach (Bitmap icon in icons)
                icon.Dispose();
        }

        Button AddButton(string text, DialogResult result, ToolbarIconKind iconKind)
        {
            using Bitmap artwork = CreateToolbarIcon(iconKind);
            var icon = new Bitmap(artwork, 16, 16);
            icons.Add(icon);
            var button = new Button
            {
                Text = text, Image = icon, TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true, DialogResult = result
            };
            buttons.Controls.Add(button);
            return button;
        }
    }

    private void EndStaleFileEdit(PakFileEditSession session)
    {
        DisposeFileEdit();
        if (!IsDisposed)
            ShowFileMessage(string.Format(Strings.PakFile_ArchiveChanged, session.WorkingPath));
    }

    private Task<bool> ConfirmEndFileEditAsync() => EndFileEditAsync(showWhenClean: false);

    private async Task<bool> EndFileEditAsync(bool showWhenClean)
    {
        if (_fileEdit is not { } session)
            return true;
        if (_checkingFileEdit || _operationInProgress)
            return false;
        bool isClean = session.CanCleanWorkingCopy();
        if (isClean && !showWhenClean)
        {
            DisposeFileEdit(cleanup: true);
            return true;
        }
        _checkingFileEdit = true;
        UpdateFileActions();
        PakFileEditSession.Revision? revision = null;
        bool cleanup = false;
        bool reopen = false;
        try
        {
            if (!isClean)
            {
                try { revision = await Task.Run(session.CaptureRevision); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    // A locked or missing file can still be retained when ending the session.
                }
            }
            if (IsDisposed || !ReferenceEquals(_fileEdit, session) || _operationInProgress)
                return false;

            string message = string.Format(Strings.PakFile_ConfirmStop, session.WorkingPath);
            if (revision != null && revision.Hash.AsSpan().SequenceEqual(session.AppliedHash))
            {
                session.Observe(revision);
                if (!showWhenClean && session.CanCleanWorkingCopy())
                {
                    cleanup = true;
                    return true;
                }
            }

            bool canUpdate = revision != null &&
                !revision.Hash.AsSpan().SequenceEqual(session.AppliedHash) &&
                ReferenceEquals(_currentReader, session.Reader);
            if (canUpdate && session.Reader.IsPspArchive &&
                revision!.Length != session.Reader.Entries[session.Ordinal].Size)
            {
                canUpdate = false;
                message += Environment.NewLine + string.Format(Strings.PakFile_PspEditSize,
                    session.Reader.Entries[session.Ordinal].Size);
            }
            DialogResult result = ShowFileEditDialog(Strings.PakFile_StopEdit, message, stopping: true, canUpdate,
                canReopen: File.Exists(session.WorkingPath));
            if (result == DialogResult.Retry)
            {
                reopen = true;
                return false;
            }
            if (result == DialogResult.OK)
            {
                cleanup = session.CanCleanWorkingCopy();
                if (!cleanup)
                    DisposeFileEdit();
                return true;
            }
            if (result != DialogResult.Yes || revision == null ||
                !await ApplyFileRevisionAsync(session, revision))
                return false;

            session.Observe(revision);
            cleanup = session.CanCleanWorkingCopy();
            return cleanup;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowFileMessage(string.Format(Strings.PakFile_EditFailed, session.WorkingPath, ex.Message));
            return false;
        }
        finally
        {
            if (revision != null)
                PakFileEditSession.TryDelete(revision.Path);
            if (cleanup)
                DisposeFileEdit(cleanup: true);
            _checkingFileEdit = false;
            if (!IsDisposed)
            {
                UpdateFileActions();
                if (reopen)
                    BeginInvoke(new Action(async () => await ReopenFileEditAsync(session)));
            }
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || e.CloseReason != CloseReason.UserClosing || _fileEdit is not { } session)
            return;
        if (!_checkingFileEdit && !_operationInProgress && session.CanCleanWorkingCopy())
        {
            DisposeFileEdit(cleanup: true);
            return;
        }
        e.Cancel = true;
        if (await ConfirmEndFileEditAsync() && !IsDisposed)
            BeginInvoke(Close);
    }

    private void DisposeFileEdit(bool cleanup = false)
    {
        _fileEditTimer?.Dispose();
        _fileEditTimer = null;
        if (_fileEdit is { } session)
        {
            _fileEdit = null;
            session.Dispose();
            if (cleanup)
                CleanupWorkingCopy(session.WorkingPath);
        }
        if (!IsDisposed)
            UpdateFileActions();
    }

    private static void CleanupWorkingCopy(string path)
    {
        PakFileEditSession.TryDelete(path);
        string? directory = Path.GetDirectoryName(path);
        if (directory == null)
            return;
        try { Directory.Delete(directory); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private bool LaunchEditor(string path)
    {
        try
        {
            using var editor = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "edit" });
            return true;
        }
        catch (Win32Exception)
        {
            var info = new OpenAsInfo { File = path, Flags = 4 }; // OAIF_EXEC
            int result = SHOpenWithDialog(Handle, ref info);
            if (result == unchecked((int)0x800704C7) || result == unchecked((int)0x80004004))
                return false;
            Marshal.ThrowExceptionForHR(result);
            return result == 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenAsInfo
    {
        public string File;
        public string? Class;
        public uint Flags;
    }

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHOpenWithDialog(IntPtr owner, ref OpenAsInfo info);

    private void ShowFileMessage(string message) => MessageBox.Show(_iffEditWindow ?? (IWin32Window)this, message, Strings.PakMaker_Warning,
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
