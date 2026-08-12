using System.Net;
using Xunit;

namespace StarterAspMVCEditorTemplates.Tests;

/// <summary>
/// Guards the editor templates themselves, using the /dev/editors gallery.
///
/// The gallery renders every template in filled, invalid and empty states, so
/// these assertions cover all of them at once. Adding a template without adding
/// it to the gallery makes the count assertion fail -- which is the point: it
/// stops editor template coverage rotting silently as the template grows.
/// </summary>
public class EditorTemplateTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    /// <summary>
    /// One entry per editor template. Update this when you add one, and add a
    /// property to EditorSampleModel so the gallery renders it.
    ///
    /// Names are checked with EndsWith because the gallery prefixes each state
    /// (Filled.Email, Empty.Email, ...).
    /// </summary>
    private static readonly string[] ExpectedFields =
    [
        // Resolved by type name
        "PlainText", "WholeNumber", "Amount", "Enabled",
        "Date", "Time", "Timestamp", "Status", "OptionalStatus",
        // Resolved by [DataType]
        "Email", "Password", "Website", "Phone", "Notes",
        // Forced with [UIHint]
        "CountryCode", "PreferredContact", "Interests", "Tags",
        "BrandColor", "Satisfaction", "Volume", "Attachment", "UserName",
        // Complex types
        "Name.FirstName", "Name.LastName",
        "Address.Line1", "Address.Line2", "Address.City",
        "Address.Region", "Address.PostalCode", "Address.Country"
    ];

    [Fact]
    public async Task Gallery_renders_every_editor_template_with_bindable_names()
    {
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await HtmlPage.ReadAsync(response);
        var names = HtmlPage.FieldNames(document);

        var missing = ExpectedFields
            .Where(field => !names.Any(name => name.EndsWith(field, StringComparison.Ordinal)))
            .ToList();

        Assert.True(missing.Count == 0,
            "These fields never rendered on /dev/editors, so their templates are untested:\n"
            + string.Join("\n", missing));
    }

    [Fact]
    public async Task Collection_template_renders_indexed_field_names()
    {
        // The collection case breaks most easily: MVC supplies the index, and a
        // template that builds its own names loses it. Binding then silently
        // drops every row.
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);
        var names = HtmlPage.FieldNames(document);

        Assert.Contains(names, n => n.Contains("LineItems[0].Description", StringComparison.Ordinal));
        Assert.Contains(names, n => n.Contains("LineItems[1].Quantity", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("input[type=\"email\"]")]
    [InlineData("input[type=\"url\"]")]
    [InlineData("input[type=\"tel\"]")]
    [InlineData("input[type=\"date\"]")]
    [InlineData("input[type=\"time\"]")]
    [InlineData("input[type=\"datetime-local\"]")]
    [InlineData("input[type=\"number\"]")]
    [InlineData("input[type=\"range\"]")]
    [InlineData("input[type=\"color\"]")]
    [InlineData("input[type=\"file\"]")]
    [InlineData("input[type=\"radio\"]")]
    [InlineData("input[type=\"checkbox\"]")]
    [InlineData("textarea")]
    [InlineData("select")]
    public async Task Gallery_renders_the_expected_input_types(string selector)
    {
        // Guards the detail that makes these templates worth having. A date
        // field rendered as a plain text box still binds correctly, so nothing
        // else in the suite would notice the regression.
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        Assert.NotEmpty(document.QuerySelectorAll(selector));
    }

    [Fact]
    public async Task Enum_template_uses_display_names()
    {
        // [Display(Name = "In review")] on an enum member must reach the option
        // text, or the UI shows the raw identifier.
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        Assert.Contains("In review", document.Body!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Editor_templates_never_render_an_empty_name()
    {
        // An editor template that loses the field prefix renders name="" and
        // binds nothing. The page still looks correct, and the failure shows up
        // later as validation errors on data the user definitely entered.
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        var unnamed = document.QuerySelectorAll("input, select")
            .Where(e => e.GetAttribute("type") != "hidden")
            .Where(e => string.IsNullOrEmpty(e.GetAttribute("name")))
            .Select(e => e.OuterHtml)
            .ToList();

        Assert.True(unnamed.Count == 0,
            "Inputs rendered without a name attribute:\n" + string.Join("\n", unnamed));
    }

    [Fact]
    public async Task Password_fields_use_the_right_autocomplete_hint()
    {
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        var passwords = document.QuerySelectorAll("input[type='password']").ToList();
        Assert.NotEmpty(passwords);

        foreach (var input in passwords)
        {
            var autocomplete = input.GetAttribute("autocomplete");
            Assert.True(
                autocomplete is "new-password" or "current-password",
                $"Password input has autocomplete='{autocomplete}'. Password managers need new-password or current-password.");
        }
    }

    [Fact]
    public async Task Email_field_is_an_email_input()
    {
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        var emails = document.QuerySelectorAll("input[type='email']").ToList();
        Assert.NotEmpty(emails);
        Assert.All(emails, e => Assert.Equal("email", e.GetAttribute("autocomplete")));
    }

    [Fact]
    public async Task Invalid_state_renders_validation_messages()
    {
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        var document = await HtmlPage.ReadAsync(response);

        var messages = document.QuerySelectorAll(".field-validation-error, .invalid-feedback")
            .Where(e => !string.IsNullOrWhiteSpace(e.TextContent))
            .ToList();

        Assert.True(messages.Count > 0,
            "The gallery's invalid column rendered no validation messages, so error styling is untested.");
    }
}
