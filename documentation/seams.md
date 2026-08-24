# Seams

Points designed to be changed. Each is marked in code with a greppable
`// SEAM:` comment, and `build/Lint-Generated.ps1` fails CI if a documented seam
disappears from generated output — so this file cannot drift away from the code.

```bash
grep -rn "SEAM:" .
```

## SEAM: database provider

SQLite and SQL Server are both first class, chosen with `--database`. For anything
else, three places:

- `Directory.Build.props` — the provider `PackageReference`
- `Program.cs` — the `UseSqlite` / `UseSqlServer` call
- `Data/SqliteDatabasePath.cs` — SQLite-only path handling; delete it for a
  server-based provider

Then delete `Migrations/` and re-run `dotnet ef migrations add Initial`.
Migrations are provider specific and are not portable.

**Why the choice matters:** SQLite has no native `decimal`. Ordering by one
throws `SQLite cannot order by expressions of type 'decimal'`, and the same
applies to `DateTimeOffset`, `TimeSpan` and `ulong`. A value converter to
`double` avoids it at the cost of precision.

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

## SEAM: password pepper (`--auth pepper` and `--auth protected`)

`Protection/PepperedPasswordHasher.cs` wraps Identity's `PasswordHasher` and
mixes a secret into every password before hashing. A salt defends against
precomputation and lives beside the hash; a pepper defends against an attacker
who has the database but not the configuration, so it is worth something only
while it lives somewhere a database dump does not.

A random pepper is generated into `appsettings.Development.json` when the project
is created, so every generated project has its own. **That file is committed, so
it is a development value.** For anything else, set `Pepper__Value` as an
environment variable, or use user-secrets.

**Changing the pepper invalidates every existing password hash.** Nobody can sign
in afterwards, and there is no migration path: the old passwords cannot be
re-hashed without the plaintext. Treat it as permanent, or plan a forced reset.

Concatenation is safe here because the base hasher is PBKDF2. It would not be
safe with bcrypt, which truncates at 72 bytes.

## SEAM: protected personal data (`--auth protected`)

`Protection/LookupProtector.cs` and `Protection/KeyRing.cs` encrypt columns
marked `[ProtectedPersonalData]`, including the email and username columns
Identity declares itself.

**Losing the keys makes that data unrecoverable.** The addresses are ciphertext,
so without the key the accounts cannot be found, matched or restored. There is no
reset flow. Back the keys up somewhere other than the database they protect.

The encryption is deliberately deterministic: the IV is derived from an HMAC of
the plaintext, so the same input always yields the same ciphertext, which is what
keeps the column searchable. `FindByEmailAsync` works by encrypting the address
and comparing. The cost is that equal plaintexts are visibly equal in the
database, and a guess can be confirmed by encrypting it. That trade is inherent
to searchable encryption rather than to this implementation.

Rotation: add a new id to `LookupProtection:Keys`, point `CurrentKeyId` at it,
and keep the old entry. Identity stores the key id with each value, so old rows
stay readable.
