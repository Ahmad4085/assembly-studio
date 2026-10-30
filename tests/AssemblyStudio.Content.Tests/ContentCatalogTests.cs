using Xunit;

namespace AssemblyStudio.Content.Tests;

/// <summary>TASK-090: the embedded content catalog (C09, FR-068/FR-069 support), with the shipped catalog and test catalogs.</summary>
public sealed class ContentCatalogTests
{
    private static EmbeddedContentCatalog Fixture(string name) => new(typeof(ContentCatalogTests).Assembly, $"fixtures/{name}/");

    [Fact]
    public void The_built_in_catalog_loads_and_every_item_is_readable()
    {
        EmbeddedContentCatalog catalog = EmbeddedContentCatalog.BuiltIn();

        Assert.All(catalog.Items, item => Assert.False(string.IsNullOrEmpty(catalog.ReadText(item)), item.Id));
    }

    [Fact]
    public void The_built_in_samples_are_listed_with_expected_output_and_the_keyboard_sample_with_its_input()
    {
        ContentItem[] samples = [.. EmbeddedContentCatalog.BuiltIn().Items.Where(i => i.Kind == ContentKind.Sample)];

        Assert.True(samples.Length >= 8);
        Assert.All(samples, s => Assert.StartsWith("Samples/", s.File, StringComparison.Ordinal));
        Assert.Equal(["Ada\r", "y"], samples.Single(s => s.Id == "keyboard-input").Input);
        Assert.All(samples.Where(s => s.Id != "keyboard-input"), s => Assert.Null(s.Input));
    }

    [Fact]
    public void A_catalog_lists_its_items_in_order_with_their_metadata()
    {
        EmbeddedContentCatalog catalog = Fixture("valid");

        Assert.Equal(["hello", "registers"], catalog.Items.Select(i => i.Id));
        ContentItem hello = catalog.Items[0];
        Assert.Equal((ContentKind.Sample, "Hello", "Prints a greeting.", "hello.asm", "Hello\r\n"),
            (hello.Kind, hello.Title, hello.Summary, hello.FileName, hello.ExpectedOutput));
        Assert.Equal(ContentKind.Tutorial, catalog.Items[1].Kind);
        Assert.Equal("hello", catalog.Items[1].Sample);
        Assert.Null(catalog.Items[1].ExpectedOutput);
    }

    [Fact]
    public void Item_text_is_read_exactly_from_the_embedded_file()
    {
        EmbeddedContentCatalog catalog = Fixture("valid");

        Assert.Equal("; Hello sample (test fixture)\norg 100h\nmov ah, 09h\n", catalog.ReadText(catalog.Items[0])); // LF, as the repository stores it
        Assert.Equal("Registers tutorial (test fixture)\n", catalog.ReadText(catalog.Items[1]));
    }

    [Fact]
    public void Only_items_of_the_catalog_can_be_read()
    {
        EmbeddedContentCatalog catalog = Fixture("valid");
        var stranger = new ContentItem("x", ContentKind.Help, "X", "s", "Samples/hello.asm");

        Assert.Throws<ArgumentException>(() => catalog.ReadText(stranger));
    }

    [Theory]
    [InlineData("duplicate", "used twice")]
    [InlineData("missing-file", "'Reference/gone.md' is not embedded")]
    [InlineData("sample-without-output", "expected output")]
    [InlineData("input-on-tutorial", "only samples have input")]
    [InlineData("tutorial-without-sample", "links to a sample")]
    [InlineData("bad-kind", "not valid")]
    [InlineData("bad-json", "not valid")]
    [InlineData("no-such-catalog", "is not embedded")]
    public void A_malformed_catalog_is_rejected_with_the_reason(string name, string reason)
    {
        ContentCatalogException error = Assert.Throws<ContentCatalogException>(() => Fixture(name));

        Assert.Contains(reason, error.Message, StringComparison.Ordinal);
    }
}
