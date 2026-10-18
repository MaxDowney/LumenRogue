param(
    [string]$Compiler = "zig"
)

$ErrorActionPreference = "Stop"
$cppRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $cppRoot
$outputDirectory = Join-Path $projectRoot "bin\cpp"
$sourceFile = Join-Path $cppRoot "LumenRogue.cpp"
$outputFile = Join-Path $cppRoot "LumenRogue-CPP.new.exe"
$releaseFiles = @((Join-Path $outputDirectory "LumenRogue-CPP.exe"), (Join-Path $cppRoot "LumenRogue-CPP.exe"))
$globalCache = Join-Path $cppRoot ".zig-global-cache"
$localCache = Join-Path $cppRoot ".zig-local-cache"

if ($Compiler -eq "zig" -and -not (Get-Command zig -ErrorAction SilentlyContinue)) {
    $bundledCompiler = Join-Path $cppRoot ".zig-full\zig-x86_64-windows-0.15.2\zig.exe"
    if (Test-Path -LiteralPath $bundledCompiler) { $Compiler = $bundledCompiler }
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$env:ZIG_GLOBAL_CACHE_DIR = $globalCache
$env:ZIG_LOCAL_CACHE_DIR = $localCache

& $Compiler c++ $sourceFile -target x86_64-windows-gnu -std=c++20 -O2 -s -static -w -o $outputFile
if ($LASTEXITCODE -ne 0) { throw "C++ compilation failed with exit code $LASTEXITCODE" }

$testOutput = Join-Path $cppRoot ".selftest.out"
$testError = Join-Path $cppRoot ".selftest.err"
$testCommand = '"' + $outputFile + '" --self-test 1>"' + $testOutput + '" 2>"' + $testError + '"'
try {
    & $env:ComSpec /d /c $testCommand
    $testExitCode = $LASTEXITCODE
    if (Test-Path -LiteralPath $testOutput) { Get-Content -LiteralPath $testOutput }
    if (Test-Path -LiteralPath $testError) { Get-Content -LiteralPath $testError }
    if ($testExitCode -ne 0) { throw "Native regression tests failed with exit code $testExitCode" }
}
finally {
    Remove-Item -LiteralPath $testOutput,$testError -Force -ErrorAction SilentlyContinue
}

# Do not replace a running game or touch its save. Keep the tested candidate
# available if the player needs to exit before installation.
$runningRelease = Get-Process -Name 'LumenRogue-CPP' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -in $releaseFiles }
if ($runningRelease) { throw "Build and tests passed. Close the running game, then rerun this script to update both release copies. Candidate: $outputFile" }
foreach ($releaseFile in $releaseFiles) { Copy-Item -LiteralPath $outputFile -Destination $releaseFile -Force }
Get-FileHash -LiteralPath $releaseFiles -Algorithm SHA256
Get-Item -LiteralPath $releaseFiles | Select-Object FullName, Length, LastWriteTime
