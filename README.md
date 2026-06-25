# DBS Logs

Visualizador interno, somente leitura, para consulta dos logs funcionais da aplicação Credibox.

## Funcionalidades

- listagem paginada de até 100 registos;
- paginação por `DetailId`, sem `OFFSET`;
- filtros por tipo, utilizador e proposta/operação;
- opção para apresentar apenas erros funcionais;
- detalhe completo dos eventos associados ao mesmo `LogId`;
- carregamento de `DetailData` apenas quando o utilizador abre o detalhe;
- interface ASP.NET Web Forms executada por compilação dinâmica no IIS;
- codificação UTF-8 e cultura `pt-PT`.

## Arquitetura

O projeto utiliza o modelo **ASP.NET Web Site**. Não é necessário compilar manualmente antes da publicação. O IIS/ASP.NET compila os ficheiros `.aspx`, os respetivos `CodeFile` e as classes de `App_Code` na primeira execução.

Ambiente validado:

- Website IIS: `DBSLogs`;
- Application Pool: `DBSLogsPool`;
- .NET Framework: 4.x;
- caminho físico: `C:\inetpub\DBSLogs`;
- binding inicial: `http://localhost:8081/`;
- base funcional: `SOL_Credibox_PRD`;
- base da plataforma: `DBSPlatform`.

## Instalação automática no IIS

### 1. Obter o código

Abra o PowerShell como **Administrador**:

```powershell
git clone https://github.com/insane-tsunami/DBS-Logs.git
cd DBS-Logs
```

Quando o repositório já existir:

```powershell
cd C:\caminho\DBS-Logs
git pull
```

### 2. Permitir a execução do script na sessão atual

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

### 3. Executar a instalação padrão

```powershell
.\installToIIS.ps1
```

A configuração padrão cria ou atualiza:

| Item | Valor padrão |
|---|---|
| Website | `DBSLogs` |
| Application Pool | `DBSLogsPool` |
| Caminho físico | `C:\inetpub\DBSLogs` |
| Porta HTTP | `8081` |
| SQL Server | `.` |
| Base funcional | `SOL_Credibox_PRD` |
| Autenticação SQL | Integrated Security |

O script:

1. valida ou instala os componentes necessários do IIS;
2. cria a pasta de publicação;
3. copia os ficheiros da aplicação;
4. gera o `web.config` com UTF-8;
5. cria/configura o Application Pool em .NET 4.x e modo Integrated;
6. atribui permissões de leitura à identidade do pool;
7. cria um Website IIS independente do `Default Web Site`;
8. inicia o pool e o site.

O site fica disponível em:

```text
http://localhost:8081/
```

## Parâmetros do instalador

### Instância SQL diferente

```powershell
.\installToIIS.ps1 `
    -SqlServer "SERVIDOR\INSTANCIA" `
    -DatabaseName "SOL_Credibox_PRD"
```

### Porta ou caminho diferentes

```powershell
.\installToIIS.ps1 `
    -Port 8090 `
    -PhysicalPath "D:\Sites\DBSLogs"
```

### Autenticação SQL

```powershell
$password = Read-Host "Password SQL" -AsSecureString

.\installToIIS.ps1 `
    -SqlServer "SERVIDOR\INSTANCIA" `
    -DatabaseName "SOL_Credibox_PRD" `
    -SqlUser "dbslogs_reader" `
    -SqlPassword $password
```

### Atualização sem reinstalar componentes do Windows

```powershell
.\installToIIS.ps1 -SkipWindowsFeatures
```

### Preservar ficheiros existentes na pasta de publicação

```powershell
.\installToIIS.ps1 -KeepExistingFiles
```

Por padrão, o instalador limpa a pasta de destino antes de copiar a nova versão.

## Permissões no SQL Server

Com `Integrated Security`, a aplicação liga-se ao SQL Server usando:

```text
IIS APPPOOL\DBSLogsPool
```

Para um SQL Server local, conceda somente leitura nas duas bases:

```sql
USE [master];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals
    WHERE name = N'IIS APPPOOL\DBSLogsPool'
)
BEGIN
    CREATE LOGIN [IIS APPPOOL\DBSLogsPool]
    FROM WINDOWS;
END
GO

USE [SOL_Credibox_PRD];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = N'IIS APPPOOL\DBSLogsPool'
)
BEGIN
    CREATE USER [IIS APPPOOL\DBSLogsPool]
    FOR LOGIN [IIS APPPOOL\DBSLogsPool];
END
GO

ALTER ROLE [db_datareader]
ADD MEMBER [IIS APPPOOL\DBSLogsPool];
GO

USE [DBSPlatform];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = N'IIS APPPOOL\DBSLogsPool'
)
BEGIN
    CREATE USER [IIS APPPOOL\DBSLogsPool]
    FOR LOGIN [IIS APPPOOL\DBSLogsPool];
END
GO

ALTER ROLE [db_datareader]
ADD MEMBER [IIS APPPOOL\DBSLogsPool];
GO
```

Para um SQL Server remoto, a identidade local do Application Pool normalmente não pode ser autenticada no outro servidor. Nesse cenário, utilize uma destas opções:

- conta de domínio dedicada no Application Pool;
- autenticação SQL com um utilizador somente leitura.

## Atualização da aplicação

No servidor:

```powershell
cd C:\caminho\DBS-Logs
git pull

.\installToIIS.ps1 -SkipWindowsFeatures
```

Não é necessário executar `iisreset`. O instalador atualiza o site e inicia o pool quando necessário.

## Diagnóstico

### Verificar estado do site e do pool

```powershell
Import-Module WebAdministration

Get-Website -Name "DBSLogs"
Get-WebAppPoolState -Name "DBSLogsPool"
```

### Reiniciar apenas o Application Pool

```powershell
Import-Module WebAdministration

Stop-WebAppPool -Name "DBSLogsPool"
Start-Sleep -Seconds 2
Start-WebAppPool -Name "DBSLogsPool"
```

### Acentuação incorreta

O `web.config` gerado pelo instalador contém:

```xml
<globalization
    requestEncoding="utf-8"
    responseEncoding="utf-8"
    fileEncoding="utf-8"
    culture="pt-PT"
    uiCulture="pt-PT" />
```

Depois de atualizar a aplicação, faça `Ctrl + F5` no navegador para evitar conteúdo em cache.

## Segurança

- A aplicação é somente leitura.
- As consultas SQL utilizam parâmetros.
- Não devem ser gravadas passwords, connection strings de produção ou outros segredos no repositório.
- Para produção, utilize um utilizador SQL ou uma identidade de serviço com acesso mínimo necessário.
- O Website deve permanecer separado do `Default Web Site` para não herdar módulos e configurações do OutSystems.
