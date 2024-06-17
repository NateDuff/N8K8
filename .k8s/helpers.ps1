## Inspiration: [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String((kubectl get secret n8-api-secrets -o jsonpath='{.data}' | ConvertFrom-Json).ConnectionStrings__tables))

function Get-K8sSecret {
    param(
        [string]$secretName
    )

    $secret = kubectl get secret $secretName -o jsonpath='{.data}' | ConvertFrom-Json
    $secret.data.GetEnumerator() | ForEach-Object {
        $key = $_.Key
        $value = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_.Value))
        Write-Output "$key=$value"
    }
}