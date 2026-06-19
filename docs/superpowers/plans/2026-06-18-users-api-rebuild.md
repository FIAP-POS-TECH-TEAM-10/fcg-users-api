# UsersAPI Rebuild — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recriar o fcg-users-api do zero seguindo o template CatalogAPI, corrigindo todos os bugs encontrados no diagnóstico e alinhando ao padrão da equipe.

**Architecture:** Clean Architecture com 5 projetos (Domain → Application → Infra → CrossCutting → Api). O CrossCutting é o composition root que conecta as camadas. MassTransit publica `UserCreatedEvent` após cadastro; falha do broker não bloqueia o cadastro.

**Tech Stack:** .NET 10, EF Core 10 (SQLite), MediatR 14, FluentValidation 12, Serilog 4, MassTransit 8.4, JWT Bearer, Scalar, FCGames.IntegrationEvents (GitHub Packages)

**Working directory:** `C:\GIT\FIAP\FIAP-POS-TECH-TEAM-10\fcg-users-api\`

**Novo código vai em:** `app/src/` — os projetos antigos na raiz do repo ficam intactos até você confirmar remoção.

---

## Estrutura de Arquivos Alvo

```
fcg-users-api/
├── nuget.config                          ← GitHub Packages auth
├── Dockerfile                            ← multi-stage SDK → ASPNet
└── app/src/
    ├── Fiap.FCGames.Users.sln
    ├── Fiap.FCGames.Users.Domain/
    │   ├── Fiap.FCGames.Users.Domain.csproj
    │   ├── Aggregates/Usuario.cs
    │   ├── Aggregates/UsuarioId.cs
    │   ├── Aggregates/TipoAcesso.cs
    │   ├── Constants/AuthConstants.cs
    │   ├── Exceptions/BusinessException.cs
    │   ├── Exceptions/LoginException.cs
    │   ├── Exceptions/NotFoundException.cs
    │   ├── Interfaces/IGenericRepository.cs
    │   ├── Interfaces/IUsuarioRepository.cs
    │   ├── Interfaces/IUnitOfWork.cs
    │   ├── Services/IPasswordHasherService.cs
    │   └── Services/ITokenService.cs
    ├── Fiap.FCGames.Users.Application/
    │   ├── Fiap.FCGames.Users.Application.csproj
    │   ├── IAssemblyMarker.cs
    │   ├── Behaviors/ValidatorBehaviors.cs
    │   ├── Commands/CriarUsuario/CriarUsuarioCommand.cs
    │   ├── Commands/CriarUsuario/CriarUsuarioCommandHandler.cs
    │   ├── Commands/CriarUsuario/CriarUsuarioCommandValidator.cs
    │   ├── Commands/CriarUsuario/CriarUsuarioResponse.cs
    │   ├── Commands/AtualizarUsuario/AtualizarUsuarioCommand.cs
    │   ├── Commands/AtualizarUsuario/AtualizarUsuarioCommandHandler.cs
    │   ├── Commands/AtualizarUsuario/AtualizarUsuarioCommandValidator.cs
    │   ├── Commands/AtualizarUsuario/AtualizarUsuarioResponse.cs
    │   ├── Commands/Login/LoginCommand.cs
    │   ├── Commands/Login/LoginCommandHandler.cs
    │   ├── Commands/Login/LoginCommandValidator.cs
    │   ├── Commands/Login/UsuarioLogadoDto.cs
    │   ├── Queries/BuscarUsuarioPorId/BuscarUsuarioPorIdQuery.cs
    │   ├── Queries/BuscarUsuarioPorId/BuscarUsuarioPorIdQueryHandler.cs
    │   ├── Queries/BuscarUsuarioPorId/DetalhesUsuarioDto.cs
    │   ├── Queries/ListarUsuarios/ListarUsuariosQuery.cs
    │   ├── Queries/ListarUsuarios/ListarUsuariosQueryHandler.cs
    │   └── Queries/ListarUsuarios/ListaUsuariosDto.cs
    ├── Fiap.FCGames.Users.Infra/
    │   ├── Fiap.FCGames.Users.Infra.csproj
    │   └── DataProvider/
    │       ├── Contexto/FcGamesContexto.cs
    │       ├── Factory/FcGamesContextoFactory.cs
    │       ├── EntityConfigurations/UsuarioConfiguration.cs
    │       ├── Repositories/Shared/GenericRepository.cs
    │       ├── Repositories/UsuarioRepository.cs
    │       ├── UnitOfWork/UnitOfWork.cs
    │       ├── Services/PasswordHasherService.cs
    │       └── Services/TokenService.cs
    ├── Fiap.FCGames.Users.CrossCutting/
    │   ├── Fiap.FCGames.Users.CrossCutting.csproj
    │   ├── Extensions/RegisterDependencyInjectionExtensions.cs
    │   ├── Extensions/JwtAutenticacaoExtensions.cs
    │   ├── Extensions/JwtAutorizacaoExtensions.cs
    │   ├── Extensions/SwaggerExtensions.cs
    │   ├── Extensions/MediatRExtensions.cs
    │   ├── Extensions/RegisterContextDatabaseExtensions.cs
    │   ├── Extensions/MassTransitExtensions.cs
    │   ├── Middleware/ErrorHandlingMiddleware.cs
    │   ├── Middleware/ErrorHandlingMiddlewareExtensions.cs
    │   └── Middleware/RegisterUsoCorrelationMiddleware.cs
    └── Fiap.FCGames.Users.Api/
        ├── Fiap.FCGames.Users.Api.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── Properties/launchSettings.json
        ├── Controllers/Shared/ApiControllerBase.cs
        └── Controllers/UsuariosController.cs
