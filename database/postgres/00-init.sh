#!/bin/sh
# Lo ejecuta la imagen oficial de PostgreSQL solo la primera vez (volumen de datos vacío).
set -e

for script in /docker-entrypoint-initdb.d/scripts/*.sql; do
    echo "Ejecutando ${script}"
    psql -v ON_ERROR_STOP=1 \
        --username "$POSTGRES_USER" \
        --dbname "$POSTGRES_DB" \
        -v db_name="$POSTGRES_DB" \
        -v app_user="$APP_DB_USER" \
        -v app_password="$APP_DB_PASSWORD" \
        -f "$script"
done
