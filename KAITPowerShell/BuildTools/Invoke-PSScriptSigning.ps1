#Change the directory to the location we're running the script from so we can use relative paths	
Set-Location -LiteralPath ([System.IO.Directory]::GetParent($MyInvocation.MyCommand.Path))

# We need this dll to check stuff back into TFS
[System.Reflection.Assembly]::LoadFile([System.IO.Path]::GetFullPath('.\Microsoft.TeamFoundation.Build.Controls.dll'))

#Region Functions

	Function New-SigningCert {
	<#
	.Synopsis
		Creates self-signed signing certificate and install it to certificate store
	.Description
		This function generates self-signed certificate with some pre-defined and
		user-definable settings. User may elect to perform complete certificate
		installation, by installing generated certificate to Trusted Root Certification
		Authorities and Trusted Publishers containers in *current user* store.
		
	.Parameter Subject
		Specifies subject for certificate. This parameter must be entered in X500
		Distinguished Name format. Default is: CN=PowerShell User, OU=Test Signing Cert.

	.Parameter KeyLength
		Sp'ecifies private key length. Due of performance and security reasons, only
		1024 and 2048 bit are supported. by default 1024 bit key length is used.

	.Parameter NotBefore
		Sets the date in local time on which a certificate becomes valid. By default
		current date and time is used.

	.Parameter NotAfter
		Sets the date in local time after which a certificate is no longer valid. By
		default certificate is valid for 365 days.

	.Parameter Force
		If Force switch is asserted, script will prepare certificate for use by adding
		it to Trusted Root Certification Authorities and Trusted Publishers containers
		in current user certificate store. During certificate installation you will be
		prompted to confirm if you want to add self-signed certificate to Trusted Root
		Certification Authorities container.
	#>
	[CmdletBinding()]
		param (
			[string]$Subject = "CN=PowerShell User, OU=Test Signing Cert",
			[int][ValidateSet("1024", "2048")]$KeyLength = 1024,
			[datetime]$NotBefore = [DateTime]::Now,
			[datetime]$NotAfter = $NotBefore.AddDays(365),
			[switch]$Force
		)
		
		$OS = (Get-WmiObject Win32_OperatingSystem).Version
		if ($OS[0] -lt 6) {
			Write-Warning "Windows XP, Windows Server 2003 and Windows Server 2003 R2 are not supported!"
			return
		}
		# while all certificate fields MUST be encoded in ASN.1 DER format
		# we will use CryptoAPI COM interfaces to generate and encode all necessary
		# extensions.
		
		# create Subject field in X.500 format using the following interface:
		# http://msdn.microsoft.com/en-us/library/aa377051(VS.85).aspx
		$SubjectDN = New-Object -ComObject X509Enrollment.CX500DistinguishedName
		$SubjectDN.Encode($Subject, 0x0)
		
		# define CodeSigning enhanced key usage (actual OID = 1.3.6.1.5.5.7.3.3) from OID
		# http://msdn.microsoft.com/en-us/library/aa376784(VS.85).aspx
		$OID = New-Object -ComObject X509Enrollment.CObjectID
		$OID.InitializeFromValue("1.3.6.1.5.5.7.3.3")
		# while IX509ExtensionEnhancedKeyUsage accept only IObjectID collection
		# (to support multiple EKUs) we need to create IObjectIDs object and add our
		# IObjectID object to the collection:
		# http://msdn.microsoft.com/en-us/library/aa376785(VS.85).aspx
		$OIDs = New-Object -ComObject X509Enrollment.CObjectIDs
		$OIDs.Add($OID)
		
		# now we create Enhanced Key Usage extension, add our OID and encode extension value
		# http://msdn.microsoft.com/en-us/library/aa378132(VS.85).aspx
		$EKU = New-Object -ComObject X509Enrollment.CX509ExtensionEnhancedKeyUsage
		$EKU.InitializeEncode($OIDs)
		
		# generate Private key as follows:
		# http://msdn.microsoft.com/en-us/library/aa378921(VS.85).aspx
		$PrivateKey = New-Object -ComObject X509Enrollment.CX509PrivateKey
		$PrivateKey.ProviderName = "Microsoft Base Cryptographic Provider v1.0"
		# private key is supposed for signature: http://msdn.microsoft.com/en-us/library/aa379409(VS.85).aspx
		$PrivateKey.KeySpec = 0x2
		$PrivateKey.Length = $KeyLength
		# key will be stored in current user certificate store
		$PrivateKey.MachineContext = 0x0
		$PrivateKey.Create()
		
		# now we need to create certificate request template using the following interface:
		# http://msdn.microsoft.com/en-us/library/aa377124(VS.85).aspx
		$Cert = New-Object -ComObject X509Enrollment.CX509CertificateRequestCertificate
		$Cert.InitializeFromPrivateKey(0x1,$PrivateKey,"")
		$Cert.Subject = $SubjectDN
		$Cert.Issuer = $Cert.Subject
		$Cert.NotBefore = $NotBefore
		$Cert.NotAfter = $NotAfter
		$Cert.X509Extensions.Add($EKU)
		# completing certificate request template building
		$Cert.Encode()
		
		# now we need to process request and build end certificate using the following
		# interface: http://msdn.microsoft.com/en-us/library/aa377809(VS.85).aspx
		
		$Request = New-Object -ComObject X509Enrollment.CX509enrollment
		# process request
		$Request.InitializeFromRequest($Cert)
		# retrievecertificate encoded in Base64.
		$endCert = $Request.CreateRequest(0x1)
		# install certificate to user store
		$Request.InstallResponse(0x2,$endCert,0x1,"")
		
		if ($Force) {
			# convert Bas64 string to a byte array
		 	[Byte[]]$bytes = [System.Convert]::FromBase64String($endCert)
			foreach ($Container in "Root", "TrustedPublisher") {
				# open Trusted Root CAs and TrustedPublishers containers and add
				# certificate
				$x509store = New-Object Security.Cryptography.X509Certificates.X509Store $Container, "CurrentUser"
				$x509store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
				$x509store.Add([Security.Cryptography.X509Certificates.X509Certificate2]$bytes)
				# close store when operation is completed
				$x509store.Close()
			}
		}
	}
	