```

---

## Decisões de Design (registradas aqui para referência)

| Decisão | Escolha | Motivo |
|---|---|---|
| Login por | Email + Senha | Alinha com schema do CLAUDE.md; sem `NomeUsuario` |
| TipoAcesso enum | `Standard=0, Admin=1` | Segue CLAUDE.md |
| JWT Sub claim | email do usuário | Identificador legível |
| JWT NameIdentifier claim | `UserId.Value.ToString()` | CatalogAPI extrai UserId aqui |
| JWT Role claim | `tipoAcesso.ToString()` → `"Admin"` / `"Standard"` | Bate com as policies |
| Rotas | `/usuarios`, `/usuarios/login` (sem `/api/`) | Segue CLAUDE.md |
| Schema tabela | `usuarios` (plural) | Segue CLAUDE.md |
| Seed Admin | Seeder no startup | Garante usuário Admin para demo |

---

## Task 1: Scaffold — solução, projetos e referências

**Files:**
- Create: `app/src/Fiap.FCGames.Users.sln` (via dotnet CLI)
- Create: os 5 projetos `.csproj`

- [ ] **Step 1.1: Criar estrutura de diretórios e projetos**

Execute a partir de `C:\GIT\FIAP\FIAP-POS-TECH-TEAM-10\fcg-users-api\`:

```bash
mkdir -p app/src
cd app/src
dotnet new classlib -n Fiap.FCGames.Users.Domain -f net10.0 --no-update-check
dotnet new classlib -n Fiap.FCGames.Users.Application -f net10.0 --no-update-check
dotnet new classlib -n Fiap.FCGames.Users.Infra -f net10.0 --no-update-check
dotnet new classlib -n Fiap.FCGames.Users.CrossCutting -f net10.0 --no-update-check
dotnet new webapi --use-controllers -n Fiap.FCGames.Users.Api -f net10.0 --no-update-check
dotnet new sln -n Fiap.FCGames.Users
dotnet sln Fiap.FCGames.Users.sln add \
  Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj \
  Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj \
  Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj \
  Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj \
  Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj
```

- [ ] **Step 1.2: Remover arquivos gerados automaticamente que serão substituídos**

```bash
# Dentro de app/src/
rm -f Fiap.FCGames.Users.Api/Controllers/WeatherForecastController.cs
rm -f Fiap.FCGames.Users.Api/WeatherForecast.cs
rm -f Fiap.FCGames.Users.Domain/Class1.cs
rm -f Fiap.FCGames.Users.Application/Class1.cs
rm -f Fiap.FCGames.Users.Infra/Class1.cs
rm -f Fiap.FCGames.Users.CrossCutting/Class1.cs
```

- [ ] **Step 1.3: Adicionar referências entre projetos**

```bash
# Application depende de Domain
dotnet add Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj \
  reference Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj

# Infra depende de Domain + Application (para IAssemblyMarker não — mas de interfaces sim)
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj \
  reference Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj

# CrossCutting depende de Domain + Application + Infra
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj \
  reference Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj \
  reference Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj \
  reference Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj

# Api depende de Application + CrossCutting + Domain
dotnet add Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj \
  reference Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj
dotnet add Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj \
  reference Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj
dotnet add Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj \
  reference Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj
```

- [ ] **Step 1.4: Adicionar pacotes NuGet**

```bash
# Domain — sem dependências externas (clean domain)
# (nada a instalar)

# Application
dotnet add Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj package MediatR --version 14.1.0
dotnet add Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj package FluentValidation --version 12.1.1
dotnet add Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 12.1.1

# Infra
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj package Microsoft.IdentityModel.Tokens --version 8.17.0
dotnet add Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj package System.IdentityModel.Tokens.Jwt --version 8.17.0

# CrossCutting
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package MediatR --version 14.1.0
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package FluentValidation --version 12.1.1
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package FluentValidation.DependencyInjectionExtensions --version 12.1.1
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package Serilog --version 4.3.1
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package Serilog.AspNetCore --version 10.0.0
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package Swashbuckle.AspNetCore --version 10.1.7
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package Scalar.AspNetCore --version 2.14.5
dotnet add Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj package MassTransit.RabbitMQ --version 8.4.1

# Api
dotnet add Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj package Microsoft.AspNetCore.OpenApi --version 10.0.0
dotnet add Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.0
```

- [ ] **Step 1.5: Verificar que o build básico funciona (vai falhar por código faltando, mas não deve ter erro de csproj)**

```bash
dotnet build Fiap.FCGames.Users.sln
```
Expected: erros de "type not found" dos classlib vazios — normal neste ponto. Não deve ter erros de referência circular ou pacote faltando.

- [ ] **Step 1.6: Commit**

```bash
cd ../..  # volta para fcg-users-api/
git add app/
git commit -m "chore: scaffold nova solução Users API no template CatalogAPI"
```

---

## Task 2: Domain Layer

**Files:**
- Create: `app/src/Fiap.FCGames.Users.Domain/` — todos os arquivos abaixo

- [ ] **Step 2.1: Criar entidades e value objects**

`app/src/Fiap.FCGames.Users.Domain/Aggregates/UsuarioId.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Aggregates;

public record struct UsuarioId(Guid Value)
{
    public static UsuarioId New() => new(Guid.NewGuid());
    public override readonly string ToString() => Value.ToString();
}
```

`app/src/Fiap.FCGames.Users.Domain/Aggregates/TipoAcesso.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Aggregates;

public enum TipoAcesso
{
    Standard = 0,
    Admin = 1
}
```

`app/src/Fiap.FCGames.Users.Domain/Aggregates/Usuario.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Aggregates;

public class Usuario
{
    public UsuarioId Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public required string SenhaHash { get; set; }
    public int IdTipoAcesso { get; set; }
    public TipoAcesso TipoAcesso
    {
        get => (TipoAcesso)IdTipoAcesso;
        set => IdTipoAcesso = (int)value;
    }
    public DateTime CriadoEm { get; set; }
}
```

- [ ] **Step 2.2: Criar constantes**

`app/src/Fiap.FCGames.Users.Domain/Constants/AuthConstants.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Constants;

public static class AuthConstants
{
    public const string AdminPolicy = "Admin";
    public const string StandardPolicy = "Standard";
}
```

- [ ] **Step 2.3: Criar exceções**

`app/src/Fiap.FCGames.Users.Domain/Exceptions/BusinessException.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
```

`app/src/Fiap.FCGames.Users.Domain/Exceptions/LoginException.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Exceptions;

public class LoginException : Exception
{
    public int StatusCode { get; }
    public LoginException(string message, int statusCode) : base(message)
        => StatusCode = statusCode;
}
```

`app/src/Fiap.FCGames.Users.Domain/Exceptions/NotFoundException.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

- [ ] **Step 2.4: Criar interfaces do domínio**

`app/src/Fiap.FCGames.Users.Domain/Interfaces/IGenericRepository.cs`:
```csharp
using System.Linq.Expressions;

namespace Fiap.FCGames.Users.Domain.Interfaces;

public interface IGenericRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetAll();
    IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate);
    TEntity? Get(params object[] key);
    void Create(TEntity entity);
    void Update(TEntity entity);
    void Delete(Func<TEntity, bool> predicate);
    void Dispose();
}
```

`app/src/Fiap.FCGames.Users.Domain/Interfaces/IUsuarioRepository.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Domain.Interfaces;

public interface IUsuarioRepository
{
    void Adicionar(Usuario usuario);
    void Atualizar(Usuario usuario);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<IEnumerable<Usuario>> ObterTodosAsync();
    Task<Usuario?> ObterPorIdAsync(UsuarioId id);
    Task<bool> ExisteEmailAsync(string email);
}
```

