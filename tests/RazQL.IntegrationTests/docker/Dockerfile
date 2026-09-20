FROM ghcr.io/pmeyer/gmdb-liquibase:2.0.0 AS migrator

FROM ghcr.io/pmeyer/gmdb-postgres-jsonschema:1.0.1

COPY --from=migrator /opt/java/openjdk /opt/java/openjdk
COPY --from=migrator /app/gmdb-liquibase.jar /opt/gmdb/gmdb-liquibase.jar
COPY start-gmdb.sh /usr/local/bin/start-gmdb.sh

RUN chmod 755 /usr/local/bin/start-gmdb.sh

ENV JAVA_HOME=/opt/java/openjdk \
    PATH="/opt/java/openjdk/bin:${PATH}" \
    POSTGRES_DB=gmdb \
    POSTGRES_USER=gmdbuser

HEALTHCHECK --interval=2s --timeout=3s --start-period=120s --retries=3 \
    CMD test -f /tmp/razql-gmdb-ready && pg_isready -h 127.0.0.1 -p 5432 -U "$POSTGRES_USER" -d gmdb

ENTRYPOINT ["/usr/local/bin/start-gmdb.sh"]
CMD ["postgres"]