#EndRegion Functions

$subject = "CN=PowerShell User, OU=Script Signing Cert"
New-SigningCert -Subject $subject

$signingCert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | ?{$_.Subject -eq $Subject} | Select -First 1

if($signingCert -eq $null)
{
	"Failed to find the code signing certificate under CurrentUser\My. Did it get created?" | Out-Host
}
else
{
	#Sign all scripts execpt for the script signing script
	#foreach($psScript in (Get-ChildItem -Path '..' -Filter '*.ps1'  -Exclude ([String]::Concat('*',($Myinvocation.MyCommand.Name))) -Recurse:$Recurse))
	foreach($psScript in (Get-ChildItem -Path '..' -Filter '*.ps1' -Recurse:$Recurse))
	{
		$scriptName = [System.IO.Path]::GetFileName($psScript.FullName)
		"Checking out PowerShell Script at '{0}'" -f $psScript.FullName | Out-Host
		.\tf.exe checkout $psScript.FullName

		"Signing script: '{0}'" -f $scriptName  | Out-Host
		Set-AuthenticodeSignature -LiteralPath $psScript.FullName -Certificate $signingCert -Force

		"Checking Script in" | Out-Host
		.\tf.exe checkin $psScript.FullName /comment:"Applying digital signature to script" /noprompt
	}
}


# SIG # Begin signature block
# MIIEQQYJKoZIhvcNAQcCoIIEMjCCBC4CAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUdPfeWteJvjunVig6l8n/SHP0
# N5+gggI/MIICOzCCAaSgAwIBAgIQdCuk843KELhDbNOOgnfHATANBgkqhkiG9w0B
# AQUFADA4MRwwGgYDVQQLDBNTY3JpcHQgU2lnbmluZyBDZXJ0MRgwFgYDVQQDDA9Q
# b3dlclNoZWxsIFVzZXIwHhcNMTUwNDIyMTMzNzMyWhcNMTYwNDIxMTMzNzMyWjA4
# MRwwGgYDVQQLDBNTY3JpcHQgU2lnbmluZyBDZXJ0MRgwFgYDVQQDDA9Qb3dlclNo
# ZWxsIFVzZXIwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBANQNVCChCUkLDXTH
# TNyUmlkPgVgaZaUE7X0Be1RHsB6yIft/UAeU15/oVXM/dY9ct9fqk3eJhbH088Bd
# I04l4GUKxvuKFimDbSX8UWZUs46q7f7tpwycjtnnQqY6qwqWhiY3ehfGxeBcV91V
# eY46c53DR89sqn3DwlEWPo+uF1aDAgMBAAGjRjBEMBMGA1UdJQQMMAoGCCsGAQUF
# BwMDMB0GA1UdDgQWBBRfprzZc1esnwc69wBbJFGEooapZDAOBgNVHQ8BAf8EBAMC
# B4AwDQYJKoZIhvcNAQEFBQADgYEAtcrAox42gQPxLUy1vP5VXuQcXwXL035hoTpx
# RRLIQYCpC+gz3+20T7v0nbpq93LvDG2fj+XQCY+7TqJIOhdQZRMQoeFXX7NlhK/N
# pXw0B6zkkQfv9Neqjhijjc8TleeHso87kl+Zxde4DAATTvquEPQb0PtU3i7F+JuK
# NV4SJoUxggFsMIIBaAIBATBMMDgxHDAaBgNVBAsME1NjcmlwdCBTaWduaW5nIENl
# cnQxGDAWBgNVBAMMD1Bvd2VyU2hlbGwgVXNlcgIQdCuk843KELhDbNOOgnfHATAJ
# BgUrDgMCGgUAoHgwGAYKKwYBBAGCNwIBDDEKMAigAoAAoQKAADAZBgkqhkiG9w0B
# CQMxDAYKKwYBBAGCNwIBBDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAj
# BgkqhkiG9w0BCQQxFgQUttS/8bqUtg4SpjoPTCWrsuFvy2QwDQYJKoZIhvcNAQEB
# BQAEgYDIvrjnihKlN9zni0ajE10u5DRLmffI6/BzibkfkqbaSDXZ62fcbWRPWKXf
# Hxeep3g8FnTlxXo2P5wj/r+ZJ7ThiShefmdeYMcLmJbTnXT9oikVA3c6mJk6CtD0
# XAwoXHMnUQ+RRE1mnoiJ0UHe3VKJ5MZrwLY3AeqMeXveLTP4kA==
# SIG # End signature block