`app/src/Fiap.FCGames.Users.Domain/Interfaces/IUnitOfWork.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Interfaces;

public interface IUnitOfWork
{
    IUsuarioRepository UsuarioRepository { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2.5: Criar interfaces de serviços**

`app/src/Fiap.FCGames.Users.Domain/Services/IPasswordHasherService.cs`:
```csharp
namespace Fiap.FCGames.Users.Domain.Services;

public interface IPasswordHasherService
{
    string GerarHash(string senha);
    bool Verificar(string senhaTexto, string senhaHash);
}
```

`app/src/Fiap.FCGames.Users.Domain/Services/ITokenService.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Domain.Services;

public interface ITokenService
{
    string GerarToken(Guid userId, string email, TipoAcesso tipoAcesso, DateTime expiracao);
}
```

- [ ] **Step 2.6: Build do Domain para verificar**

```bash
dotnet build Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 2.7: Commit**

```bash
git add app/src/Fiap.FCGames.Users.Domain/
git commit -m "feat(domain): entidades, interfaces e exceções do Users Domain"
```

---

## Task 3: Infrastructure Layer

**Files:**
- Create: `app/src/Fiap.FCGames.Users.Infra/` — todos os arquivos abaixo

- [ ] **Step 3.1: Criar DbContext**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Contexto/FcGamesContexto.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Infra.DataProvider.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Fiap.FCGames.Users.Infra.DataProvider.Contexto;

public class FcGamesContexto : DbContext
{
    public FcGamesContexto(DbContextOptions<FcGamesContexto> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
    }
}
```

- [ ] **Step 3.2: Criar factory para migrations**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Factory/FcGamesContextoFactory.cs`:
```csharp
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fiap.FCGames.Users.Infra.DataProvider.Factory;

public class FcGamesContextoFactory : IDesignTimeDbContextFactory<FcGamesContexto>
{
    public FcGamesContexto CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FcGamesContexto>();
        optionsBuilder.UseSqlite("Data Source=fcgames.db");
        return new FcGamesContexto(optionsBuilder.Options);
    }
}
```

- [ ] **Step 3.3: Criar configuração EF do Usuario**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/EntityConfigurations/UsuarioConfiguration.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fiap.FCGames.Users.Infra.DataProvider.EntityConfigurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(e => e.Id).HasName("PK_usuarios");

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new UsuarioId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Nome)
            .HasColumnName("nome")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.SenhaHash)
            .HasColumnName("senha_hash")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(e => e.IdTipoAcesso)
            .HasColumnName("tipo_acesso")
            .IsRequired();

        builder.Ignore(e => e.TipoAcesso);
    }
}
```

- [ ] **Step 3.4: Criar repositório genérico**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Repositories/Shared/GenericRepository.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Fiap.FCGames.Users.Infra.DataProvider.Repositories.Shared;

public abstract class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class
{
    private readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> GetAll() => _dbSet;
    public IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate) => _dbSet.Where(predicate);
    public TEntity? Get(params object[] key) => _dbSet.Find(key);
    public void Create(TEntity entity) => _dbSet.Add(entity);
    public void Update(TEntity entity) => _context.Entry(entity).State = EntityState.Modified;
    public void Delete(Func<TEntity, bool> predicate)
        => _dbSet.Where(predicate).ToList().ForEach(e => _dbSet.Remove(e));

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

- [ ] **Step 3.5: Criar repositório de Usuário**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Repositories/UsuarioRepository.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;
using Fiap.FCGames.Users.Infra.DataProvider.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Fiap.FCGames.Users.Infra.DataProvider.Repositories;

public class UsuarioRepository : GenericRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(FcGamesContexto context) : base(context) { }

    public void Adicionar(Usuario usuario) => Create(usuario);
    public void Atualizar(Usuario usuario) => Update(usuario);

    public async Task<IEnumerable<Usuario>> ObterTodosAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    public async Task<Usuario?> ObterPorEmailAsync(string email)
        => await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());

    public async Task<Usuario?> ObterPorIdAsync(UsuarioId id)
        => await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> ExisteEmailAsync(string email)
        => _dbSet.AsNoTracking().AnyAsync(x => x.Email.ToLower() == email.ToLower());
}
```

- [ ] **Step 3.6: Criar UnitOfWork**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/UnitOfWork/UnitOfWork.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;

namespace Fiap.FCGames.Users.Infra.DataProvider.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly FcGamesContexto _context;
    public IUsuarioRepository UsuarioRepository { get; }

    public UnitOfWork(FcGamesContexto context, IUsuarioRepository usuarioRepository)
    {
        _context = context;
        UsuarioRepository = usuarioRepository;
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 3.7: Criar PasswordHasherService**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Services/PasswordHasherService.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Services;
using System.Security.Cryptography;

namespace Fiap.FCGames.Users.Infra.DataProvider.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string GerarHash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2|SHA256|{Iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(key)}";
    }

    public bool Verificar(string senhaTexto, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash)) return false;

        var partes = senhaHash.Split('|');
        if (partes.Length == 5 && partes[0] == "PBKDF2")
        {
            var algoritmo = partes[1] == "SHA1" ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
            var iterations = int.Parse(partes[2]);
            var salt = Convert.FromBase64String(partes[3]);
            var hashEsperado = Convert.FromBase64String(partes[4]);
            var hashAtual = Rfc2898DeriveBytes.Pbkdf2(senhaTexto, salt, iterations, algoritmo, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hashAtual, hashEsperado);
        }
        return false;
    }
}
```

- [ ] **Step 3.8: Criar TokenService**

`app/src/Fiap.FCGames.Users.Infra/DataProvider/Services/TokenService.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fiap.FCGames.Users.Infra.DataProvider.Services;

public class TokenService : ITokenService
{
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;

    public TokenService(IConfiguration configuration)
    {
        _jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuração Jwt:Key não encontrada. Defina a env var JWT__KEY.");
        _jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuração Jwt:Issuer não encontrada. Defina a env var JWT__ISSUER.");
    }

