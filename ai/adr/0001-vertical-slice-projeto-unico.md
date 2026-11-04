# Vertical slice em um projeto só

O código se organiza por área de negócio (`Feature/<Area>`), não por camada técnica, dentro de um único projeto. Uma área inteira cabe em uma pasta e em um Module, e o custo de manter vários projetos e referências não se paga em uma API deste tamanho.

Consequências: a separação em camadas é de convenção e protegida por testes de arquitetura (`tests/Starter.Api.Tests/Architecture/ArchitectureTests.cs`), não pelo compilador. Entidades e `DbContext` ficam em `Infrastructure/Persistence`, compartilhados por todas as áreas. Sem Controllers, MediatR nem CQRS formal.

Alternativa rejeitada: solução com um projeto por camada. Mais cerimônia, mesma garantia que os testes já dão.
