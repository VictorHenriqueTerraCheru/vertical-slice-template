# FluentValidation na borda, regra de negócio no service

Formato e limites da entrada são validados no Module com `.Validate<T>()` e um validator por request; todos os erros de campo voltam juntos em `ValidationProblem` (400). O service só aplica regra de negócio e devolve `Result<T>` com um erro por resultado.

Por que: o cliente corrige tudo de uma vez, e o service nunca precisa re-checar formato. Constantes de limite (por exemplo `AuthConstants.PasswordMinLength`) são únicas e usadas no validator e na configuração do Identity.

Consequência: regra que depende de estado (conflito, existência, transição inválida) não vai no validator; vira `Rule` em `<Area>Errors`.