    public string GerarToken(Guid userId, string email, TipoAcesso tipoAcesso, DateTime expiracao)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, tipoAcesso.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            claims: claims,
            expires: expiracao,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 3.9: Build do Infra**

```bash
dotnet build Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 3.10: Commit**

```bash
git add app/src/Fiap.FCGames.Users.Infra/
git commit -m "feat(infra): EF Core, repositórios, UnitOfWork, PasswordHasher, TokenService"
```

---

## Task 4: Application Layer

**Files:**
- Create: `app/src/Fiap.FCGames.Users.Application/` — todos os arquivos abaixo

- [ ] **Step 4.1: Criar marker e behavior**

`app/src/Fiap.FCGames.Users.Application/IAssemblyMarker.cs`:
```csharp
namespace Fiap.FCGames.Users.Application;

public interface IAssemblyMarker { }
```

`app/src/Fiap.FCGames.Users.Application/Behaviors/ValidatorBehaviors.cs`:
```csharp
using FluentValidation;
using MediatR;

namespace Fiap.FCGames.Users.Application.Behaviors;

public class ValidatorBehaviors<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidatorBehaviors(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }
        return await next();
    }
}
```

- [ ] **Step 4.2: Criar CriarUsuario**

`app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioCommand.cs`:
```csharp
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public record CriarUsuarioCommand(
    string Nome,
    string Email,
    string Senha) : IRequest<CriarUsuarioResponse>;
```

`app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioResponse.cs`:
```csharp
namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public class CriarUsuarioResponse
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioCommandValidator.cs`:
```csharp
using FluentValidation;

namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public class CriarUsuarioCommandValidator : AbstractValidator<CriarUsuarioCommand>
{
    public CriarUsuarioCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(6);
    }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioCommandHandler.cs`:

> **NOTA:** O publisher MassTransit será injetado na Task 9. Por ora o handler não tem a dependência. Será modificado depois.

```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public class CriarUsuarioCommandHandler : IRequestHandler<CriarUsuarioCommand, CriarUsuarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;

    public CriarUsuarioCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<CriarUsuarioResponse> Handle(CriarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var emailExiste = await _unitOfWork.UsuarioRepository.ExisteEmailAsync(request.Email);
        if (emailExiste)
            throw new BusinessException("Já existe um usuário com o e-mail informado.");

        var usuario = new Usuario
        {
            Id = UsuarioId.New(),
            Nome = request.Nome,
            Email = request.Email.ToLower(),
            SenhaHash = _passwordHasher.GerarHash(request.Senha),
            TipoAcesso = TipoAcesso.Standard,
            CriadoEm = DateTime.UtcNow
        };

        _unitOfWork.UsuarioRepository.Adicionar(usuario);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CriarUsuarioResponse
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email
        };
    }
}
```

- [ ] **Step 4.3: Criar AtualizarUsuario**

`app/src/Fiap.FCGames.Users.Application/Commands/AtualizarUsuario/AtualizarUsuarioCommand.cs`:
```csharp
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public record AtualizarUsuarioCommand(
    Guid Id,
    string Nome,
    string Email,
    string Senha) : IRequest<AtualizarUsuarioResponse>;
```

`app/src/Fiap.FCGames.Users.Application/Commands/AtualizarUsuario/AtualizarUsuarioResponse.cs`:
```csharp
namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public class AtualizarUsuarioResponse
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/AtualizarUsuario/AtualizarUsuarioCommandValidator.cs`:
```csharp
using FluentValidation;

namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public class AtualizarUsuarioCommandValidator : AbstractValidator<AtualizarUsuarioCommand>
{
    public AtualizarUsuarioCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(6);
    }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/AtualizarUsuario/AtualizarUsuarioCommandHandler.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public class AtualizarUsuarioCommandHandler : IRequestHandler<AtualizarUsuarioCommand, AtualizarUsuarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;

    public AtualizarUsuarioCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AtualizarUsuarioResponse> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _unitOfWork.UsuarioRepository.ObterPorIdAsync(new UsuarioId(request.Id));
        if (usuario is null)
            throw new NotFoundException("Usuário não encontrado.");

        usuario.Nome = request.Nome;
        usuario.Email = request.Email.ToLower();
        usuario.SenhaHash = _passwordHasher.GerarHash(request.Senha);

        _unitOfWork.UsuarioRepository.Atualizar(usuario);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AtualizarUsuarioResponse
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email
        };
    }
}
```

- [ ] **Step 4.4: Criar Login**

`app/src/Fiap.FCGames.Users.Application/Commands/Login/LoginCommand.cs`:
```csharp
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.Login;

public record LoginCommand(string Email, string Senha) : IRequest<UsuarioLogadoDto>;
```

`app/src/Fiap.FCGames.Users.Application/Commands/Login/UsuarioLogadoDto.cs`:
```csharp
namespace Fiap.FCGames.Users.Application.Commands.Login;

public class UsuarioLogadoDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public DateTime Expiracao { get; set; }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/Login/LoginCommandValidator.cs`:
```csharp
using FluentValidation;

namespace Fiap.FCGames.Users.Application.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty();
    }
}
```

`app/src/Fiap.FCGames.Users.Application/Commands/Login/LoginCommandHandler.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, UsuarioLogadoDto>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<UsuarioLogadoDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(request.Email);

        if (usuario is null || !_passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            throw new LoginException("E-mail ou senha inválidos.", 401);

        var expiracao = DateTime.UtcNow.AddMinutes(30);
        var token = _tokenService.GerarToken(usuario.Id.Value, usuario.Email, usuario.TipoAcesso, expiracao);

        return new UsuarioLogadoDto
        {
            Email = usuario.Email,
            Token = token,
            Expiracao = expiracao
        };
    }
}
```

- [ ] **Step 4.5: Criar Queries**

`app/src/Fiap.FCGames.Users.Application/Queries/BuscarUsuarioPorId/BuscarUsuarioPorIdQuery.cs`:
```csharp
using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public record BuscarUsuarioPorIdQuery(Guid Id) : IRequest<DetalhesUsuarioDto>;
```

`app/src/Fiap.FCGames.Users.Application/Queries/BuscarUsuarioPorId/DetalhesUsuarioDto.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public class DetalhesUsuarioDto
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public TipoAcesso TipoAcesso { get; set; }
    public DateTime CriadoEm { get; set; }
}
```

`app/src/Fiap.FCGames.Users.Application/Queries/BuscarUsuarioPorId/BuscarUsuarioPorIdQueryHandler.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public class BuscarUsuarioPorIdQueryHandler : IRequestHandler<BuscarUsuarioPorIdQuery, DetalhesUsuarioDto>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public BuscarUsuarioPorIdQueryHandler(IUsuarioRepository usuarioRepository)
        => _usuarioRepository = usuarioRepository;

    public async Task<DetalhesUsuarioDto> Handle(BuscarUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(new UsuarioId(request.Id));
        if (usuario is null)
            throw new NotFoundException("Usuário não encontrado.");

        return new DetalhesUsuarioDto
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email,
            TipoAcesso = usuario.TipoAcesso,
            CriadoEm = usuario.CriadoEm
        };
    }
}
```

`app/src/Fiap.FCGames.Users.Application/Queries/ListarUsuarios/ListarUsuariosQuery.cs`:
```csharp
using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.ListarUsuarios;

