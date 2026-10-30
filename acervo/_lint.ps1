param([string]$Path)
$t = [IO.File]::ReadAllText($Path)
$tok = $null; $err = $null
[void][System.Management.Automation.Language.Parser]::ParseInput($t, [ref]$tok, [ref]$err)
if ($err -and $err.Count -gt 0) {
    foreach ($e in $err) {
        "FAIL L$($e.Extent.StartLineNumber) : $($e.Message)"
        "     >> " + $e.Extent.Text
    }
    "TOTAL ERROS = $($err.Count)"
}
else { "SINTAXE OK :: $(Split-Path -Leaf $Path)" }

