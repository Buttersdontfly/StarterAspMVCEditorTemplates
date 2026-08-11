using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SampleIdentityApp.Models;
using SampleIdentityApp.Services;

namespace SampleIdentityApp.Controllers;

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
    /// The editor template kitchen sink: every template rendered in valid,
    /// invalid and empty states.
    ///
    /// This page is also the anchor for the L5 test. If you add an editor
    /// template and do not add it here, that test fails — which is what stops
    /// coverage quietly rotting as the template grows. See
    /// documentation/editor-templates.md.
    /// </summary>
    [HttpGet("editors")]
    public IActionResult Editors()
    {
        var model = new EditorGalleryModel
        {
            Filled = new EditorSampleModel
            {
                Email = "someone@example.com",
                Password = "not-a-real-password",
                Name = new PersonNameInputModel { FirstName = "Ada", LastName = "Lovelace" },
                Address = new AddressInputModel
                {
                    Line1 = "12 Example Street",
                    City = "Wiener Neustadt",
                    PostalCode = "2700",
                    Country = "AT"
                }
            },
            Empty = new EditorSampleModel()
        };

        // Force the invalid state so the error styling is visible without
        // anyone having to submit the form by hand.
        ModelState.AddModelError("Invalid.Email", "Enter an email address in the form name@example.com.");
        ModelState.AddModelError("Invalid.Password", "Choose a password.");
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

public class EditorSampleModel
{
    [Display(Name = "Email")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Name")]
    public PersonNameInputModel Name { get; set; } = new();

    [Display(Name = "Address")]
    public AddressInputModel Address { get; set; } = new();
}