public record ListarUsuariosQuery : IRequest<IEnumerable<ListaUsuariosDto>>;
```

`app/src/Fiap.FCGames.Users.Application/Queries/ListarUsuarios/ListaUsuariosDto.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Application.Queries.ListarUsuarios;

public class ListaUsuariosDto
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public TipoAcesso TipoAcesso { get; set; }
}
```

`app/src/Fiap.FCGames.Users.Application/Queries/ListarUsuarios/ListarUsuariosQueryHandler.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Interfaces;
using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.ListarUsuarios;

public class ListarUsuariosQueryHandler : IRequestHandler<ListarUsuariosQuery, IEnumerable<ListaUsuariosDto>>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ListarUsuariosQueryHandler(IUsuarioRepository usuarioRepository)
        => _usuarioRepository = usuarioRepository;

    public async Task<IEnumerable<ListaUsuariosDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync();
        return usuarios.Select(u => new ListaUsuariosDto
        {
            Id = u.Id.Value,
            Nome = u.Nome,
            Email = u.Email,
            TipoAcesso = u.TipoAcesso
        });
    }
}
```

- [ ] **Step 4.6: Build da Application**

```bash
dotnet build Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 4.7: Commit**

```bash
git add app/src/Fiap.FCGames.Users.Application/
git commit -m "feat(application): CQRS handlers e validators para Users"
```

---

## Task 5: CrossCutting Layer

**Files:**
- Create: `app/src/Fiap.FCGames.Users.CrossCutting/` — todos os arquivos abaixo

- [ ] **Step 5.1: Criar DI registration**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/RegisterDependencyInjectionExtensions.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using Fiap.FCGames.Users.Infra.DataProvider.Repositories;
using Fiap.FCGames.Users.Infra.DataProvider.Services;
using Fiap.FCGames.Users.Infra.DataProvider.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class RegisterDependencyInjectionExtensions
{
    public static void RegisterDI(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, TokenService>();
    }
}
```

- [ ] **Step 5.2: Criar extensões JWT**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/JwtAutenticacaoExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class JwtAutenticacaoExtensions
{
    public static void AddAutenticacaoApi(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuração Jwt:Key não encontrada. Defina a env var JWT__KEY.");
        var jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuração Jwt:Issuer não encontrada. Defina a env var JWT__ISSUER.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    }
}
```

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/JwtAutorizacaoExtensions.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class JwtAutorizacaoExtensions
{
    public static void AddAutorizacaoApi(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("Standard", policy => policy.RequireRole("Standard"));
        });
    }
}
```

- [ ] **Step 5.3: Criar extensão Swagger**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/SwaggerExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class SwaggerExtensions
{
    public static void RegisterSwaggerGenerator(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Fiap - FCGames Users", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token: Bearer {seu-token}"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
            });
        });
    }

    public static void RegisterSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Fiap - FCGames Users"));
    }

    public static void RegisterScalar(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapScalarApiReference(options =>
        {
            options.Title = "API Fiap - FCGames Users";
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        });
    }
}
```

- [ ] **Step 5.4: Criar extensão MediatR**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/MediatRExtensions.cs`:
```csharp
using Fiap.FCGames.Users.Application;
using Fiap.FCGames.Users.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class MediatRExtensions
{
    public static IServiceCollection AddMediatRConfiguration(this IServiceCollection services)
    {
        var appAssembly = typeof(IAssemblyMarker).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
        services.AddValidatorsFromAssembly(appAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehaviors<,>));

        return services;
    }
}
```

- [ ] **Step 5.5: Criar extensão Database**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/RegisterContextDatabaseExtensions.cs`:
```csharp
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class RegisterContextDatabaseExtensions
{
    public static void AddContextDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FcGamesContexto>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
    }
}
```

- [ ] **Step 5.6: Criar placeholder MassTransit (será completado na Task 9)**

`app/src/Fiap.FCGames.Users.CrossCutting/Extensions/MassTransitExtensions.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class MassTransitExtensions
{
    public static void AddMassTransitRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO Task 9: registrar MassTransit + RabbitMQ
        // Deixar vazio até Task 9 para não bloquear o build
    }
}
```

- [ ] **Step 5.7: Criar middlewares**

`app/src/Fiap.FCGames.Users.CrossCutting/Middleware/ErrorHandlingMiddleware.cs`:
```csharp
using Fiap.FCGames.Users.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.FCGames.Users.CrossCutting.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        if (exception is ValidationException ve)
        {
            _logger.LogWarning("Erro de validação: {Fields}", ve.Errors.Select(e => e.PropertyName));
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var r = new { StatusCode = 400, Errors = ve.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }) };
            return context.Response.WriteAsync(JsonSerializer.Serialize(r, opts));
        }

        if (exception is LoginException le)
        {
            _logger.LogWarning(le, "Erro de login");
            context.Response.StatusCode = le.StatusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = le.StatusCode, Message = le.Message }, opts));
        }

        if (exception is NotFoundException nfe)
        {
            _logger.LogWarning(nfe, "Recurso não encontrado");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 404, Message = nfe.Message }, opts));
        }

        if (exception is BusinessException be)
        {
            _logger.LogWarning(be, "Erro de negócio");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 400, Message = be.Message }, opts));
        }

        _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { StatusCode = 500, Message = "Ocorreu um erro interno. Tente novamente mais tarde." }, opts));
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public required string Message { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DetailedMessage { get; set; }
}
```

`app/src/Fiap.FCGames.Users.CrossCutting/Middleware/ErrorHandlingMiddlewareExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Builder;

namespace Fiap.FCGames.Users.CrossCutting.Middleware;

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<ErrorHandlingMiddleware>();
}
```

`app/src/Fiap.FCGames.Users.CrossCutting/Middleware/RegisterUsoCorrelationMiddleware.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Serilog.Context;

namespace Fiap.FCGames.Users.CrossCutting.Middleware;

public static class RegisterUsoCorrelationMiddleware
{
    public static void UseCorrelationId(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["x-correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.Headers["x-correlation-ID"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}
```

- [ ] **Step 5.8: Build do CrossCutting**

