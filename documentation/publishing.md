# Publishing setup

## One-time, on nuget.org

1. Log in to nuget.org, click your username, choose **Trusted Publishing**.
2. Add a policy with:
   - Repository owner: `Buttersdontfly`
   - Repository: `StarterAspMVCEditorTemplates`
   - Workflow file: `publish.yml` — **filename only**, not the
     `.github/workflows/` path
   - Environment: `nuget-release` — must match `environment:` in the workflow.
     Leave blank only if you drop the environment gate.

If you do not see the Trusted Publishing option, it is still rolling out
gradually to accounts.

## One-time, on GitHub

1. Create the `nuget-release` environment (Settings → Environments). Add a
   required reviewer if you want a human gate before publish.
2. Add a repository secret `NUGET_USER` containing your **nuget.org profile
   name** — not your email address. This is not a credential; the actual key is
   issued per-run via OIDC.

## The private-repo caveat

This matters given the repo is private for now.

A policy created against a private repo starts **temporarily active for 7 days**.
If no publish happens in that window, it goes inactive. You can restart the
7-day window at any time, including after it has expired.

The reason is that NuGet needs GitHub's numeric repository and owner IDs to bind
the policy to the original repo, which prevents a resurrection attack — deleting
a repo, recreating it under the same name, and publishing as though nothing
changed. Those IDs are readable once the repo is public.

Practical options:

- Publish the first release within 7 days of creating the policy, or
- Make the repo public before creating the policy, or
- Just restart the window when you are ready to ship.

Either way, nothing breaks permanently. It is only worth knowing so that a
`401: No matching trust policy owned by user` failure on release day is not a
surprise.

## Package ID

`StarterAspMVCEditorTemplates` is unclaimed, so the first successful push
registers it to your account. Confirm it is still free right before the first
release — IDs are first-come and cannot be transferred casually afterwards.

## Releasing

```bash
git tag v0.1.0
git push origin v0.1.0
```

Then publish a GitHub Release pointing at the tag. MinVer reads the tag for the
package version. Prereleases work as expected: `v0.1.0-beta.1`.

The workflow packs, then **installs the packed nupkg and generates and builds
both combos before pushing**. Publishing a template that does not instantiate is
the failure mode most worth 90 seconds of CI.

The OIDC-issued key lasts about an hour, so the login step sits immediately
before the push rather than at the top of the job.
