<#
.SYNOPSIS
    Initialise the S3 backend for a Block_Ticket Terraform environment.

.DESCRIPTION
    Discovers the S3 state bucket and DynamoDB lock table created by the
    `bootstrap` module and writes the matching `terraform init -backend-config`
    credentials into the chosen environment.

.PARAMETER Environment
    One of: dev, staging, prod.

.EXAMPLE
    PS> .\scripts\init-backend.ps1 dev
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$bootstrapDir = Resolve-Path (Join-Path $scriptDir '..\bootstrap')

if (-not (Test-Path $bootstrapDir)) {
    Write-Error "Bootstrap module not found at $bootstrapDir"
}

Push-Location $bootstrapDir
try {
    $stateBucket = (terraform output -raw state_bucket).Trim()
    $lockTable   = (terraform output -raw lock_table).Trim()
}
finally {
    Pop-Location
}

if (-not $stateBucket -or -not $lockTable) {
    Write-Error "Bootstrap state is empty. Run 'terraform init && terraform apply' in $bootstrapDir first."
}

$region = $env:AWS_REGION
if (-not $region) { $region = 'ap-southeast-1' }

$stateKey = "envs/$Environment/terraform.tfstate"

$envDir = Resolve-Path (Join-Path $scriptDir "..\environments\$Environment")

Write-Host "==> Initialising $Environment backend"
Write-Host "    bucket       = $stateBucket"
Write-Host "    key          = $stateKey"
Write-Host "    region       = $region"
Write-Host "    lock_table   = $lockTable"
Write-Host ""

Push-Location $envDir
try {
    terraform init `
        -backend-config="bucket=$stateBucket" `
        -backend-config="key=$stateKey" `
        -backend-config="region=$region" `
        -backend-config="dynamodb_table=$lockTable" `
        -backend-config="encrypt=true"
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "==> Backend initialised. Next:"
Write-Host "    terraform validate"
Write-Host "    terraform plan"
Write-Host "    terraform apply   # gated by the $Environment GitHub Environment in CI"