```bash
dotnet build Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 5.9: Commit**

```bash
git add app/src/Fiap.FCGames.Users.CrossCutting/
git commit -m "feat(crosscutting): Serilog, JWT, Swagger, ErrorHandling, CorrelationId"
```

---

## Task 6: Api Layer — Program.cs, Controllers, Configs

**Files:**
- Create/Modify: `app/src/Fiap.FCGames.Users.Api/`

- [ ] **Step 6.1: Reescrever appsettings.json (SEM JWT key)**

`app/src/Fiap.FCGames.Users.Api/appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "Enrich": [ "FromLogContext" ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ]
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "AppFiapFcGames"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=fcgames.db"
  }
}
```

> **ATENÇÃO:** `Jwt:Key` foi removido intencionalmente. Definir via env var `JWT__KEY`.

- [ ] **Step 6.2: Criar appsettings.Development.json**

`app/src/Fiap.FCGames.Users.Api/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 6.3: Criar launchSettings.json com env vars para dev local**

`app/src/Fiap.FCGames.Users.Api/Properties/launchSettings.json`:
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "JWT__KEY": "ChaveSegredoLocalDesenvolvimento1234567890Ab",
        "JWT__ISSUER": "AppFiapFcGames",
        "ConnectionStrings__DefaultConnection": "Data Source=fcgames.db"
      }
    }
  }
}
```

- [ ] **Step 6.4: Criar ApiControllerBase**

`app/src/Fiap.FCGames.Users.Api/Controllers/Shared/ApiControllerBase.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Users.Api.Controllers.Shared;

[ApiController]
public abstract class ApiControllerBase<T> : ControllerBase where T : class
{
    protected readonly ISender _sender;
    protected readonly ILogger<T> _logger;

    protected ApiControllerBase(ISender sender, ILogger<T> logger)
    {
        _sender = sender;
        _logger = logger;
    }
}
```

- [ ] **Step 6.5: Criar UsuariosController**

`app/src/Fiap.FCGames.Users.Api/Controllers/UsuariosController.cs`:
```csharp
using Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;
using Fiap.FCGames.Users.Application.Commands.CriarUsuario;
using Fiap.FCGames.Users.Application.Commands.Login;
using Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;
using Fiap.FCGames.Users.Application.Queries.ListarUsuarios;
using Fiap.FCGames.Users.Api.Controllers.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.FCGames.Users.Api.Controllers;

