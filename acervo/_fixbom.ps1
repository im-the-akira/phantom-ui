$ErrorActionPreference = 'Stop'
Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.ps1 | ForEach-Object {
    $b = [System.IO.File]::ReadAllBytes($_.FullName)
    if (-not ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)) {
        $n = New-Object byte[] ($b.Length + 3)
        $n[0] = 0xEF; $n[1] = 0xBB; $n[2] = 0xBF
        [System.Array]::Copy($b, 0, $n, 3, $b.Length)
        [System.IO.File]::WriteAllBytes($_.FullName, $n)
        "BOM+ : $($_.Name)"
    }
}
