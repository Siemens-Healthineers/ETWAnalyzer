param([string] $InputFileName)
# Replace in .wprp file Strict="true" to nothing to make recording of wpr data possible
# even when some CPU counter recording is already running
# This looks to be some bug of WPR which bails out even if you do not intend to record any CPU counters

Write-Host Input: $InputFileName
$var=(Get-Content -path $InputFileName -Raw);
$replaced=$var.Replace('Strict="true"','');
#Write-Host $replaced
Set-Content -Value $replaced -Path $InputFileName
