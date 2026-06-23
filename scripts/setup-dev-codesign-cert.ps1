# Chay 1 lan duy nhat, voi PowerShell "Run as Administrator".
# Tao 1 chung chi code-signing tu ky tren may dev, them vao Trusted Root + Trusted Publisher (LocalMachine)
# de Windows Smart App Control tin tuong cac file .exe FaskBar tu build ra (chi danh cho dev local, KHONG dung de phat hanh).
$ErrorActionPreference = "Stop"

$subject = "CN=FaskBar Dev Code Signing"
$existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq $subject } | Select-Object -First 1

if ($existing) {
    $cert = $existing
    Write-Output "Da co cert san: $($cert.Thumbprint)"
} else {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject `
        -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5) `
        -KeyUsage DigitalSignature -FriendlyName "FaskBar Dev Code Signing"
    Write-Output "Da tao cert moi: $($cert.Thumbprint)"
}

foreach ($storeName in @("Root", "TrustedPublisher")) {
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, "LocalMachine")
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $alreadyIn = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
    if (-not $alreadyIn) {
        $store.Add($cert)
        Write-Output "Da them cert vao LocalMachine\$storeName"
    } else {
        Write-Output "Cert da co trong LocalMachine\$storeName"
    }
    $store.Close()
}

Write-Output "Thumbprint (dung trong FaskBar.App.csproj -> FaskBarDevSigningThumbprint): $($cert.Thumbprint)"
Write-Output "DONE"
