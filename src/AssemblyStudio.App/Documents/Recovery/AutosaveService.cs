namespace AssemblyStudio.App.Documents.Recovery;

/// <summary>
/// Autosave for crash recovery (NFR-011: at most the last 60 s of edits lost). Each <see cref="Tick"/> (every
/// <see cref="DefaultInterval"/> on the UI thread) writes the documents whose unsaved text changed since the last
/// tick, and removes the autosave files of documents that were saved or closed. Failures are logged and retried on the
/// next tick: autosave must never interrupt editing.
/// </summary>
public sealed class AutosaveService
{
    /// <summary>The autosave interval: half the 60 s NFR-011 allows, so a crash loses at most 30 s of edits.</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    private readonly WorkspaceViewModel _workspace;
    private readonly RecoveryStore _store;
    private readonly Dictionary<Guid, string> _written = [];
    private readonly Logging.ILog _log;

    /// <summary>Creates the service for <paramref name="workspace"/>'s documents.</summary>
    public AutosaveService(WorkspaceViewModel workspace, RecoveryStore store, Logging.ILog? log = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(store);
        _log = log ?? Logging.NullLog.Instance;
        _workspace = workspace;
        _store = store;
    }

    /// <summary>Writes changed unsaved documents and removes stale autosave files.</summary>
    public void Tick()
    {
        var open = new HashSet<Guid>();
        foreach (DocumentViewModel document in _workspace.Documents)
        {
            open.Add(document.Id);
            if (document.IsDirty)
            {
                if (!_written.TryGetValue(document.Id, out string? text) || text != document.Text)
                {
                    Try(() => _store.Write(document), () => _written[document.Id] = document.Text);
                }
            }
            else if (_written.ContainsKey(document.Id))
            {
                Try(() => _store.Remove(document.Id), () => _written.Remove(document.Id));
            }
        }

        foreach (Guid closed in _written.Keys.Where(id => !open.Contains(id)).ToList())
        {
            Try(() => _store.Remove(closed), () => _written.Remove(closed));
        }
    }

    private void Try(Action action, Action onSuccess)
    {
        try
        {
            action();
            onSuccess();
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            _log.Write(Logging.LogLevel.Warning, "Autosave failed; retrying on the next tick.", ex);
        }
    }
}