[Route("usuarios")]
public class UsuariosController : ApiControllerBase<UsuariosController>
{
    public UsuariosController(ISender sender, ILogger<UsuariosController> logger)
        : base(sender, logger) { }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CriarAsync([FromBody] CriarUsuarioCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);
        _logger.LogInformation("Login realizado para {Email}", command.Email);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ListarTodosAsync()
    {
        var result = await _sender.Send(new ListarUsuariosQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> BuscarPorIdAsync(Guid id)
    {
        var result = await _sender.Send(new BuscarUsuarioPorIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AtualizarAsync(Guid id, [FromBody] AtualizarUsuarioCommand command)
    {
        var result = await _sender.Send(command with { Id = id });
        return Ok(result);
    }
}
```

- [ ] **Step 6.6: Criar Program.cs**

`app/src/Fiap.FCGames.Users.Api/Program.cs`:
```csharp
using Fiap.FCGames.Users.CrossCutting.Extensions;
using Fiap.FCGames.Users.CrossCutting.Middleware;
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);
builder.Services.AddOpenApi();

builder.Services.RegisterDI();
builder.Services.AddMediatRConfiguration();
builder.Services.RegisterSwaggerGenerator();
builder.Services.AddAutenticacaoApi(builder.Configuration);
builder.Services.AddAutorizacaoApi();
builder.Services.AddContextDatabase(builder.Configuration);
builder.Services.AddMassTransitRabbitMq(builder.Configuration);
builder.Services.AddHealthChecks();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FcGamesContexto>();
    db.Database.Migrate();
    await SeedAdminAsync(scope.ServiceProvider);
}

app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.RegisterSwagger();
    app.MapOpenApi();
    app.RegisterScalar();
}

app.UseErrorHandlingMiddleware();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task SeedAdminAsync(IServiceProvider services)
{
    var db = services.GetRequiredService<FcGamesContexto>();
    var hasAdmin = await db.Usuarios.AnyAsync(u => u.IdTipoAcesso == 1);
    if (hasAdmin) return;

    var passwordHasher = services.GetRequiredService<Fiap.FCGames.Users.Domain.Services.IPasswordHasherService>();
    var admin = new Fiap.FCGames.Users.Domain.Aggregates.Usuario
    {
        Id = Fiap.FCGames.Users.Domain.Aggregates.UsuarioId.New(),
        Nome = "Administrador",
        Email = "admin@fcgames.com",
        SenhaHash = passwordHasher.GerarHash("Admin@123"),
        TipoAcesso = Fiap.FCGames.Users.Domain.Aggregates.TipoAcesso.Admin,
        CriadoEm = DateTime.UtcNow
    };

    db.Usuarios.Add(admin);
    await db.SaveChangesAsync();

    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Seed: usuário Admin criado (admin@fcgames.com / Admin@123)");
}
```

- [ ] **Step 6.7: Atualizar o .csproj da Api para limpar referências desnecessárias**

Substituir o conteúdo de `app/src/Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Fiap.FCGames.Users.Application\Fiap.FCGames.Users.Application.csproj" />
    <ProjectReference Include="..\Fiap.FCGames.Users.CrossCutting\Fiap.FCGames.Users.CrossCutting.csproj" />
    <ProjectReference Include="..\Fiap.FCGames.Users.Domain\Fiap.FCGames.Users.Domain.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6.8: Build completo da solução**

```bash
dotnet build Fiap.FCGames.Users.sln
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 6.9: Commit**

```bash
git add app/src/Fiap.FCGames.Users.Api/
git commit -m "feat(api): Program.cs, controllers, appsettings e seed Admin"
```

---

## Task 7: EF Core Migration

**Files:**
- Create: `app/src/Fiap.FCGames.Users.Infra/Migrations/` (gerado pelo EF)

- [ ] **Step 7.1: Instalar dotnet-ef tool se necessário**

```bash
dotnet tool install --global dotnet-ef
# Se já instalado: dotnet tool update --global dotnet-ef
```

- [ ] **Step 7.2: Gerar migration inicial**

Execute a partir de `app/src/`:
```bash
dotnet ef migrations add InitialCreate \
  --project Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj \
  --startup-project Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj \
  --output-dir DataProvider/Migrations
```

Expected: criados 3 arquivos em `Fiap.FCGames.Users.Infra/DataProvider/Migrations/`:
- `<timestamp>_InitialCreate.cs`
- `<timestamp>_InitialCreate.Designer.cs`
- `FcGamesContextoModelSnapshot.cs`

- [ ] **Step 7.3: Verificar conteúdo da migration**

Abrir o arquivo `<timestamp>_InitialCreate.cs` e confirmar que a tabela `usuarios` tem as colunas:
- `id` (TEXT PK)
- `nome` (TEXT NOT NULL, maxLength 150)
- `email` (TEXT NOT NULL, maxLength 200) + índice UNIQUE
- `senha_hash` (TEXT NOT NULL, maxLength 255)
- `criado_em` (TEXT NOT NULL)
- `tipo_acesso` (INTEGER NOT NULL)

- [ ] **Step 7.4: Commit**

```bash
git add app/src/Fiap.FCGames.Users.Infra/DataProvider/Migrations/
git commit -m "feat(infra): migration inicial da tabela usuarios"
```

---

## Task 8: FCGames.IntegrationEvents — nuget.config e referência

**Files:**
- Create: `nuget.config` (raiz do fcg-users-api)
- Modify: `app/src/Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj`

- [ ] **Step 8.1: Criar nuget.config na raiz do repo**

`nuget.config` (em `fcg-users-api/`, não dentro de `app/src/`):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/FIAP-POS-TECH-TEAM-10/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="OWNER" />
      <add key="ClearTextPassword" value="%NUGET_AUTH_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

> Para usar localmente: `$env:NUGET_AUTH_TOKEN = "ghp_seu_pat_com_read_packages"`

- [ ] **Step 8.2: Adicionar pacote FCGames.IntegrationEvents na Application**

```bash
cd app/src
dotnet add Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj \
  package FCGames.IntegrationEvents --version 1.0.0-preview.1
```

> **Se a env var não estiver setada, esse comando vai falhar.** Isso é esperado em CI sem credenciais. Para build local sem o PAT, comente a referência e descomente ao integrar com RabbitMQ.

- [ ] **Step 8.3: Commit**

```bash
git add nuget.config app/src/Fiap.FCGames.Users.Application/
git commit -m "feat: nuget.config GitHub Packages + FCGames.IntegrationEvents ref"
```

---

## Task 9: MassTransit — publisher UserCreatedEvent

**Files:**
- Modify: `app/src/Fiap.FCGames.Users.CrossCutting/Extensions/MassTransitExtensions.cs`
- Modify: `app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioCommandHandler.cs`

- [ ] **Step 9.1: Implementar MassTransitExtensions**

Substituir o conteúdo de `app/src/Fiap.FCGames.Users.CrossCutting/Extensions/MassTransitExtensions.cs`:
```csharp
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class MassTransitExtensions
{
    public static void AddMassTransitRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    "/",
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });
            });
        });
    }
}
```

- [ ] **Step 9.2: Adicionar publisher ao CriarUsuarioCommandHandler**

Substituir o conteúdo de `app/src/Fiap.FCGames.Users.Application/Commands/CriarUsuario/CriarUsuarioCommandHandler.cs`:
```csharp
using FCGames.IntegrationEvents;
using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public class CriarUsuarioCommandHandler : IRequestHandler<CriarUsuarioCommand, CriarUsuarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CriarUsuarioCommandHandler> _logger;

    public CriarUsuarioCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasherService passwordHasher,
        IPublishEndpoint publishEndpoint,
        ILogger<CriarUsuarioCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<CriarUsuarioResponse> Handle(CriarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var emailExiste = await _unitOfWork.UsuarioRepository.ExisteEmailAsync(request.Email);
        if (emailExiste)
            throw new BusinessException("Já existe um usuário com o e-mail informado.");

        var usuario = new Usuario
        {
            Id = UsuarioId.New(),
            Nome = request.Nome,
            Email = request.Email.ToLower(),
            SenhaHash = _passwordHasher.GerarHash(request.Senha),
            TipoAcesso = TipoAcesso.Standard,
            CriadoEm = DateTime.UtcNow
        };

        _unitOfWork.UsuarioRepository.Adicionar(usuario);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            await _publishEndpoint.Publish(new UserCreatedEvent(
                UserId: usuario.Id.Value,
                Nome: usuario.Nome,
                Email: usuario.Email,
                CreatedAtUtc: usuario.CriadoEm,
                CorrelationId: Guid.NewGuid()
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar UserCreatedEvent para UserId {UserId}. Cadastro mantido.", usuario.Id.Value);
        }

        return new CriarUsuarioResponse
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email
        };
    }
}
```

- [ ] **Step 9.3: Build completo**

```bash
dotnet build Fiap.FCGames.Users.sln
```
Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 9.4: Commit**

```bash
git add app/src/Fiap.FCGames.Users.CrossCutting/ app/src/Fiap.FCGames.Users.Application/
git commit -m "feat(messaging): MassTransit RabbitMQ + publisher UserCreatedEvent"
```

---

## Task 10: Dockerfile multi-stage

**Files:**
- Create: `Dockerfile` (raiz do `fcg-users-api/`)

- [ ] **Step 10.1: Criar Dockerfile**

`Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["app/src/Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj", "app/src/Fiap.FCGames.Users.Api/"]
COPY ["app/src/Fiap.FCGames.Users.Application/Fiap.FCGames.Users.Application.csproj", "app/src/Fiap.FCGames.Users.Application/"]
COPY ["app/src/Fiap.FCGames.Users.CrossCutting/Fiap.FCGames.Users.CrossCutting.csproj", "app/src/Fiap.FCGames.Users.CrossCutting/"]
COPY ["app/src/Fiap.FCGames.Users.Domain/Fiap.FCGames.Users.Domain.csproj", "app/src/Fiap.FCGames.Users.Domain/"]
COPY ["app/src/Fiap.FCGames.Users.Infra/Fiap.FCGames.Users.Infra.csproj", "app/src/Fiap.FCGames.Users.Infra/"]
COPY ["nuget.config", "."]

RUN dotnet restore "app/src/Fiap.FCGames.Users.Api/Fiap.FCGames.Users.Api.csproj"

COPY . .

