# wwwroot/lib

Client-side assets are **vendored and committed** rather than restored from a CDN
or LibMan, so a generated project works with no network.

```
lib/bootstrap/css/bootstrap.min.css
lib/bootstrap/js/bootstrap.bundle.min.js
lib/jquery/jquery.min.js
lib/jqueryval/jquery.validate.min.js
lib/jqueryval/jquery.validate.unobtrusive.min.js
```

Both jQuery Validation packages share one short `jqueryval/` folder rather than
`jquery-validation/` and `jquery-validation-unobtrusive/`. That is the
convention the older ASP.NET MVC templates used, and it matters here: the
template ships a nested project tree, so those two folder names alone pushed the
packaged path past NuGet's long-path warning threshold.

Run `build/Get-VendorAssets.ps1` in the template repository to refresh these from
upstream and commit the result. That script is the only place that touches the
network.
