# Labpoint API

Backend para do Labpoint, sistema de reservas de laboratórios.

> [!WARNING]
> Versão recomendada do [Java ou OpenJDK](https://adoptium.net/pt-BR/temurin/releases) é 25 LTS ou superior

> [!WARNING]
> Versão recomendada do [Maven](https://maven.apache.org/) é 3.8 ou superior(adicionar as variaveis de ambiente)

> [!WARNING]
> Para o banco de dados, recomendo baixar o [Docker](https://www.docker.com/) ou [Postgres](https://www.postgresql.org/)
>
> Caso esteja usando o postgre nativo, nao se esqueça de configurar as credencias de acesso em `./src/main/resources/application-dev.yaml`

# Iniciando

```bash
mvn clean install
mvn spring-boot:run
```

## Scripts

- `mvn clean install` - Limpa o projeto e instala as dependências
- `mvn spring-boot:run` - Inicia o servidor de desenvolvimento
- `mvn clean package` - Cria o arquivo jar do projeto
- `mvn test` - Executa os testes do projeto
- `mvn spring-boot:run -Dspring-boot.run.profiles=rel` - Inicia o servidor de desenvolvimento com o perfil para prod
- `mvn clean package -Dspring-boot.run.profiles=rel` - Cria o arquivo jar do projeto com o perfil para prod

Abra http://localhost:8080/ no seu navegador para ver o resultado.

Abra http://localhost:8080/docs no seu navegador para ver a documentação das rotas.

## Packages

- [Spring Boot](https://start.spring.io/)

## application.yaml

- spring.datasource.url=jdbc:[url do banco de dados]()
- spring.datasource.username=[usuario do banco de dados]()
- spring.datasource.password=[senha do banco de dados]()
- spring.jpa.properties.hibernate.dialect=[dialecto do banco de dados]()
- spring.mail.username=[gmail para mail server]()
- spring.mail.password=[aplication password para do gmail]()
