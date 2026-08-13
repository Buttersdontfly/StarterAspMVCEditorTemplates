# Editor templates

Every form field in this project is rendered by a template in
`Views/Shared/EditorTemplates/`. Change one file and every form using it changes.
This guide covers how MVC picks a template, what each one does, how to pass
options, and how to add your own.

Run the app and open **`/dev/editors`** to see them all side by side
(Development only, Identity combo).

---

## 1. How MVC chooses a template

When you write `@Html.EditorFor(m => m.Something)`, MVC builds a list of
candidate template names and uses the first file that exists:

| # | Source | Example | Beats |
|---|---|---|---|
| 1 | Name passed to `EditorFor` | `@Html.EditorFor(m => m.X, "Rating")` | everything |
| 2 | `[UIHint("Name")]` | `[UIHint("Dropdown")]` | 3, 4, 5 |
| 3 | `[DataType(...)]` | `[DataType(DataType.Url)]` to `Url.cshtml` | 4, 5 |
| 4 | The model's **type name** | `decimal` to `Decimal.cshtml` | 5 |
| 5 | Base types, then `Object` / `String` | framework default | — |

Three consequences worth internalising:

- **A type-name template applies everywhere automatically.** Adding
  `Decimal.cshtml` restyles every `decimal` in the app at once. That is both the
  power and the risk.
- **`[DataType]` beats the type name.** A `string` with
  `[DataType(DataType.Url)]` gets `Url.cshtml`, not `String.cshtml`.
- **`[UIHint]` beats both**, which is why anything that cannot be inferred from
  the type — a dropdown, a rating, a colour picker — uses one.

### Why this project mixes the mechanisms

Scalars carrying a meaningful `DataType` use it, because the attribute already
says what the value *is*. Anything the type cannot express uses `[UIHint]`.
Complex types resolve on their type name, having no data type at all.

Each template opens with a comment stating which rule fires it, so the mix reads
as deliberate rather than accidental.

---

## 2. The templates

### Resolved by type name — apply automatically

| Template | Model | Notes |
|---|---|---|
| `String.cshtml` | `string` | Fallback for any string with no hint or data type |
| `Int32.cshtml` | `int` | `min`/`max` read from `[Range]` |
| `Decimal.cshtml` | `decimal` | Currency prefix, right aligned, `step` overridable |
| `Boolean.cshtml` | `bool` | Bootstrap switch; `asp-for` emits the hidden companion field so an unchecked box still posts |
| `DateOnly.cshtml` | `DateOnly` | `type="date"` |
| `TimeOnly.cshtml` | `TimeOnly` | `type="time"` |
| `DateTime.cshtml` | `DateTime` | `type="datetime-local"` |
| `Enum.cshtml` | any enum | Options generated from the enum, honouring `[Display(Name=)]`; a nullable enum gains an empty option |

### Resolved by `[DataType]`

| Template | Attribute |
|---|---|
| `EmailAddress.cshtml` | `[DataType(DataType.EmailAddress)]` |
| `Password.cshtml` | `[DataType(DataType.Password)]` |
| `Url.cshtml` | `[DataType(DataType.Url)]` |
| `PhoneNumber.cshtml` | `[DataType(DataType.PhoneNumber)]` |
| `MultilineText.cshtml` | `[DataType(DataType.MultilineText)]` |

### Forced with `[UIHint]`

| Template | Model | Requires |
|---|---|---|
| `Dropdown.cshtml` | any | `items` |
| `RadioGroup.cshtml` | enum or any | `items`, unless the model is an enum |
| `CheckboxList.cshtml` | `IEnumerable<int>` | `items` |
| `Tags.cshtml` | `IEnumerable<string>` | — |
| `Color.cshtml` | `string` | — |
| `Rating.cshtml` | `int` | `[Range]` sets the maximum (default 5) |
| `Range.cshtml` | `int` | `[Range]` sets the bounds |
| `FileUpload.cshtml` | `IFormFile` | form needs `enctype="multipart/form-data"` |
| `UserName.cshtml` | `string` | part of the username/email seam |

