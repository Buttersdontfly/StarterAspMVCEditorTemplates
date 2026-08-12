using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StarterAspMVCEditorTemplates.Models;
using StarterAspMVCEditorTemplates.Services;

namespace StarterAspMVCEditorTemplates.Controllers;

/// <summary>
/// Development-only pages. Every action returns 404 outside Development, so
/// this cannot leak if it is left in place by accident.
/// </summary>
[Route("dev")]
public class DevController(IWebHostEnvironment environment, IAppEmailSender emailSender) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!environment.IsDevelopment())
        {
            context.Result = NotFound();
        }
        base.OnActionExecuting(context);
    }

    /// <summary>
    /// The editor template gallery: every template rendered in filled, invalid
    /// and empty states.
    ///
    /// This page is the anchor for EditorTemplateTests. If you add a template
    /// and do not add a property for it to EditorSampleModel, that test fails --
    /// which is what stops coverage quietly rotting. See
    /// documentation/editor-templates.md.
    /// </summary>
    [HttpGet("editors")]
    public IActionResult Editors()
    {
        var model = new EditorGalleryModel
        {
            Filled = new EditorSampleModel
            {
                PlainText = "Anything at all",
                WholeNumber = 42,
                Amount = 1299.50m,
                Enabled = true,
                Status = SampleStatus.InReview,
                OptionalStatus = SampleStatus.Published,
                Email = "someone@example.com",
                Password = "not-a-real-password",
                Website = "https://example.com",
                Phone = "+43 1 234 5678",
                Notes = "A few lines of\nfree text.",
                CountryCode = "AT",
                PreferredContact = SampleStatus.Published,
                Interests = [1, 3],
                Tags = ["mvc", "editor-templates"],
                BrandColor = "#20c997",
                Satisfaction = 4,
                Volume = 9,
                UserName = "ada",
                Name = new PersonNameInputModel { FirstName = "Ada", LastName = "Lovelace" },
                Address = new AddressInputModel
                {
                    Line1 = "12 Example Street",
                    City = "Wiener Neustadt",
                    PostalCode = "2700",
                    Country = "AT"
                },
                LineItems =
                [
                    new LineItem { Id = 1, Description = "Consulting", Quantity = 3, UnitPrice = 250m },
                    new LineItem { Id = 2, Description = "Licence", Quantity = 1, UnitPrice = 799m }
                ]
            },
            Empty = new EditorSampleModel()
        };

        // Force the invalid state so the error styling is visible without anyone
        // having to submit the form by hand.
        ModelState.AddModelError("Invalid.PlainText", "Enter some text.");
        ModelState.AddModelError("Invalid.Email", "Enter an email address in the form name@example.com.");
        ModelState.AddModelError("Invalid.Password", "Choose a password.");
        ModelState.AddModelError("Invalid.WholeNumber", "Enter a number between 0 and 100.");
        ModelState.AddModelError("Invalid.Website", "Enter a valid URL.");
        ModelState.AddModelError("Invalid.Name.FirstName", "Enter a first name.");
        ModelState.AddModelError("Invalid.Address.Line1", "Enter a street address.");

        return View(model);
    }

    /// <summary>
    /// Everything the fake email sender has "sent" this process, newest first.
    /// This is where the password reset links land.
    /// </summary>
    [HttpGet("mailbox")]
    public IActionResult Mailbox() =>
        View(emailSender.Sent.OrderByDescending(m => m.SentAt).ToList());
}

public class EditorGalleryModel
{
    public EditorSampleModel Filled { get; set; } = new();
    public EditorSampleModel Invalid { get; set; } = new();
    public EditorSampleModel Empty { get; set; } = new();
}
