param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,

    [Parameter(Mandatory = $true)]
    [string]$Thumbprint
)

$cert = Get-ChildItem "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction SilentlyContinue

if (-not $cert) {
    Write-Warning "FaskBar dev signing cert ($Thumbprint) not found in Cert:\CurrentUser\My - skip signing. Run scripts/setup-dev-codesign-cert.ps1 as Administrator first."
    exit 0
}

$result = Set-AuthenticodeSignature -FilePath $TargetPath -Certificate $cert

if ($result.Status -ne "Valid") {
    Write-Warning "Sign FaskBar.App.exe khong thanh cong: $($result.StatusMessage)"
}
