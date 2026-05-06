# Labpoint API

Backend para do Labpoint, sistema de reservas de laboratórios.

> [!WARNING]
> Versão recomendada do [Java ou OpenJDK](https://adoptium.net/pt-BR/temurin/releases) é 25 LTS ou superior

> [!WARNING]
> Versão recomendada do [Docker](https://www.docker.com/) é 4 ou superior

# Iniciando

```bash
mvn clean install
mvn spring-boot:run
```

## Scripts

- `mvn clean install` - Limpa o projeto e instala as dependências
- `mvn spring-boot:run` - Inicia o servidor de desenvolvimento

Abra http://localhost:3001/ no seu navegador para ver o resultado.

Abra http://localhost:3001/docs no seu navegador para ver a documentação das rotas.

## Packages

- [Spring Boot](https://start.spring.io/)

## Application properties

- spring.datasource.url=jdbc:[url do banco de dados]()
- spring.datasource.username=[usuario do banco de dados]()
- spring.datasource.password=[senha do banco de dados]()
- spring.jpa.properties.hibernate.dialect=[dialecto do banco de dados]()
