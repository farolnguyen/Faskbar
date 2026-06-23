param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPaths,

    [Parameter(Mandatory = $true)]
    [string]$Thumbprint
)

# -File mode khong tach mang theo dau phay, nen truyen nhieu path noi voi nhau bang ';' roi tu split.
$paths = $TargetPaths -split ';'

$cert = Get-ChildItem "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction SilentlyContinue

if (-not $cert) {
    Write-Warning "FaskBar dev signing cert ($Thumbprint) not found in Cert:\CurrentUser\My - skip signing. Run scripts/setup-dev-codesign-cert.ps1 as Administrator first."
    exit 0
}

foreach ($path in $paths) {
    if (-not (Test-Path $path)) {
        continue
    }

    $result = Set-AuthenticodeSignature -FilePath $path -Certificate $cert

    if ($result.Status -ne "Valid") {
        Write-Warning "Sign $path khong thanh cong: $($result.StatusMessage)"
    }
}
