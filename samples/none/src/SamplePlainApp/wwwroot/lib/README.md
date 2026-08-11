# Vendored client-side assets

Downloaded by `build/Get-VendorAssets.ps1`. Do not edit these files by hand.

| Package | Version |
|---|---|
| bootstrap | 5.3.3 |
| jquery | 3.7.1 |
| jquery-validation | 1.21.0 |
| jquery-validation-unobtrusive | 4.0.0 |

Committed on purpose: the template must work offline, which rules out CDN
references and LibMan restore. To upgrade, re-run the script with new version
parameters and commit the diff.
