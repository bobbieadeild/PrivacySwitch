$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $compiler = @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $compiler) { throw 'The .NET Framework C# compiler is missing. Install a suitable .NET Framework development environment.' }
    & $compiler /nologo /target:winexe /out:PrivacySwitch.exe /win32icon:PrivacySwitch.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll PrivacySwitch.cs CoreAudio.cs Window.cs CopilotKey.cs CopilotApp.cs
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
    foreach ($test in @('--test','--test-key','--test-app','--test-ui')) {
        $process = Start-Process (Join-Path $PSScriptRoot 'PrivacySwitch.exe') -ArgumentList $test -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "Simulation failed: $test (exit $($process.ExitCode))" }
        Write-Output "$test passed"
    }
    Write-Output 'Build and all four simulation suites passed.'
} finally { Pop-Location }

