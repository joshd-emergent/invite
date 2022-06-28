##run as admin

$thumbprint = (New-SelfSignedCertificate `
        -Subject "CN=Azure B2C Local Cert Dev" `
        -Type SSLServerAuthentication `
        -KeyUsage DigitalSignature -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(2) `
        -FriendlyName "Azure B2C Local Cert Dev").Thumbprint

$certificateFilePath = "C:\Code\B2C\invite\$thumbprint.pfx"

$mypwd = ConvertTo-SecureString -String "xxx" -Force -AsPlainText

Export-PfxCertificate `
    -cert cert:\LocalMachine\MY\$thumbprint `
    -FilePath "$certificateFilePath" `
    -Password $mypwd #(Read-Host -Prompt "Enter password that would protect the certificate" -AsSecureString)

