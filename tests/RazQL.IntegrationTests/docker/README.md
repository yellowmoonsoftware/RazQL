# GMDB integration-test image

This image extends the versioned GMDB PostgreSQL image with the versioned Liquibase application. At startup it initializes PostgreSQL, runs the `bootstrap` and `migrate` profiles in order, and becomes healthy only after both succeed. It does not contain test seed data.

Build from the RazQL repository root:

```sh
docker build -t razql-gmdb-integration:local tests/RazQL.IntegrationTests/docker
```

The build pulls both base images from GitHub Container Registry. If anonymous pulls are denied, authenticate Docker with a GitHub token that has package read access before building; CI will need the same access. Do not put registry credentials in the Dockerfile or repository.

The container requires `POSTGRES_PASSWORD`, `GMDB_APP_PASSWORD`, and `GMDB_ADMIN_PASSWORD` at runtime. `POSTGRES_DB=gmdb` and `POSTGRES_USER=gmdbuser` are set by the image. The .NET fixture builds this image, supplies ephemeral credentials, waits for the healthy state, and applies `Fixtures/test-data.sql` separately.