### Complex types and collections

| Template | Model |
|---|---|
| `PersonNameInputModel.cshtml` | `PersonNameInputModel` |
| `AddressInputModel.cshtml` | `AddressInputModel` |
| `LineItem.cshtml` | `LineItem`, rendered once per collection element |

---

## 3. Passing options

The second argument to `EditorFor` is `additionalViewData`. Every property of
that anonymous object lands in the template's `ViewData`:

```csharp
@Html.EditorFor(m => m.Budget,   new { currency = "CHF", step = "0.05" })
@Html.EditorFor(m => m.Bio,      new { rows = 8 })
@Html.EditorFor(m => m.Password, new { autocomplete = "new-password" })
@Html.EditorFor(m => m.Country,  new { items = ViewData["Countries"] })
@Html.EditorFor(m => m.Skills,   new { items = skillItems, columns = 2 })
@Html.EditorFor(m => m.Photo,    new { accept = ".jpg,.png", currentFile = Model.PhotoName })
```

Read inside a template as:

```csharp
var rows = ViewData["rows"] as int? ?? 4;
```

Options the shipped templates honour:

| Option | Templates | Default |
|---|---|---|
| `items` | `Dropdown`, `RadioGroup`, `CheckboxList` | empty / enum members |
| `currency` | `Decimal` | `EUR` |
| `step` | `Decimal`, `Range` | `0.01` / `1` |
| `rows` | `MultilineText` | `4` |
| `columns` | `CheckboxList` | `3` |
| `inline` | `RadioGroup` | `true` |
| `accept`, `currentFile` | `FileUpload` | — |
| `autocomplete` | `Password` | inferred from the property name |
| `placeholder` | `Dropdown` | `-- select --` |

**`additionalViewData` does not cascade.** It reaches the template you called,
not templates that one renders in turn. Pass it again if a nested template needs
it.

---

## 4. What the model tells the template

Templates read `ViewData.ModelMetadata` rather than taking parameters, so the
model stays the single source of truth:

```csharp
[Display(Name = "Unit price", Prompt = "0.00", Description = "Excluding VAT.")]
[Range(0, 1_000_000)]
public decimal UnitPrice { get; set; }
```

| Metadata | Comes from | Used for |
|---|---|---|
| `GetDisplayName()` | `[Display(Name=)]`, else the property name | the label |
| `Placeholder` | `[Display(Prompt=)]` | the placeholder |
| `Description` | `[Display(Description=)]` | help text under the field |
| `IsRequired` | `[Required]`, or a non-nullable value type | the `*` marker |
| `ValidatorMetadata` | validation attributes | `Int32`, `Range` and `Rating` read `[Range]` for bounds |

Changing `[Range(0, 11)]` moves the slider bounds. No view edit.

---

## 5. Field names and binding

This is where custom templates usually go wrong.

`asp-for` and `Html.*For` derive the field name from
`ViewData.TemplateInfo.HtmlFieldPrefix`, which already carries the full path
including collection indexes. Inside `LineItem.cshtml`, `asp-for="Description"`
becomes `name="LineItems[0].Description"` — the template never knows the index.

When you need the name yourself, for markup the tag helpers cannot produce:

```csharp
var name   = ViewData.TemplateInfo.GetFullHtmlFieldName(string.Empty);
var baseId = TagBuilder.CreateSanitizedId(name, "_");
```

Never hard-code a `name` attribute. A template that does works at the top level
and silently loses every value inside a collection.

`EditorTemplateTests` asserts that no input renders with an empty `name`, and
that `LineItems[0].Description` and `LineItems[1].Quantity` both appear. The
failure mode is invisible in the browser, so it is worth a test.

### Controls that must not post: `data-no-post`

A few controls are deliberately nameless, because giving them a name would post
a duplicate or a half-typed value:

