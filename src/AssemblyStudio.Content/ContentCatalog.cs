using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssemblyStudio.Content;

/// <summary>The kinds of built-in learning content (UI-015).</summary>
public enum ContentKind
{
    /// <summary>A sample program, opened as an untitled copy (FR-068, FR-069).</summary>
    Sample,

    /// <summary>A beginner tutorial (FR-070).</summary>
    Tutorial,

    /// <summary>A concept or instruction reference page (FR-071).</summary>
    Reference,

    /// <summary>Help: supported scope, limitations, shortcuts (FR-032, FR-045).</summary>
    Help,
}

/// <summary>One piece of built-in content.</summary>
/// <param name="Id">Stable identifier, unique in the catalog (for example <c>hello</c>).</param>
/// <param name="Kind">What it is.</param>
/// <param name="Title">The title shown in the Learn sidebar.</param>
/// <param name="Summary">One sentence shown under the title.</param>
/// <param name="File">The content file, relative to the content root (for example <c>Samples/hello.asm</c>).</param>
/// <param name="ExpectedOutput">For samples: the documented console output, which the program-level tests check (AC-035).</param>
public sealed record ContentItem(string Id, ContentKind Kind, string Title, string Summary, string File, string? ExpectedOutput = null)
{
    /// <summary>The content file's name, such as <c>hello.asm</c> (a sample copy's suggested Save As name).</summary>
    public string FileName => File[(File.LastIndexOf('/') + 1)..];
}

/// <summary>The built-in, read-only learning content (C09, architecture §5: embedded resources, offline).</summary>
public interface IContentCatalog
{
    /// <summary>All items in catalog order.</summary>
    IReadOnlyList<ContentItem> Items { get; }

    /// <summary>The text of <paramref name="item"/>.</summary>
    string ReadText(ContentItem item);
}

/// <summary>A catalog with no content (used when the built-in catalog can't be loaded).</summary>
public sealed class EmptyContentCatalog : IContentCatalog
{
    /// <summary>The shared instance.</summary>
    public static EmptyContentCatalog Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<ContentItem> Items { get; } = [];

    /// <inheritdoc />
    public string ReadText(ContentItem item) => throw new ArgumentException($"'{item?.Id}' is not in this catalog.", nameof(item));
}

/// <summary>Thrown when a content catalog is malformed (a build-time content error, caught by the content tests).</summary>
public sealed class ContentCatalogException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ContentCatalogException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    public ContentCatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    public ContentCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// A catalog read from embedded resources: <c>content/catalog.json</c> lists the items, and each item's file is the
/// resource <c>content/&lt;File&gt;</c>. Everything is checked when the catalog is created (unique ids, known kinds,
/// non-empty titles, every file present, expected output only on samples), so a content mistake fails the content
/// tests instead of reaching the user.
/// </summary>
public sealed class EmbeddedContentCatalog : IContentCatalog
{
    /// <summary>The resource name prefix of the built-in content.</summary>
    public const string DefaultRoot = "content/";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly Assembly _assembly;
    private readonly string _root;
    private readonly Dictionary<string, string> _resources; // "content/Samples/hello.asm" → the manifest resource name

    /// <summary>Loads the catalog embedded in <paramref name="assembly"/> under <paramref name="root"/>.</summary>
    /// <exception cref="ContentCatalogException">The catalog is missing or malformed.</exception>
    public EmbeddedContentCatalog(Assembly assembly, string root = DefaultRoot)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrEmpty(root);
        _assembly = assembly;
        _root = root;
        _resources = assembly.GetManifestResourceNames().ToDictionary(n => n.Replace('\\', '/'), n => n, StringComparer.Ordinal);
        Items = Load();
    }

    /// <summary>The catalog shipped with the application (this assembly's resources).</summary>
    public static EmbeddedContentCatalog BuiltIn() => new(typeof(EmbeddedContentCatalog).Assembly);

    /// <inheritdoc />
    public IReadOnlyList<ContentItem> Items { get; }

    /// <inheritdoc />
    public string ReadText(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!Items.Contains(item))
        {
            throw new ArgumentException($"'{item.Id}' is not in this catalog.", nameof(item));
        }

        return Read(_root + item.File);
    }

    private System.Collections.ObjectModel.ReadOnlyCollection<ContentItem> Load()
    {
        string catalog = _root + "catalog.json";
        if (!_resources.ContainsKey(catalog))
        {
            throw new ContentCatalogException($"The content catalog '{catalog}' is not embedded in {_assembly.GetName().Name}.");
        }

        List<ContentItem> items;
        try
        {
            items = JsonSerializer.Deserialize<CatalogFile>(Read(catalog), Options)?.Items
                    ?? throw new ContentCatalogException("The content catalog is empty.");
        }
        catch (JsonException ex)
        {
            throw new ContentCatalogException($"The content catalog is not valid: {ex.Message}", ex);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ContentItem item in items)
        {
            string where = $"Content item '{item.Id}'";
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id))
            {
                throw new ContentCatalogException($"{where}: the id is empty or used twice.");
            }

            if (!Enum.IsDefined(item.Kind) || string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Summary))
            {
                throw new ContentCatalogException($"{where}: the kind, title and summary are required.");
            }

            if (item.File is null || !_resources.ContainsKey(_root + item.File))
            {
                throw new ContentCatalogException($"{where}: the file '{item.File}' is not embedded.");
            }

            if (item.Kind == ContentKind.Sample ? item.ExpectedOutput is null : item.ExpectedOutput is not null)
            {
                throw new ContentCatalogException($"{where}: samples, and only samples, document their expected output.");
            }
        }

        return items.AsReadOnly();
    }

    private string Read(string resource)
    {
        using Stream stream = (_resources.TryGetValue(resource, out string? name) ? _assembly.GetManifestResourceStream(name) : null)
            ?? throw new ContentCatalogException($"The resource '{resource}' is not embedded.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private sealed record CatalogFile(List<ContentItem> Items);
}
