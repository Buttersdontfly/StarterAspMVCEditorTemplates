# Seams

Points designed to be changed. Each is marked in code with a greppable
`// SEAM:` comment, and `build/Lint-Generated.ps1` fails CI if a documented seam
disappears from generated output — so this file cannot drift away from the code.

```bash
grep -rn "SEAM:" .
```

## SEAM: database provider

Three places: `Directory.Build.props` (package reference), `Program.cs`
(`UseSqlite` call), and `Data/SqliteDatabasePath.cs` (SQLite-only path handling,
delete it entirely for a server-based provider).

Swapping providers also means deleting `Migrations/` and re-running
`dotnet ef migrations add Initial`. The shipped migrations are SQLite-generated
and are **not** portable. This is easy but silent, so it is called out in both
places in code.

## SEAM: username identity

`Identity/AccountIdentityConventions.cs` -- the only file that knows how a login
identifier maps onto `IdentityUser.UserName`.

**To separate username from email, flip one constant:**

```csharp
public const bool SignInWithEmail = false;
```

That is the whole change. There is nothing to uncomment.

- `CreateUser` uses the supplied username instead of mirroring the email.
- `FindForSignInAsync` looks the user up by name instead of by email.
- The login view asks for a username instead of an email; the register view asks
  for both.
- Validation follows: the identifier that is actually in use becomes the
  required one.
- The seeded development user gets `SeedData.DevUserName`.
- Sign-in failure copy switches from "email" to "username".

Password reset stays keyed on email in both modes, since the reset link is
delivered there and a username would add nothing.

The `UserName.cshtml` editor template ships in both modes and renders only when
the constant is false.

After flipping it, review `AllowedUserNameCharacters` and `RequireUniqueEmail`
in `Program.cs` -- the defaults suit email-as-username and you may want them
looser or stricter.

### The invariant

Exactly one file may construct an `IdentityUser` or touch its `UserName`. Two
tests and `build/Lint-Generated.ps1` enforce it.

The rule is about `IdentityUser`, not about the word "UserName": the views bind
`m => m.UserName` on the input model, which is the field the user types into and
exactly what the seam exists to make easy.

## SEAM: email sender

Ships with `--auth identity` only. Without the account flows nothing sends mail,
so the sender, the mailbox page and their tests would all be dead code; add an
`IAppEmailSender` of your own when your app needs to send something.

`Services/DevConsoleEmailSender.cs` implements `IAppEmailSender`, which is
adapted onto Identity's `IEmailSender<TUser>`.

The dev implementation writes to the console, writes `.eml` files under
`App_Data/mail/`, and exposes a Development-only `/dev/mailbox` page listing sent
messages with clickable reset links. The mailbox page is not just convenience:
it is what makes the password reset flow assertable in the L4 integration tests.
Console-only output is genuinely hard to test.

Swapping in SMTP or SendGrid is one class plus one DI registration.