WORKDIR "/src/app/src/Fiap.FCGames.Users.Api"
RUN dotnet build "Fiap.FCGames.Users.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fiap.FCGames.Users.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Fiap.FCGames.Users.Api.dll"]
```

> **Variáveis obrigatórias em runtime (passar via -e ou docker-compose env_file):**
> - `JWT__KEY`
> - `JWT__ISSUER` (default: AppFiapFcGames)
> - `ConnectionStrings__DefaultConnection`
> - `RabbitMQ__Host`, `RabbitMQ__Username`, `RabbitMQ__Password`

- [ ] **Step 10.2: Verificar build Docker (opcional — requer Docker Desktop)**

```bash
docker build -t fcg-users-api:local . --build-arg NUGET_AUTH_TOKEN=$env:NUGET_AUTH_TOKEN
```

- [ ] **Step 10.3: Commit**

```bash
git add Dockerfile
git commit -m "feat: Dockerfile multi-stage SDK->ASPNet para Users API"
```

---

## Task 11: README

**Files:**
- Modify: `README.md` (raiz do `fcg-users-api/`)

- [ ] **Step 11.1: Reescrever README.md**

Substituir o conteúdo de `README.md`:
```markdown
# fcg-users-api

Microsserviço de usuários do sistema FCGames. Responsável por cadastro, autenticação JWT e gestão de usuários. **Único serviço que emite tokens JWT** — os demais apenas validam.

## Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/usuarios` | Público | Cadastro de novo usuário |
| POST | `/usuarios/login` | Público | Autenticação — retorna JWT |
| GET | `/usuarios` | Admin | Lista todos os usuários |
| GET | `/usuarios/{id}` | Admin | Busca usuário por ID |
| PUT | `/usuarios/{id}` | Admin | Atualiza dados do usuário |
| GET | `/health` | Público | Health check |

## Evento Publicado

- **`UserCreatedEvent`** (RabbitMQ via MassTransit) — disparado após commit do cadastro
  - Consumido por: CatalogAPI (cria Biblioteca), NotificationsAPI (log boas-vindas)
  - Falha do broker **não** interrompe o cadastro

## Variáveis de Ambiente Obrigatórias

| Variável | Descrição | Exemplo |
|---|---|---|
| `JWT__KEY` | Chave de assinatura JWT (mín. 32 chars) | `MinhaChaveSeguraComPeloMenos32Chars!` |
| `JWT__ISSUER` | Issuer do token | `AppFiapFcGames` |
| `ConnectionStrings__DefaultConnection` | Connection string SQLite / PostgreSQL | `Data Source=fcgames.db` |
| `RabbitMQ__Host` | Host do RabbitMQ | `rabbitmq` (Docker) / `localhost` (dev) |
| `RabbitMQ__Username` | Usuário RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha RabbitMQ | `guest` |

## Rodar Localmente

```bash
cd app/src/Fiap.FCGames.Users.Api
dotnet run
# Swagger: http://localhost:5001/swagger
# Scalar:  http://localhost:5001/scalar
```

O banco SQLite é criado e migrado automaticamente na primeira execução.

**Seed inicial:** usuário Admin criado automaticamente se não existir (`admin@fcgames.com` / `Admin@123`).

## Rodar via Docker

```bash
docker run -p 5001:8080 \
  -e JWT__KEY="SuaChaveAqui" \
  -e JWT__ISSUER="AppFiapFcGames" \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/fcgames.db" \
  -e RabbitMQ__Host="localhost" \
  -e RabbitMQ__Username="guest" \
  -e RabbitMQ__Password="guest" \
  fcg-users-api:local
```

## Estrutura

```
app/src/
  Fiap.FCGames.Users.Api/          ← Entry point, controllers, Program.cs
  Fiap.FCGames.Users.Application/  ← CQRS: commands, queries, validators
  Fiap.FCGames.Users.CrossCutting/ ← Serilog, JWT, Swagger, ErrorHandling, MassTransit
  Fiap.FCGames.Users.Domain/       ← Entidades, interfaces, exceções
  Fiap.FCGames.Users.Infra/        ← EF Core, repositórios, migrations, services
```

## JWT — Claims emitidos

| Claim | Valor |
|---|---|
| `nameidentifier` | UserId (GUID) — usado pelo CatalogAPI |
| `sub` | Email do usuário |
| `role` | `Admin` ou `Standard` |
| `jti` | UUID único por token |

Expiração: 30 minutos.
```

- [ ] **Step 11.2: Commit**

```bash
git add README.md
git commit -m "docs: README completo com endpoints, env vars, seed e estrutura"
```

---

## Task 12: Build e Smoke Test

- [ ] **Step 12.1: Build final limpo**

```bash
cd app/src
dotnet build Fiap.FCGames.Users.sln -c Release
```
Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 12.2: Rodar a API localmente**

```bash
cd Fiap.FCGames.Users.Api
dotnet run
```
Expected: Logs JSON do Serilog, mensagem "Now listening on: http://localhost:5001", e se for primeira execução: "Seed: usuário Admin criado".

- [ ] **Step 12.3: Smoke test — cadastro + login**

```bash
# 1. Criar usuário
curl -s -X POST http://localhost:5001/usuarios \
  -H "Content-Type: application/json" \
  -d '{"nome":"Teste","email":"teste@exemplo.com","senha":"Senha@123"}' | ConvertFrom-Json

# 2. Login com Admin (seed)
curl -s -X POST http://localhost:5001/usuarios/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@fcgames.com","senha":"Admin@123"}' | ConvertFrom-Json

# 3. Usar token do login acima para listar usuários
$token = "<token-do-step-2>"
curl -s http://localhost:5001/usuarios \
  -H "Authorization: Bearer $token" | ConvertFrom-Json

# 4. Health check
curl http://localhost:5001/health
# Expected: Healthy
```

- [ ] **Step 12.4: Verificar /health retorna 200**

```bash
curl -o /dev/null -s -w "%{http_code}" http://localhost:5001/health
```
Expected: `200`

- [ ] **Step 12.5: Commit final**

```bash
git add .
git commit -m "chore: verificação final Users API — build e smoke test ok"
```

---

## Checklist de Entrega deste Serviço

- [ ] `POST /usuarios` aceita sem autenticação e cria usuário
- [ ] `POST /usuarios/login` retorna JWT válido com claims `nameidentifier`, `sub`, `role`
- [ ] `GET /usuarios` exige token Admin, retorna lista
- [ ] `PUT /usuarios/{id}` exige token Admin, atualiza
- [ ] `GET /health` retorna 200
- [ ] Serilog emite JSON com `CorrelationId` em todos os logs
- [ ] JWT key não está em nenhum arquivo commitado
- [ ] `UserCreatedEvent` publicado após cadastro (verificável no RabbitMQ Management UI :15672)
- [ ] Build Docker funciona
- [ ] README tem todos os endpoints e env vars documentadas
