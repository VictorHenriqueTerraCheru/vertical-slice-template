# EF Core sempre atrás de repository, com IUnitOfWork

Todo acesso a dados passa por um repository, inclusive em `Auth`. Service não vê `AppDbContext` nem `IQueryable`; repository não salva. O service decide quando salvar por `IUnitOfWork`: `SaveChangesAsync` para escrita única sem conflito, `ExecuteInTransactionAsync` para update ou delete que pode conflitar e para operações com mais de um repository. A transação faz rollback quando o `Result<T>` falha e traduz conflito de concorrência em 409.

Por que: mantém regra de negócio testável pela API sem LINQ espalhado, e dá um único lugar para a transação.

Consequências: um repository por agregado, com métodos nomeados pela intenção. `SaveChangesAsync` devolve `Task` de propósito, porque não há resultado a traduzir.
