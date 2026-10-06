param([string]$Filter = "")
$ErrorActionPreference = "Stop"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe"
$results = "Logs/editmode-results.xml"
$log = "Logs/editmode.log"
New-Item -ItemType Directory -Force Logs | Out-Null
Remove-Item $results -ErrorAction SilentlyContinue
$unityArgs = @("-batchmode", "-nographics", "-projectPath", (Get-Location).Path,
    "-runTests", "-testPlatform", "EditMode", "-testResults", $results, "-logFile", $log)
if ($Filter) { $unityArgs += @("-testFilter", $Filter) }
& $unity @unityArgs | Out-Null
if (-not (Test-Path $results)) {
    Write-Host "没有测试结果。编译错误："
    Select-String -Path $log -Pattern "error CS\d+" | ForEach-Object { $_.Line }
    exit 1
}
[xml]$xml = Get-Content $results
$run = $xml.'test-run'
Write-Host "result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed)"
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host "FAILED $($case.fullname)"
    Write-Host $case.SelectSingleNode("failure/message").InnerText
}
if ($run.failed -ne "0") { exit 1 }