| Template | Control | Why |
|---|---|---|
| `Tags` | the visible text box | only the hidden inputs it creates should post |
| `Color` | the swatch | the text box beside it owns the name, so one value posts |

Mark such a control with `data-no-post`. That does two things: it states the
intent where the markup is, and it satisfies the empty-name test without
weakening it. A matching test asserts the reverse — anything carrying
`data-no-post` must genuinely have no `name` — so the marker cannot be used to
wave a real mistake through.

### Collections

`@Html.EditorFor(m => m.LineItems)` renders the item template once per element
with the index supplied. Two details matter on the way back:

- **A hidden `Id`** keeps an existing row identifiable, which is how a controller
  tells an edit from an insert.
- **Indexes must stay contiguous.** The default binder stops at the first gap, so
  removing row 1 of 4 would silently discard the rest. `editor-templates.js`
  renumbers after each removal.

---

## 6. Templates that need JavaScript

`wwwroot/js/editor-templates.js` is dependency-free and keyed on `data-`
attributes rather than ids or classes, so restyling a template does not break its
behaviour. It uses event delegation, so markup added after page load works
without re-wiring.

| Template | Behaviour |
|---|---|
| `Tags` | Enter or comma adds a tag; Backspace on an empty box removes the last; each tag is a hidden input |
| `Color` | Swatch and hex box stay in step; only the text box carries the field name, so exactly one value posts |
| `Range` | Live value readout |
| `LineItem` | Remove a row, then renumber the remaining indexes |

Everything else is plain HTML and works with scripting disabled. `Rating` in
particular is radio buttons styled by Bootstrap, not a script.

---

## 7. Adding a template

1. Create `Views/Shared/EditorTemplates/YourThing.cshtml`.
2. Open with a comment saying **which rule fires it**.
3. Read `ViewData.ModelMetadata` for label, placeholder, description and required
   marker, so the model stays authoritative.
4. Use `asp-for` / `Html.*For` so the field name is derived, never hard-coded.
5. Add a property to `EditorSampleModel` so `/dev/editors` renders it.
6. Add its field name to `ExpectedFields` in `EditorTemplateTests`.

Steps 5 and 6 are not bureaucracy: skip them and the template is never rendered
by any test, so it can break with nothing failing.

### Choosing the trigger

- Does the **type** always want this rendering? Name the file after the type.
- Does a `DataType` value describe it? Use `[DataType]`.
- Otherwise use `[UIHint]`, and say so in the comment.

---

## 8. Gotchas

**Overriding `String.cshtml` affects everything** — every string with no hint or
data type. Usually what you want, but check the account pages afterwards.

**`asp-format` is required for date and time inputs.** Without it the value is
written in the current culture's format and `<input type="date">` silently
ignores it: the field renders empty with no error. Hence
`asp-format="{0:yyyy-MM-dd}"`.

**`[UIHint]` on a collection changes what the template receives.** Normally MVC
renders the item template once per element; a `[UIHint]` short-circuits that and
hands the template the *whole* collection. That is exactly what `CheckboxList`
and `Tags` need.

**`bool` needs the hidden companion field.** An unchecked checkbox posts nothing;
`asp-for` emits a hidden `false` alongside so a value always arrives.
Hand-written checkbox markup loses this.

**`enctype="multipart/form-data"`** is required on any form containing
`FileUpload`, or the file silently never arrives.

**Enums post as name or number.** `RadioGroup` accepts both, which matters when a
value round-trips through a URL.

---

## 9. Where to look

| File | Purpose |
|---|---|
| `Views/Shared/EditorTemplates/` | the templates |
| `Models/EditorSampleModel.cs` | one property per template, with worked attributes |
| `Views/Dev/Editors.cshtml` | the gallery |
| `wwwroot/js/editor-templates.js` | behaviour for the four interactive ones |
| `tests/.../EditorTemplateTests.cs` | the coverage guard |

The gallery and its tests ship with `--auth identity` only. With `--auth none`
the templates are all still present and still work; what is missing is the
gallery that showcases them.
