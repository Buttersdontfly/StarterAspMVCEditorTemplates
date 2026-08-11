using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace StarterAspMVCEditorTemplates.Tests;

/// <summary>
/// Small helpers for driving the HTML forms in tests.
///
/// Posting to any of the account actions requires the antiforgery token, which
/// only exists in the rendered page. So every POST test has to GET the form
/// first, read the hidden field, and send it back along with the cookie -- which
/// the HttpClient does automatically because HandleCookies defaults to true.
/// </summary>
public static class HtmlPage
{
    private static readonly HtmlParser Parser = new();

    public static async Task<IHtmlDocument> ReadAsync(HttpResponseMessage response) =>
        (IHtmlDocument)await Parser.ParseDocumentAsync(await response.Content.ReadAsStringAsync());

    public static string AntiforgeryToken(IHtmlDocument document)
    {
        var input = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
        return input?.Value
               ?? throw new InvalidOperationException(
                   "No antiforgery token on the page. Does the form use asp-action, and is ValidateAntiForgeryToken still on the action?");
    }

    /// <summary>
    /// Every name attribute rendered by the page's inputs and selects. Used to
    /// assert that editor templates produce the field names the model binder
    /// expects -- a template that renders name="" binds nothing and fails
    /// silently, with validation errors that look like user error.
    /// </summary>
    public static IReadOnlyList<string> FieldNames(IHtmlDocument document) =>
        document.QuerySelectorAll("input, select, textarea")
            .Select(e => e.GetAttribute("name"))
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();
}
