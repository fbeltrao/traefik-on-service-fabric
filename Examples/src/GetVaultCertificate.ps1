$VaultName = "<vault name>"
$CertificateName = "<vault certificate name>"
$SubscriptionId = "<your subscription id>"

# Login
Login-AzureRmAccount 

Set-AzureRmContext -SubscriptionId $SubscriptionId

#get Secret object (Containing private key) from Key Vault
$AzureKeyVaultSecret=Get-AzureKeyVaultSecret -VaultName $VaultName -Name $CertificateName

#Convert private cert to bytes
$PrivateCertKVBytes = [System.Convert]::FromBase64String($AzureKeyVaultSecret.SecretValueText)

#Convert Bytes to Certificate (flagged as exportable & retaining private key)
#possible flags: https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509keystorageflags(v=vs.110).aspx
$certObject = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -argumentlist $PrivateCertKVBytes, $null, "Exportable, PersistKeySet"


#Optional: import certificate to current user Certificate store
$Certificatestore = New-Object System.Security.Cryptography.X509Certificates.X509Store -argumentlist "My","Currentuser"
$Certificatestore.open("readWrite")
$Certificatestore.Add($certObject)
$Certificatestore.Close()

#if private certificate needs to be exported, then it needs a password - create Temporary Random Password for certificate
$PasswordLength=20
$ascii = 33..126 | %{[char][byte]$_}
$CertificatePfxPassword = $(0..$passwordLength | %{$ascii | get-random}) -join ""

#Encrypt private Certificate using password (required if exporting to file or memory for use in ARM template)
$protectedCertificateBytes = $certObject.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12,
$CertificatePfxPassword)
Write-output "Private Certificate Password: '$CertificatePfxPassword'"

#Optional: Export encrypted certificate to Base 64 String in memory (for use in ARM templates / other):
$InternetPfxCertdata = [System.Convert]::ToBase64String($protectedCertificateBytes)

#Optional: Export encrypted certificate to file on desktop:
$pfxPath = '{0}\{1}.pfx' -f [Environment]::GetFolderPath("Desktop") ,$CertificateName
[System.IO.File]::WriteAllBytes($pfxPath, $protectedCertificateBytes)