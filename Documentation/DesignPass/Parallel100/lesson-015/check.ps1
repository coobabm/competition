$ErrorActionPreference = 'Stop'
# In-memory compiler + fixture events only. No Unity or persistence API is invoked.
Add-Type -Path @(
    (Join-Path $PSScriptRoot 'XgLessonSubscription.cs'),
    (Join-Path $PSScriptRoot 'SubscriptionChecks.cs')
)
[SubscriptionChecks]::Run()
