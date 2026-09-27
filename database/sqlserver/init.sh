#!/bin/bash
# Aplica los scripts de /init/scripts en orden. Todos son idempotentes: se puede ejecutar varias veces.
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

for script in /init/scripts/*.sql; do
    echo "Ejecutando ${script}"
    "$SQLCMD" -S "${DB_HOST},${DB_PORT:-1433}" -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I \
        -v DbName="$DB_NAME" AppUser="$APP_DB_USER" AppPassword="$APP_DB_PASSWORD" \
        -i "$script"
done

echo "Base de datos SQL Server lista."
