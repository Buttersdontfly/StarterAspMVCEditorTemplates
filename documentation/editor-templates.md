# Editor templates

## Resolution order

MVC picks an editor template in roughly this order:

1. Explicit `@Html.EditorFor(m => m.X, "TemplateName")`
2. `[UIHint("TemplateName")]`
3. `[DataType(...)]` — e.g. `DataType.EmailAddress` → `EmailAddress.cshtml`
4. The model's type name — e.g. `AddressInputModel` → `AddressInputModel.cshtml`
5. Built-in fallbacks (`String`, `Boolean`, `Object`, ...)

This template uses (3) for scalars and (4) for complex types. That mix is
deliberate, not an oversight: scalars carry a meaningful `DataType` already, and
adding `[UIHint]` on top would be redundant noise, while complex types have no
data type to hang off. Every template file opens with a comment stating which
rule fires it.

## Shipped templates

| File | Fires on | Notes |
|---|---|---|
| `EmailAddress.cshtml` | `[DataType(DataType.EmailAddress)]` | `type=email`, `autocomplete=email` |
| `Password.cshtml` | `[DataType(DataType.Password)]` | `autocomplete` varies by usage — see below |
| `PersonNameInputModel.cshtml` | Type name | First / last, rendered as a fieldset |
| `AddressInputModel.cshtml` | Type name | Street, line 2, city, region, postal code, country select |
| `UserName.cshtml` | `[UIHint("UserName")]` | Ships unused; see `seams.md` |

All templates wire `aria-describedby` to their validation message and set
`autocomplete` correctly (`new-password` on register and reset, `current-password`
on login) — both are routinely wrong in hand-rolled Identity pages and are free
to get right here.

## Adding one

1. Add `Views/Shared/EditorTemplates/YourThing.cshtml`.
2. Register it in the `/dev/editors` gallery.
3. Run the tests. If you skipped step 2, the field-name assertion in
   `EditorTemplateTests` fails — that is intentional, and is what stops
   editor-template coverage rotting as the template grows.

Note that `/dev/editors` and its tests ship only with `--auth identity`. With
`--auth none` the editor templates are still there and still work; what is
missing is the gallery, the fake email sender and the mailbox, since without the
account flows nothing renders or sends through them.
