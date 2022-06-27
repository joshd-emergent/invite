##run as admin

$thumbprint = (New-SelfSignedCertificate `
-Subject "CN=Azure B2C Local Cert Dev" `
-Type SSLServerAuthentication `
-FriendlyName "Azure B2C Local Cert Dev").Thumbprint

$certificateFilePath = "C:\Code\B2C\invite\$thumbprint.pfx"

$mypwd = ConvertTo-SecureString -String "xx" -Force -AsPlainText

Export-PfxCertificate `
    -cert cert:\LocalMachine\MY\$thumbprint `
    -FilePath "$certificateFilePath" `
    -Password $mypwd #(Read-Host -Prompt "Enter password that would protect the certificate" -AsSecureString)


